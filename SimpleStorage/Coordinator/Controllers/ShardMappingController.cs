using System;
using System.Web.Http;

namespace Coordinator.Controllers
{
    public class ShardMappingController : ApiController
    {
        private readonly IConfiguration configuration;
        public ShardMappingController(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public int Get(string id)
        {
            return Math.Abs(id.GetHashCode()) % configuration.ShardCount;
        }
    }
}