using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Client;

namespace SimpleStorage.Infrastructure.Replication
{
    public class OperationLogSynchronizer : IOperationLogSynchronizer
    {
        private readonly IConfiguration configuration;
        private readonly IStorage storage;

        public OperationLogSynchronizer(IConfiguration configuration, IStorage storage)
        {
            this.configuration = configuration;
            this.storage = storage;
        }

        public Task Synchronize(CancellationToken cancellationToken)
        {
            Task synchronizationTask = Task.Factory.StartNew(() => SynchronizationAction(cancellationToken), cancellationToken);
            return synchronizationTask;
        }

        private void SynchronizationAction(CancellationToken token)
        {
            if (configuration.IsMaster)
                return;
            var masterEndpoint = string.Format("http://{0}/", configuration.MasterEndpoint);
            var position = 0;
            const int operationsToReadCount = 100;
            while (true)
            {
                token.ThrowIfCancellationRequested();
                var operationLogClient = new OperationLogClient(masterEndpoint);
                var operations = operationLogClient.Read(position, operationsToReadCount).ToArray();
                foreach (var operation in operations)
                {
                    storage.Set(operation.Id, operation.Value);
                }
                position += operations.Length;
                if (operations.Length < operationsToReadCount)
                    Thread.Sleep(1000);
            }
        }
    }
}