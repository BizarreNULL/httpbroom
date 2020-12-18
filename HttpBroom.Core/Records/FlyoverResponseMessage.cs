using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

namespace HttpBroom.Core.Records
{
    public class FlyoverResponseMessage
    {
        public bool Successful { get; set; }
        public string ErrorMessage { get; set; }
        public Uri Target { get; set; }
        public HttpStatusCode StatusCode { get; set; }
        public HttpResponseHeaders Headers { get; set; }
        public HttpContent Content { get; set; }
    }
}