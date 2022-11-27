using System.Threading.Tasks;

namespace MIFCore.Hangfire.APIETL.Extract
{
    public interface IPrepareRequest : IApiEndpointService
    {
        Task OnPrepareRequest(PrepareRequestArgs args);
    }
}