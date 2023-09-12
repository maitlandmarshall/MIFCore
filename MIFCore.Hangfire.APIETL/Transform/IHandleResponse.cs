using System.Threading.Tasks;

namespace MIFCore.Hangfire.APIETL.Transform
{
    public interface IHandleResponse : IApiEndpointService
    {
        Task OnHandleResponse(HandleResponseArgs args);
    }
}