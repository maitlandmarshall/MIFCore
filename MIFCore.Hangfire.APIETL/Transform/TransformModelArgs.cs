using MIFCore.Hangfire.APIETL.Extract;
using static MIFCore.Hangfire.APIETL.Transform.ExtractDistinctGraphObjectSetsExtensions;

namespace MIFCore.Hangfire.APIETL.Transform
{
    public class TransformModelArgs : ResponseArgsBase
    {
        public TransformModelArgs(ApiEndpoint endpoint, ApiData apiData, ExtractArgs extractArgs, TransformObjectArgs transformArgs) : base(endpoint, apiData, extractArgs)
        {
            this.Transform = transformArgs;
        }

        public TransformObjectArgs Transform { get; }
    }
}
