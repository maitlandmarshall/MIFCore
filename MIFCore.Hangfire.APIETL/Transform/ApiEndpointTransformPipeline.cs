using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MIFCore.Hangfire.APIETL.Transform
{
    internal class ApiEndpointTransformPipeline : IApiEndpointTransformPipeline
    {
        private readonly IEnumerable<IHandleResponse> handleResponses;

        public ApiEndpointTransformPipeline(IEnumerable<IHandleResponse> handleResponses)
        {
            this.handleResponses = handleResponses;
        }

        public async Task OnHandleResponse(HandleResponseArgs args)
        {
            var relatedHandleResponses = this.handleResponses
               .Where(y => y.RespondsToEndpointName(args.Endpoint.Name));

            foreach (var handleResponse in relatedHandleResponses)
            {
                await handleResponse.OnHandleResponse(args);
            }
        }
    }
}
