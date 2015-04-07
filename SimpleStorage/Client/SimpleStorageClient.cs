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

        public SimpleStorageClient(params string[] endpoints)
        {
            if (endpoints == null || !endpoints.Any())
                throw new ArgumentException("Empty endpoints!", "endpoints");
            this.endpoints = endpoints;
        }

        public void Put(string id, Value value)
        {
            var putUri = ChooseEndpoint(id) + "api/values/" + id;
            using (var client = new HttpClient())
            using (var response = client.PutAsJsonAsync(putUri, value).Result)
                response.EnsureSuccessStatusCode();
        }

        public Value Get(string id)
        {
            var requestUri = ChooseEndpoint(id) + "api/values/" + id;
            using (var client = new HttpClient())
            using (var response = client.GetAsync(requestUri).Result)
            {
                response.EnsureSuccessStatusCode();
                return response.Content.ReadAsAsync<Value>().Result;
            }
        }

        public string ChooseEndpoint(string id)
        {
            return endpoints.ElementAt(Math.Abs(id.GetHashCode()) % endpoints.Count());
        }
    }
}