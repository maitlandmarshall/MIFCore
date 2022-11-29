using System.Collections.Generic;

namespace MIFCore.Hangfire.APIETL.Extract
{
    public interface IApiEndpointFactory
    {
        IAsyncEnumerable<ApiEndpoint> Create();
    }
}