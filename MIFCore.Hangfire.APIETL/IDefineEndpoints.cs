using System.Collections.Generic;

namespace MIFCore.Hangfire.APIETL
{
    public interface IDefineEndpoints
    {
        IAsyncEnumerable<ApiEndpoint> DefineEndpoints(string endpointName);
    }
}