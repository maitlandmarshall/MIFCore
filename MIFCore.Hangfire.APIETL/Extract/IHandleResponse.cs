using System.Threading.Tasks;

namespace MIFCore.Hangfire.APIETL.Extract
{
    public interface IHandleResponse : IApiEndpointService
    {
        Task OnHandleResponse(HandleResponseArgs args);
    }
}