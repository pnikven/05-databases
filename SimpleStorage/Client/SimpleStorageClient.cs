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
            foreach (var endpoint in endpoints)
            {
                var putUri = endpoint + "api/values/" + id;
                using (var client = new HttpClient())
                using (var response = client.PutAsJsonAsync(putUri, value).Result)
                    try
                    {
                        response.EnsureSuccessStatusCode();
                        return;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Can't put to endpoint {0}", endpoint);
                    }
            }
            throw new InvalidOperationException("Can't put to any endpoint");
        }

        public Value Get(string id)
        {
            foreach (var endpoint in endpoints)
            {
                var requestUri = endpoint + "api/values/" + id;
                using (var client = new HttpClient())
                using (var response = client.GetAsync(requestUri).Result)
                    try
                    {
                        response.EnsureSuccessStatusCode();
                        return response.Content.ReadAsAsync<Value>().Result;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Can't get from endpoint {0}", endpoint);
                    }
            }
            throw new InvalidOperationException("Can't get from any endpoint");
        }
    }
}