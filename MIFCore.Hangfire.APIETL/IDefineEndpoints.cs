using System.Collections.Generic;

namespace MIFCore.Hangfire.APIETL
{
    public interface IDefineEndpoints : IApiEndpointService
    {
        IAsyncEnumerable<ApiEndpoint> DefineEndpoints(string endpointName);
    }
}