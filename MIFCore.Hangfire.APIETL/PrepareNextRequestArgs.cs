using System.Collections.Generic;

namespace MIFCore.Hangfire.APIETL
{
    public class PrepareNextRequestArgs
    {
        public PrepareNextRequestArgs(ApiData apiData, IDictionary<string, object> data)
        {
            this.ApiData = apiData;
            this.Data = data ?? new Dictionary<string, object>();
        }

        public ApiData ApiData { get; }
        public IDictionary<string, object> Data { get; }
    }
}