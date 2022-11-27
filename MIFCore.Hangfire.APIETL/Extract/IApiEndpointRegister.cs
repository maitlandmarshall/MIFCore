using System.Collections.Generic;
using System.Threading.Tasks;

namespace MIFCore.Hangfire.APIETL.Extract
{
    public interface IApiEndpointRegister
    {
        IEnumerable<ApiEndpoint> Endpoints { get; }

        ApiEndpoint Get(string endpointName);
        Task Register();
        ApiEndpointRegister Register(ApiEndpoint endpoint);
    }
}