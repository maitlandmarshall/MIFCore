using System.Collections.Generic;
using System.Threading.Tasks;

namespace MIFCore.Hangfire.APIETL
{
    internal interface IPrepareNextRequest : IApiEndpointService
    {
        Task<IDictionary<string, object>> OnPrepareNextRequest(PrepareNextRequestArgs args);
    }
}