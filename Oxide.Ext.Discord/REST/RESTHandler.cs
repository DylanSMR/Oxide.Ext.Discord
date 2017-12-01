﻿namespace Oxide.Ext.Discord.REST
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Newtonsoft.Json;

    public class RESTHandler
    {
        public List<Bucket> Buckets = new List<Bucket>();

        private string apiKey;

        private Dictionary<string, string> headers;

        public RESTHandler(string apiKey)
        {
            this.apiKey = apiKey;

            headers = new Dictionary<string, string>()
            {
                { "Authorization", $"Bot {this.apiKey}" },
                { "Content-Type", "application/json" }
            };
        }

        public void Shutdown()
        {
            Buckets.ForEach(x => x.Disposed = true);
        }

        public void DoRequest(string url, RequestMethod method, object data, Action callback)
        {
            CreateRequest(method, url, headers, data, obj => callback?.Invoke());
        }

        public void DoRequest<T>(string url, RequestMethod method, object data, Action<object> callback)
        {
            CreateRequest(method, url, headers, data, response =>
            {
                var callbackObj = JsonConvert.DeserializeObject(response.Data, typeof(T));
                callback.Invoke(callbackObj);
            });
        }
        
        private void CreateRequest(RequestMethod method, string url, Dictionary<string, string> headers, object data, Action<RestResponse> callback)
        {
            // this is bad I know, but I'm way too fucking lazy to go 
            // and rewrite every single fucking REST request call
            string[] parts = url.Split('/');

            string route = string.Join("/", parts.Take(3).ToArray()).TrimEnd('/');

            string endpoint = "/" + string.Join("/", parts.Skip(3).ToArray());
            endpoint = endpoint.TrimEnd('/');
            
            var request = new Request(method, route, endpoint, headers, data, callback);
            BucketRequest(request, callback);
        }

        private void BucketRequest(Request request, Action<RestResponse> callback)
        {
            Buckets.ForEach(x =>
            {
                if (x.Disposed)
                {
                    Buckets.Remove(x);
                }
            });

            var bucket = Buckets.SingleOrDefault(x => x.Method == request.Method &&
                                                      x.Route == request.Route);

            if (bucket != null)
            {
                bucket.Queue(request);
                return;
            }

            var newBucket = new Bucket(request.Method, request.Route);
            Buckets.Add(newBucket);
            
            newBucket.Queue(request);
        }
    }
}