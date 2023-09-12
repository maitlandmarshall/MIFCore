using System.Collections.Generic;
using System.Threading.Tasks;

namespace MIFCore.Hangfire.APIETL.Extract
{
    public interface IPrepareNextRequest : IApiEndpointService
    {
        Task<IDictionary<string, object>> OnPrepareNextRequest(PrepareNextRequestArgs args);
    }
}