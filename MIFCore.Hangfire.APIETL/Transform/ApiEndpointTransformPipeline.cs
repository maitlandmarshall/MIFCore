using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MIFCore.Hangfire.APIETL.Transform
{
    internal class ApiEndpointTransformPipeline : IApiEndpointTransformPipeline
    {
        private readonly IEnumerable<IHandleResponse> handleResponses;
        private readonly IEnumerable<IParseResponse> parseResponses;
        private readonly IEnumerable<ITransformModel> transformModels;

        public ApiEndpointTransformPipeline(IEnumerable<IHandleResponse> handleResponses, IEnumerable<IParseResponse> parseResponses, IEnumerable<ITransformModel> transformModels)
        {
            this.handleResponses = handleResponses;
            this.parseResponses = parseResponses;
            this.transformModels = transformModels;
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

        public async Task<IEnumerable<IDictionary<string, object>>> OnParse(ParseResponseArgs args)
        {
            var relatedParseResponses = this.parseResponses
               .Where(y => y.RespondsToEndpointName(args.Endpoint.Name));

            foreach (var handleResponse in relatedParseResponses)
            {
                return await handleResponse.OnParse(args);
            }

            return null;
        }

        public async Task OnTransformModel(TransformModelArgs args)
        {
            var relatedTransformModels = this.transformModels
               .Where(y => y.RespondsToEndpointName(args.Endpoint.Name));

            foreach (var transformModel in relatedTransformModels)
            {
                await transformModel.OnTransformModel(args);
            }
        }
    }
}
