using System.Threading.Tasks;

namespace MIFCore.Hangfire.APIETL
{
    public interface IPrepareRequest : IApiEndpointService
    {
        Task OnPrepareRequest(PrepareRequestArgs args);
    }
}