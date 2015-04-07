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
            CheckState();
            var result = storage.Get(id);
            if (result == null)
                throw new HttpResponseException(HttpStatusCode.NotFound);
            return result;
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
                if (TryPutToOtherReplica(id, value, replicas.Current))
                    successfulWritesCount++;
            if (successfulWritesCount < quorum)
                throw new HttpResponseException(HttpStatusCode.InternalServerError);
        }

        private int GetQuorum()
        {
            var totalReplicasCount = configuration.Replicas.Count() + 1;
            return totalReplicasCount / 2 + 1;
        }

        private bool TryPutToOtherReplica(string id, Value value, IPEndPoint endpoint)
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