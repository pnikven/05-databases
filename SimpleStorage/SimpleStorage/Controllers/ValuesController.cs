using System;
using System.Linq;
using System.Net;
using System.Web.Http;
using Client;
using Domain;
using SimpleStorage.Infrastructure;

namespace SimpleStorage.Controllers
{
    public class ValuesController : ApiController
    {
        private readonly IConfiguration configuration;
        private readonly IStateRepository stateRepository;
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
            var port = ChooseShardPort(id);
            if (port == configuration.CurrentNodePort)
            {
                CheckState();
                var result = storage.Get(id);
                if (result == null)
                    throw new HttpResponseException(HttpStatusCode.NotFound);
                return result;
            }
            return new SimpleStorageClient(string.Format("http://127.0.0.1:{0}/", port)).Get(id);
        }

        // PUT api/values/5
        public void Put(string id, [FromBody] Value value)
        {
            var port = ChooseShardPort(id);
            if (port == configuration.CurrentNodePort)
            {
                CheckState();
                storage.Set(id, value);
            }
            else
                new SimpleStorageClient(string.Format("http://127.0.0.1:{0}/", port)).Put(id, value);
        }

        private int ChooseShardPort(string id)
        {
            var allShardPorts = new[] { configuration.CurrentNodePort }
                .Concat(configuration.OtherShardsPorts.ToList())
                .OrderBy(x => x)
                .ToArray();
            var port = allShardPorts[Math.Abs(id.GetHashCode()) % allShardPorts.Length];
            return port;
        }
    }
}