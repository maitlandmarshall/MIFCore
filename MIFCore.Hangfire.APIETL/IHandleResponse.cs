using System.Threading.Tasks;

namespace MIFCore.Hangfire.APIETL
{
    internal interface IHandleResponse : IApiEndpointService
    {
        Task OnHandleResponse(HandleResponseArgs args);
    }
}