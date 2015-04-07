using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Client;
using Domain;
using SimpleStorage.Infrastructure;

namespace SimpleStorage.Controllers
{
    public class ValuesController : ApiController
    {
        private readonly IStateRepository stateRepository;
        private readonly IConfiguration configuration;
        private readonly IStorage storage;

        public ValuesController(IStorage storage, IStateRepository stateRepository, IConfiguration configuration)
        {
            this.storage = storage;
            this.stateRepository = stateRepository;
            this.configuration = configuration;
        }

        private void CheckState()
        {
            if (stateRepository.GetState() != State.Started)
                throw new HttpResponseException(HttpStatusCode.InternalServerError);
        }

        // GET api/values/5 
        public Value Get(string id)
        {
            Value result = null;
            var valueComparer = new ValueComparer();
            var quorum = GetQuorum();
            var successfulReadsCount = 0;
            Value value;
            if (TryReadFromThisReplica(id, out value))
            {
                successfulReadsCount++;
                result = value;
            }
            var replicas = configuration.Replicas.GetEnumerator();
            while (successfulReadsCount < quorum && replicas.MoveNext())
                if (TryReadFromOtherReplica(replicas.Current, id, out value))
                {
                    successfulReadsCount++;
                    if (valueComparer.Compare(result, value) < 0)
                        result = value;
                }
            if (successfulReadsCount < quorum)
                throw new HttpResponseException(HttpStatusCode.InternalServerError);
            if (result == null)
                throw new HttpResponseException(HttpStatusCode.NotFound);
            return result;
        }

        private bool TryReadFromOtherReplica(IPEndPoint endpoint, string id, out Value value)
        {
            var internalClient = new InternalClient(string.Format("http://{0}/", endpoint));
            try
            {
                value = internalClient.Get(id);
                return true;
            }
            catch (Exception e)
            {
                value = null;
                if (ReplicaNotFoundValue(e))
                    return true;
                Console.WriteLine("Can't read from {0}: {1}", endpoint.Port, e.Message);
                return false;
            }
        }

        private bool ReplicaNotFoundValue(Exception e)
        {
            var expectedHttpResponseException = new HttpResponseException(HttpStatusCode.NotFound);
            var expectedStatusCode = (int)expectedHttpResponseException.Response.StatusCode;
            return
                e.GetType() == typeof(HttpRequestException) &&
                ((HttpRequestException)e).Message.Contains(expectedStatusCode.ToString());
        }

        private bool TryReadFromThisReplica(string id, out Value value)
        {
            try
            {
                CheckState();
                value = storage.Get(id);
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("Can't read from {0}: {1}", configuration.CurrentNodePort, e.Message);
                value = null;
                return false;
            }
        }

        // PUT api/values/5
        public void Put(string id, [FromBody] Value value)
        {
            var quorum = GetQuorum();
            var successfulWritesCount = 0;
            if (TryPutToThisReplica(id, value))
                successfulWritesCount++;
            var replicas = configuration.Replicas.GetEnumerator();
            while (successfulWritesCount < quorum && replicas.MoveNext())
                if (TryPutToOtherReplica(replicas.Current, id, value))
                    successfulWritesCount++;
            if (successfulWritesCount < quorum)
                throw new HttpResponseException(HttpStatusCode.InternalServerError);
        }

        private int GetQuorum()
        {
            var totalReplicasCount = configuration.Replicas.Count() + 1;
            return totalReplicasCount / 2 + 1;
        }

        private bool TryPutToOtherReplica(IPEndPoint endpoint, string id, Value value)
        {
            var internalClient = new InternalClient(string.Format("http://{0}/", endpoint));
            try
            {
                internalClient.Put(id, value);
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("Can't write to {0}: {1}", endpoint.Port, e.Message);
                return false;
            }
        }

        private bool TryPutToThisReplica(string id, Value value)
        {
            try
            {
                CheckState();
                storage.Set(id, value);
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("Can't write to {0}: {1}", configuration.CurrentNodePort, e.Message);
                return false;
            }
        }
    }
}