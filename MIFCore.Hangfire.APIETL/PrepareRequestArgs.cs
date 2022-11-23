using System.Collections.Generic;
using System.Net.Http;

namespace MIFCore.Hangfire.APIETL
{
    public class PrepareRequestArgs
    {
        public PrepareRequestArgs(HttpRequestMessage request, IDictionary<string, object> data)
        {
            this.Request = request;
            this.Data = data ?? new Dictionary<string, object>();
        }

        public HttpRequestMessage Request { get; set; }
        public IDictionary<string, object> Data { get; set; }
    }
}