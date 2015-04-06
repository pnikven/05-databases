using System.Linq;
using Client;
using Domain;
using Microsoft.Owin.Hosting;
using NUnit.Framework;
using SimpleStorage.Infrastructure;

namespace SimpleStorage.Tests.Controllers
{
    public class OperationLogControllerFunctionalTests : FuctionalTestBase
    {
        private const int port = 15000;
        private readonly string endpoint = string.Format("http://127.0.0.1:{0}/", port);
        private OperationLogClient operationLogClient;
        private SimpleStorageClient storageClient;

        public override void SetUp()
        {
            base.SetUp();

            var topology = new Topology(new int[0]);
            var configuration = new Configuration(topology) { CurrentNodePort = port, OtherShardsPorts = new int[0] };
            container.Configure(c => c.For<IConfiguration>().Use(configuration));

            storageClient = new SimpleStorageClient(endpoint);
            operationLogClient = new OperationLogClient(endpoint);
        }

        [Test]
        public void Read_Always_ShouldReturnAllOperations()
        {
            const string id = "id";
            var version1 = new Value { Content = "content", IsDeleted = false, Revision = 0 };
            using (GetStartedWebApp(port))
            {
                storageClient.Put(id, version1);
                var version2 = new Value { IsDeleted = true, Revision = 1, Content = "anotherContent" };
                storageClient.Put(id, version2);

                var actual = operationLogClient.Read(0, 100).ToArray();

                Assert.That(actual.Length, Is.EqualTo(2));
                Assert.That(actual[0].Id, Is.EqualTo(id));
                Assert.That(actual[0].Value.Content, Is.EqualTo(version1.Content));
                Assert.That(actual[0].Value.IsDeleted, Is.False);
                Assert.That(actual[1].Id, Is.EqualTo(id));
                Assert.That(actual[1].Value.IsDeleted, Is.True);
            }
        }

        [Test]
        public void Read_WithSeek_ShouldSkip()
        {
            using (GetStartedWebApp(port))
            {
                storageClient.Put("id1", new Value { Content = "1" });
                storageClient.Put("id2", new Value { Content = "2" });
                storageClient.Put("id3", new Value { Content = "3" });

                var actual = operationLogClient.Read(1, 1).ToArray();

                Assert.That(actual.Length, Is.EqualTo(1));
            }
        }

        [Test]
        public void Read_BigPosition_ShouldReturnEmpty()
        {
            using (GetStartedWebApp(port))
            {
                var actual = operationLogClient.Read(1000, 1).ToArray();
                Assert.That(actual.Length, Is.EqualTo(0));
            }
        }
    }
}