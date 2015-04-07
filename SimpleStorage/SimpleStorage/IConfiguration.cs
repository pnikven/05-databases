using System.Collections.Generic;
using System.Net;

namespace SimpleStorage
{
    public interface IConfiguration
    {
        int CurrentNodePort { get; }
        int[] OtherShardsPorts { get; }
        bool IsMaster { get; }
        IPEndPoint MasterEndpoint { get; }
        IEnumerable<IPEndPoint> Replicas { get; }

    }
}