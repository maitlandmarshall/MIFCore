using MIFCore.Hangfire.APIETL.Extract;

namespace MIFCore.Hangfire.APIETL.Transform
{
    public class ParseResponseArgs : ResponseArgsBase
    {
        public ParseResponseArgs(ApiEndpoint endpoint, ApiData apiData, ExtractArgs extractArgs) : base(endpoint, apiData, extractArgs)
        {
        }
    }
}
