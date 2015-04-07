using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Client;
using Domain;

namespace SimpleStorage.Infrastructure.Replication
{
    public class OperationLogSynchronizer : IOperationLogSynchronizer
    {
        private readonly IConfiguration configuration;
        private readonly IStorage storage;
        private const int operationsToReadByTimeCount = 100;

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
            while (true)
            {
                token.ThrowIfCancellationRequested();

                Operation[] operations;
                if (TryReadOperationsFromEndpointLog(masterEndpoint, position, out operations))
                {
                    foreach (var operation in operations)
                        storage.Set(operation.Id, operation.Value);
                    position += operations.Length;
                }
                if (operations == null || operations.Length < operationsToReadByTimeCount)
                    Thread.Sleep(1000);
            }
        }

        private bool TryReadOperationsFromEndpointLog(string endpoint, int position, out Operation[] operations)
        {
            try
            {
                var operationLogClient = new OperationLogClient(endpoint);
                operations = operationLogClient.Read(position, operationsToReadByTimeCount).ToArray();
                return true;
            }
            catch
            {
                operations = null;
                return false;
            }
        }
    }
}