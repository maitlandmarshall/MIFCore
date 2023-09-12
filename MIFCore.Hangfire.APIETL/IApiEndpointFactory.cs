using System.Collections.Generic;

namespace MIFCore.Hangfire.APIETL
{
    public interface IApiEndpointFactory
    {
        IAsyncEnumerable<ApiEndpoint> Create();
    }
}