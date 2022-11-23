using System.Threading.Tasks;

namespace MIFCore.Hangfire.APIETL
{
    public interface IHandleResponse : IApiEndpointService
    {
        Task OnHandleResponse(HandleResponseArgs args);
    }
}