using System;
using System.Net;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;

using HttpBroom.Core.Records;

namespace HttpBroom.Core
{
    /// <summary>
    /// Entry point to <see cref="HttpBroom"/>.
    /// </summary>
    public class HttpBroom : IDisposable
    {
        private readonly HttpClientHandler _httpClientHandler;
        private readonly HttpClient _httpClient;
        
        /// <summary>
        /// Current <see cref="HttpBroom"/> instance ports.
        /// </summary>
        public readonly List<int> Ports;
        
        /// <summary>
        /// Current <see cref="HttpBroom"/> instance proxies.
        /// </summary>
        public readonly List<WebProxy> Proxies;
        
        /// <summary>
        /// If current <see cref="HttpBroom"/> instance will ignore SSL/TLS.
        /// </summary>
        public readonly bool IgnoreTls;
        
        /// <summary>
        /// If current <see cref="HttpBroom"/> instance will follow redirects.
        /// </summary>
        public readonly bool FollowRedirect;
        
        /// <summary>
        /// If current <see cref="HttpBroom"/> instance follow redirects (check <see cref="FollowRedirect"/>), the
        /// limit of automatic following.
        /// </summary>
        public readonly int MaxFollowRedirect;
        
        /// <summary>
        /// Current <see cref="HttpBroom"/> instance fixed headers to every request.
        /// </summary>
        public readonly Dictionary<string, string> FixedHeaders;

        /// <summary>
        /// Default constructor for <see cref="HttpBroom"/>.
        /// </summary>
        /// <param name="ports">Ports to test.</param>
        /// <param name="proxies">List of proxies to be utilized.</param>
        /// <param name="ignoreTls">If the client will ignore any SSL/TLS errors.</param>
        /// <param name="followRedirect">If the client will follow automatic redirects.</param>
        /// <param name="maxFollowRedirect">If <see cref="followRedirect"/> is active, the limit of times to accept
        /// automatic redirection.</param>
        /// <param name="fixedHeaders">Fixed headers added to every request.</param>
        public HttpBroom(List<int> ports, List<WebProxy> proxies, bool ignoreTls, bool followRedirect, 
            int maxFollowRedirect, Dictionary<string, string> fixedHeaders)
        {
            Ports = ports;
            Proxies = proxies;
            IgnoreTls = ignoreTls;
            FollowRedirect = followRedirect;
            MaxFollowRedirect = maxFollowRedirect;
            FixedHeaders = fixedHeaders;

            _httpClientHandler = new HttpClientHandler
            {
                AllowAutoRedirect = followRedirect,
                MaxAutomaticRedirections = maxFollowRedirect
            };

            if (ignoreTls)
            {
                _httpClientHandler.ServerCertificateCustomValidationCallback = 
                    (message, cert, chain, errors) => true;
            }
            
            _httpClient = new HttpClient(_httpClientHandler);
        }

        public async Task<List<FlyoverResponseMessage>> ValidateWithGetAsync(string target)
        {
            var flyoverResponses = new List<FlyoverResponseMessage>();
            
            var validationTasks = Ports
                .Select(p => ValidateWithGetHelperAsync(new Uri($"{target}:{p}")))
                .ToList();

            while (validationTasks.Any())
            {
               var finishedTask = await Task.WhenAny(validationTasks);
               validationTasks.Remove(finishedTask);
               
               flyoverResponses.Add(await finishedTask);
            }

            return flyoverResponses;
        }

        private async Task<FlyoverResponseMessage> ValidateWithGetHelperAsync(Uri target)
        {
            var message = new 
                HttpRequestMessage(HttpMethod.Get, target ?? throw new ArgumentNullException(nameof(target)));

            if (FixedHeaders.Any())
            {
                foreach (var header in FixedHeaders)
                {
                    message.Headers.Add(header.Key, header.Value);
                }
            }

            try
            {
                var response = await _httpClient.SendAsync(message);
            
                return new FlyoverResponseMessage
                {
                    Successful = response.IsSuccessStatusCode,
                    StatusCode = response.StatusCode,
                    Headers = response.Headers,
                    Target = target,
                    Content = response.Content
                };
            }
            catch (Exception e)
            {
                return new FlyoverResponseMessage
                {
                    Successful = false,
                    ErrorMessage = e.Message
                };
            }
        }

        public void Dispose()
        {
            _httpClientHandler?.Dispose();
            _httpClient?.Dispose();
        }
    }
}