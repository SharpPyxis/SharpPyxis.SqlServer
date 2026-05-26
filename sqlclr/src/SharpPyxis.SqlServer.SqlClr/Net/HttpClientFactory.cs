using System;
using System.Net;
using System.Net.Http;

namespace SharpPyxis.SqlServer.SqlClr.Net
{
    internal static class HttpClientFactory
    {
        private static readonly Lazy<HttpClient> _client =
            new Lazy<HttpClient>(() =>
            {
                try
                {
                    ServicePointManager.SecurityProtocol =
                        SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;
                }
                catch
                {
                    // TLS 1.3 enum value absent on some .NET 4.8 builds; fall back to TLS 1.2 only
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                }

                var handler = new HttpClientHandler()
                {
                    AllowAutoRedirect = false,
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                    UseCookies = false
                };

                var client = new HttpClient(handler, true)
                {
                    Timeout = TimeSpan.FromSeconds(100)
                };
                client.DefaultRequestHeaders.ConnectionClose = false;
                return client;
            });

        public static HttpClient Client
        {
            get { return _client.Value; }
        }
    }
}
