using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MIFCore.Hangfire.APIETL.Load
{
    public interface IGetDestinationType
    {
        Task<string> GetDestinationType(ApiEndpointModel apiEndpointModel, string sourceKey, IEnumerable<Type> sourceModelTypes);
    }
}
