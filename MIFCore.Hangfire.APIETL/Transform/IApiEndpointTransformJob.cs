using MIFCore.Hangfire.APIETL.Extract;
using System.Threading.Tasks;

namespace MIFCore.Hangfire.APIETL.Transform
{
    public interface IApiEndpointTransformJob
    {
        Task ExecuteTransformationJob(ApiEndpoint endpoint, ApiData apiData);
    }
}