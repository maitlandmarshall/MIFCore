using System.Collections.Generic;

namespace MIFCore.Hangfire.APIETL.Extract
{
    public interface IApiEndpointAttributeFactory
    {
        IAsyncEnumerable<ApiEndpoint> Create();
    }
}