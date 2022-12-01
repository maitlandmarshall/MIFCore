using MIFCore.Hangfire.APIETL.Extract;
using static MIFCore.Hangfire.APIETL.Transform.ExtractDistinctGraphObjectSetsExtensions;

namespace MIFCore.Hangfire.APIETL.Transform
{
    public class TransformModelArgs : ResponseArgsBase
    {
        public TransformModelArgs(ApiEndpoint endpoint, ApiData apiData, TransformObjectArgs transformArgs) : base(endpoint, apiData)
        {
            this.Transform = transformArgs;
        }

        public TransformObjectArgs Transform { get; }
    }
}
