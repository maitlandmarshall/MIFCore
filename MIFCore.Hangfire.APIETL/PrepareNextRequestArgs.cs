using System.Collections.Generic;

namespace MIFCore.Hangfire.APIETL
{
    public class PrepareNextRequestArgs
    {
        public PrepareNextRequestArgs(ApiEndpoint endpoint, ApiData apiData, IDictionary<string, object> data)
        {
            this.Endpoint = endpoint;
            this.ApiData = apiData;
            this.Data = data ?? throw new System.ArgumentNullException(nameof(data));
        }

        public ApiEndpoint Endpoint { get; }
        public ApiData ApiData { get; }
        public IDictionary<string, object> Data { get; }
    }
}