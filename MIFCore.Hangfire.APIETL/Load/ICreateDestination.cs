using System.Threading.Tasks;

namespace MIFCore.Hangfire.APIETL.Load
{
    public interface ICreateDestination : IApiEndpointService
    {
        Task OnCreateDestination(CreateDestinationArgs args);
    }
}
