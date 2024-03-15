using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Parse.Service
{
    public static class HttpClientFactory
    {
        private static readonly Lazy<HttpClient> _httpClient = new Lazy<HttpClient>(() =>
        {
            var handler = new SocketsHttpHandler();
            handler.PooledConnectionLifetime = TimeSpan.FromSeconds(10); 
            handler.PooledConnectionIdleTimeout = TimeSpan.FromSeconds(100);
            handler.ConnectTimeout = TimeSpan.FromSeconds(10);
            handler.MaxConnectionsPerServer = 10;
            var httpClient = new HttpClient(handler);
            
            return httpClient;
        });

        public static HttpClient Instance => _httpClient.Value;
    }
}
