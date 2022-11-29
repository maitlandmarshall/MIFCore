using System.Collections.Generic;
using System.Threading.Tasks;

namespace MIFCore.Hangfire.APIETL
{
    public interface IApiEndpointRegister
    {
        IEnumerable<ApiEndpoint> Endpoints { get; }

        ApiEndpoint Get(string endpointName);
        Task Register();
        IApiEndpointRegister Register(ApiEndpoint endpoint);
    }
}