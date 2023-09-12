using System.Threading.Tasks;

namespace MIFCore.Hangfire.APIETL.Transform
{
    public interface ITransformModel : IApiEndpointService
    {
        Task OnTransformModel(TransformModelArgs args);
    }
}
