using System.Collections.Generic;
using System.Linq;
using System.Net;
using SimpleStorage.Infrastructure;

namespace SimpleStorage
{
    public class Configuration : IConfiguration
    {
        private readonly ITopology topology;
        public Configuration(ITopology topology)
        {
            this.topology = topology;
            IsMaster = !topology.Replicas.Any();
            if (!IsMaster)
                MasterEndpoint = topology.Replicas.First();
        }

        public bool IsMaster { get; private set; }
        public IPEndPoint MasterEndpoint { get; private set; }

        public IEnumerable<IPEndPoint> Replicas
        {
            get { return topology.Replicas; }
        }

        public int CurrentNodePort { get; set; }
        public int[] OtherShardsPorts { get; set; }
    }
}