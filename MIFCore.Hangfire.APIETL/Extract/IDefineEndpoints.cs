using System.Collections.Generic;

namespace MIFCore.Hangfire.APIETL.Extract
{
    public interface IDefineEndpoints : IApiEndpointService
    {
        IAsyncEnumerable<ApiEndpoint> DefineEndpoints(string endpointName);
    }
}