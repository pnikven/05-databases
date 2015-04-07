using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using Client;
using Domain;
using NUnit.Framework;
using System.Threading.Tasks;

namespace SimpleStorage.Tests.Sharding
{
    [TestFixture]
    //[Ignore]
    public class Task1Tests
    {
        protected static bool RunServersFromTests = true;

        private static readonly string endpoint1 = string.Format("http://127.0.0.1:{0}/", 16000);
        private static readonly string endpoint2 = string.Format("http://127.0.0.1:{0}/", 16001);
        private static readonly string endpoint3 = string.Format("http://127.0.0.1:{0}/", 16002);
        private readonly string[] endpoints = { endpoint1, endpoint2, endpoint3 };
        private SimpleStorageClient client;

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            if (RunServersFromTests)
            {
                Program.ShouldRunInfinitely = true;
                var masterTask = new Task(() => Program.Main(new[] { "-p", "16000", "--sp", "16001,16002" }));
                masterTask.Start();
                var slave1Task = new Task(() => Program.Main(new[] { "-p", "16001", "--sp", "16000,16002" }));
                slave1Task.Start();
                var slave2Task = new Task(() => Program.Main(new[] { "-p", "16002", "--sp", "16000,16001" }));
                slave2Task.Start();
            }
        }

        [SetUp]
        public void SetUp()
        {
            client = new SimpleStorageClient(endpoints);

            using (var httpClient = new HttpClient())
                foreach (var endpoint in endpoints)
                {
                    using (var response =
                        httpClient.PostAsync(endpoint + "api/admin/removeAllData", new ByteArrayContent(new byte[0]))
                            .Result)
                        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
                }
        }

        [Test]
        public void Sharding_EachShard_ShouldNotContainAllData()
        {
            for (var i = 0; i < 100; i++)
                client.Put(Guid.NewGuid().ToString(), new Value { Content = "content" });

            Assert.That(GetAll(endpoint1).ToArray(), Has.Length.LessThan(100));
            Assert.That(GetAll(endpoint2).ToArray(), Has.Length.LessThan(100));
            Assert.That(GetAll(endpoint3).ToArray(), Has.Length.LessThan(100));
        }

        [Test]
        public void Sharding_AllShards_ShouldContainSomeData()
        {
            for (var i = 0; i < 100; i++)
                client.Put(Guid.NewGuid().ToString(), new Value { Content = "content" });

            Assert.That(GetAll(endpoint1).ToArray(), Has.Length.GreaterThan(0));
            Assert.That(GetAll(endpoint2).ToArray(), Has.Length.GreaterThan(0));
            Assert.That(GetAll(endpoint3).ToArray(), Has.Length.GreaterThan(0));
        }

        [Test]
        public void Sharding_Always_ShouldSaveAllData()
        {
            var items = new List<KeyValuePair<string, Value>>();
            for (var i = 0; i < 100; i++)
            {
                var id = Guid.NewGuid().ToString();
                var value = new Value { Content = "content" };
                items.Add(new KeyValuePair<string, Value>(id, value));
                client.Put(id, value);
            }

            foreach (var item in items)
            {
                var actual = client.Get(item.Key);
                Assert.That(actual.Content, Is.EqualTo(item.Value.Content));
                Assert.That(actual.IsDeleted, Is.EqualTo(item.Value.IsDeleted));
                Assert.That(actual.Revision, Is.EqualTo(item.Value.Revision));
            }
        }

        private IEnumerable<ValueWithId> GetAll(string endpoint)
        {
            var requestUri = endpoint + "api/admin/getAllLocalData";
            using (var httpClient = new HttpClient())
            using (var response = httpClient.GetAsync(requestUri).Result)
            {
                response.EnsureSuccessStatusCode();
                return response.Content.ReadAsAsync<IEnumerable<ValueWithId>>().Result;
            }
        }
    }
}