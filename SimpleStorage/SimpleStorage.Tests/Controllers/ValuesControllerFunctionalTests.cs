using System.Net;
using System.Net.Http;
using Client;
using Domain;
using Microsoft.Owin.Hosting;
using NUnit.Framework;
using SimpleStorage.Infrastructure;

namespace SimpleStorage.Tests.Controllers
{
    public class ValuesControllerFunctionalTests : FuctionalTestBase
    {
        private const int port = 15000;
        private readonly string endpoint = string.Format("http://127.0.0.1:{0}/", port);
        private SimpleStorageClient client;

        public override void SetUp()
        {
            base.SetUp();
            client = new SimpleStorageClient(endpoint);

            var topology = new Topology(new int[0]);
            var configuration = new Configuration(topology) { CurrentNodePort = port, OtherShardsPorts = new int[0] };
            container.Configure(c => c.For<IConfiguration>().Use(configuration));
        }

        [Test]
        public void Get_KnownId_ShouldReturnValue()
        {
            const string id = "id";
            var value = new Value { Content = "content" };

            using (GetStartedWebApp(port))
            {
                client.Put(id, value);
                var actual = client.Get(id);
                Assert.That(actual.Content, Is.EqualTo(value.Content));
            }
        }

        [Test]
        public void Get_StopInstance_ShouldThrow()
        {
            using (GetStartedWebApp(port))
            {
                using (var httpClient = new HttpClient())
                {
                    using (
                        var response =
                            httpClient.PostAsync(endpoint + "api/admin/stop", new ByteArrayContent(new byte[0])).Result)
                        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

                    using (var response = httpClient.GetAsync(endpoint + "api/values/id").Result)
                        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
                }
            }
        }

        [Test]
        public void Get_StartInstance_ShouldNotThrow()
        {
            using (GetStartedWebApp(port))
            {
                client.Put("id", new Value());
                using (var httpClient = new HttpClient())
                {
                    using (
                        var response =
                            httpClient.PostAsync(endpoint + "api/admin/stop", new ByteArrayContent(new byte[0])).Result)
                        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

                    using (
                        var response =
                            httpClient.PostAsync(endpoint + "api/admin/start", new ByteArrayContent(new byte[0])).Result
                        )
                        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

                    using (var response = httpClient.GetAsync(endpoint + "api/values/id").Result)
                        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                }
            }
        }

        [Test]
        public void Get_UnknownId_ShouldReturnNotFound()
        {
            var requestUri = endpoint + "api/values/unknownId";

            using (GetStartedWebApp(port))
            using (var httpClient = new HttpClient())
            using (var response = httpClient.GetAsync(requestUri).Result)
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }
    }
}