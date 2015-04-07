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
            var putUri = endpoints.First() + "api/values/" + id;
            using (var client = new HttpClient())
            using (var response = client.PutAsJsonAsync(putUri, value).Result)
                response.EnsureSuccessStatusCode();
        }

        public Value Get(string id)
        {
            foreach (var endpoint in endpoints)
            {
                var requestUri = endpoint + "api/values/" + id;
                Value value;
                if (TryGetResponse(requestUri, out value))
                    return value;
            }
            throw new HttpRequestException();
        }

        private static bool TryGetResponse(string requestUri, out Value value)
        {
            try
            {
                using (var client = new HttpClient())
                using (var response = client.GetAsync(requestUri).Result)
                {
                    response.EnsureSuccessStatusCode();
                    value = response.Content.ReadAsAsync<Value>().Result;
                    return true;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Can't get response from {0}", requestUri);
                value = null;
                return false;
            }
        }
    }
}