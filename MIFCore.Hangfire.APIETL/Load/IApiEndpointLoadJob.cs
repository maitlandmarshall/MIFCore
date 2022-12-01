using System.Collections.Generic;
using System.Threading.Tasks;

namespace MIFCore.Hangfire.APIETL.Load
{
    public interface IApiEndpointLoadJob
    {
        Task Load(ApiEndpoint apiEndpoint, ApiEndpointModel model, List<IDictionary<string, object>> dataToLoad);
    }
}