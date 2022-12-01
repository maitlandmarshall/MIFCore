using MIFCore.Hangfire.APIETL.Extract;

namespace MIFCore.Hangfire.APIETL.Transform
{
    public abstract class ResponseArgsBase
    {
        public ResponseArgsBase(ApiEndpoint endpoint, ApiData apiData)
        {
            this.Endpoint = endpoint;
            this.ApiData = apiData;
        }

        public ApiData ApiData { get; }
        public ApiEndpoint Endpoint { get; }
    }
}