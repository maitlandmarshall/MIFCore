using MIFCore.Hangfire.APIETL.Extract;

namespace MIFCore.Hangfire.APIETL.Transform
{
    public class HandleResponseArgs : ResponseArgsBase
    {
        public HandleResponseArgs(ApiEndpoint endpoint, ApiData apiData) : base(endpoint, apiData)
        {
        }
    }
}