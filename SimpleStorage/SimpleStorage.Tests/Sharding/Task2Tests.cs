using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using Client;
using Domain;
using NUnit.Framework;

namespace SimpleStorage.Tests.Sharding
{
    [TestFixture]
    [Ignore]
    public class Task2Tests: ShardingTestBase
    {
        private static readonly string endpoint1 = string.Format("http://127.0.0.1:{0}/", 16000);
        private static readonly string endpoint2 = string.Format("http://127.0.0.1:{0}/", 16001);
        private static readonly string endpoint3 = string.Format("http://127.0.0.1:{0}/", 16002);

        [SetUp]
        public void SetUp()
        {
            using (var httpClient = new HttpClient())
                foreach (var endpoint in new[] { endpoint1, endpoint2, endpoint3 })
                {
                    using (var response =
                        httpClient.PostAsync(endpoint + "api/admin/removeAllData", new ByteArrayContent(new byte[0]))
                            .Result)
                        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
                }
        }

        [Test]
        public void Sharding_AllShards_ShouldContainSomeData()
        {
            var client = new SimpleStorageClient(endpoint1);
            for (var i = 0; i < 100; i++)
                client.Put(Guid.NewGuid().ToString(), new Value { Content = "content" });

            Assert.That(GetAll(endpoint1).ToArray(), Has.Length.GreaterThan(0));
            Assert.That(GetAll(endpoint2).ToArray(), Has.Length.GreaterThan(0));
            Assert.That(GetAll(endpoint3).ToArray(), Has.Length.GreaterThan(0));
        }

        [Test]
        public void Sharding_Always_ShouldReadAllData()
        {
            var client = new SimpleStorageClient(endpoint1);
            var items = new List<KeyValuePair<string, Value>>();
            for (var i = 0; i < 100; i++)
            {
                var id = Guid.NewGuid().ToString();
                var value = new Value { Content = "content" };
                items.Add(new KeyValuePair<string, Value>(id, value));
                client.Put(id, value);
            }

            foreach (var item in items)
                foreach (var endpoint in new[] { endpoint1, endpoint2, endpoint3 })
                {
                    var actual = new SimpleStorageClient(endpoint).Get(item.Key);
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