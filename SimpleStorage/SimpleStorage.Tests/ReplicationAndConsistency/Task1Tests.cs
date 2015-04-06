using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Client;
using Domain;
using NUnit.Framework;
using NUnit.Framework.Constraints;

namespace SimpleStorage.Tests.ReplicationAndConsistency
{
    [TestFixture]
    //[Ignore]
    public class Task1Tests : FuctionalTestBase
    {
        [TestFixtureSetUp]
        public override void TestFixtureSetUp()
        {
            base.TestFixtureSetUp();
            Program.ShouldRunInfinitely = true;
            var masterTask = new Task(() => Program.Main(new[] { "-p", "16000" }));
            masterTask.Start();
            var slave1Task = new Task(() => Program.Main(new[] { "-p", "16001", "--rp", "16000" }));
            slave1Task.Start();
            var slave2Task = new Task(() => Program.Main(new[] { "-p", "16002", "--rp", "16000" }));
            slave2Task.Start();
        }

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            masterClient = new SimpleStorageClient(masterEndpoint);
            slave1Client = new SimpleStorageClient(slave1Endpoint);
            slave2Client = new SimpleStorageClient(slave2Endpoint);
            fullTopologyClient = new SimpleStorageClient(masterEndpoint, slave1Endpoint, slave2Endpoint);
            using (var httpClient = new HttpClient())
            {
                ActionOnReplica(slave1Endpoint, httpClient, "start");
                ActionOnReplica(slave2Endpoint, httpClient, "start");
                ActionOnReplica(masterEndpoint, httpClient, "start");
            }
        }

        private readonly string masterEndpoint = "http://127.0.0.1:16000/";
        private readonly string slave1Endpoint = "http://127.0.0.1:16001/";
        private readonly string slave2Endpoint = "http://127.0.0.1:16002/";
        private SimpleStorageClient masterClient;
        private SimpleStorageClient slave1Client;
        private SimpleStorageClient slave2Client;
        private SimpleStorageClient fullTopologyClient;

        private void TestReplicaDown(string replica, string id)
        {
            using (var httpClient = new HttpClient())
            {
                ActionOnReplica(replica, httpClient, "stop");

                for (int i = 0; i < 10; ++i)
                {
                    fullTopologyClient.Get(id);
                }

                ActionOnReplica(replica, httpClient, "start");
            }
        }

        private static void ActionOnReplica(string replica, HttpClient httpClient, string action)
        {
            using (
                HttpResponseMessage response =
                    httpClient.PostAsync(replica + "api/admin/" + action, new ByteArrayContent(new byte[0])).Result)
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        }

        private static Constraint CheckHttpException(HttpStatusCode code)
        {
            return Is.TypeOf<HttpRequestException>().And.Property("Message").ContainsSubstring(((int)code).ToString());
        }

        [Test]
        public void Get_ManyTimes_ShouldWorkWhenReplicasDown()
        {
            string id = Guid.NewGuid().ToString();
            var value = new Value { Content = "content" };
            fullTopologyClient.Put(id, value);

            for (int i = 0; i < 100; ++i)
            {
                fullTopologyClient.Get(id);
            }
            Thread.Sleep(2000);
            TestReplicaDown(slave1Endpoint, id);
            TestReplicaDown(slave2Endpoint, id);
            TestReplicaDown(masterEndpoint, id);
        }

        [Test]
        public void Put_ManyTimes_ShouldWorkWithoutException()
        {
            for (int i = 0; i < 100; ++i)
            {
                string id = Guid.NewGuid().ToString();
                var value = new Value { Content = "content" };
                fullTopologyClient.Put(id, value);
            }
        }

        [Test]
        public void Put_OnMaster_ShouldAvailableOnSlaves()
        {
            string id = Guid.NewGuid().ToString();
            var value = new Value { Content = "content" };
            masterClient.Put(id, value);
            Thread.Sleep(2000);
            Value value1 = slave1Client.Get(id);
            Assert.That(value1.Content, Is.EqualTo("content"));
            Value value2 = slave2Client.Get(id);
            Assert.That(value2.Content, Is.EqualTo("content"));
        }

        [Test]
        public void Put_OnMaster_ShouldAvailableOnMaster()
        {
            string id = Guid.NewGuid().ToString();
            var value = new Value { Content = "content" };
            masterClient.Put(id, value);
            Value value1 = masterClient.Get(id);
            Assert.That(value1.Content, Is.EqualTo("content"));
        }

        [Test]
        public void Put_OnSlaves_ShouldThrow()
        {
            string id = Guid.NewGuid().ToString();
            var value = new Value { Content = "content" };
            Assert.Throws(CheckHttpException(HttpStatusCode.NotImplemented), () => slave1Client.Put(id, value));
            Assert.Throws(CheckHttpException(HttpStatusCode.NotImplemented), () => slave2Client.Put(id, value));
            masterClient.Put(id, value);
        }
    }
}