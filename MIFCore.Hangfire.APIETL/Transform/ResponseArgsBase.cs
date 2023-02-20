using MIFCore.Hangfire.APIETL.Extract;

namespace MIFCore.Hangfire.APIETL.Transform
{
    public abstract class ResponseArgsBase
    {
        public ResponseArgsBase(ApiEndpoint endpoint, ApiData apiData, ExtractArgs extractArgs)
        {
            this.Endpoint = endpoint;
            this.ApiData = apiData;
            this.ExtractArgs = extractArgs;
        }

        public ApiData ApiData { get; }
        public ApiEndpoint Endpoint { get; }

        public ExtractArgs ExtractArgs { get; }
    }
}