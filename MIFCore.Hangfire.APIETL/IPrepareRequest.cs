using System.Threading.Tasks;

namespace MIFCore.Hangfire.APIETL
{
    public interface IPrepareRequest
    {
        Task OnPrepareRequest(PrepareRequestArgs args);
    }
}