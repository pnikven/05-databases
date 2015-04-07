using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Domain;

namespace Client
{
    public class SimpleStorageClient : ISimpleStorageClient
    {
        private readonly IEnumerable<string> endpoints;
        private readonly ICoordinatorClient coordinatorClient = new CoordinatorClient("http://127.0.0.1:17000/");

        public SimpleStorageClient(params string[] endpoints)
        {
            if (endpoints == null || !endpoints.Any())
                throw new ArgumentException("Empty endpoints!", "endpoints");
            this.endpoints = endpoints;
        }

        public void Put(string id, Value value)
        {
            var putUri = ChooseEndpointUsingCoordinator(id) + "api/values/" + id;
            using (var client = new HttpClient())
            using (var response = client.PutAsJsonAsync(putUri, value).Result)
                response.EnsureSuccessStatusCode();
        }

        public Value Get(string id)
        {
            var requestUri = ChooseEndpointUsingCoordinator(id) + "api/values/" + id;
            using (var client = new HttpClient())
            using (var response = client.GetAsync(requestUri).Result)
            {
                response.EnsureSuccessStatusCode();
                return response.Content.ReadAsAsync<Value>().Result;
            }
        }

        private string ChooseEndpointUsingCoordinator(string id)
        {
            return endpoints.ElementAt(coordinatorClient.Get(id));
        }
    }
}