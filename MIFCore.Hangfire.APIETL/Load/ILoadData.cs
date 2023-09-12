using System.Threading.Tasks;

namespace MIFCore.Hangfire.APIETL.Load
{
    public interface ILoadData : IApiEndpointService
    {
        public Task OnLoadData(LoadDataArgs args);
    }
}
