using MIFCore.Hangfire.APIETL.Load;
using System.Collections.Generic;
using System.Linq;

namespace MIFCore.Hangfire.APIETL
{
    internal class ApiEndpointFactory : IApiEndpointFactory
    {
        private readonly IEnumerable<ApiEndpointAttribute> endpointNameAttributes;
        private readonly IEnumerable<ApiEndpointModel> apiEndpointModels;
        private readonly IEnumerable<IDefineEndpoints> endpointDefiners;

        public ApiEndpointFactory(
            IEnumerable<ApiEndpointAttribute> apiEndpointAttributes,
            IEnumerable<ApiEndpointModel> apiEndpointModels,
            IEnumerable<IDefineEndpoints> endpointDefiners)
        {
            this.endpointNameAttributes = apiEndpointAttributes;
            this.apiEndpointModels = apiEndpointModels;
            this.endpointDefiners = endpointDefiners;
        }

        public async IAsyncEnumerable<ApiEndpoint> Create()
        {
            // Get the endpoints defined by the ApiEndpoint or by the ApiEndpointModels apis
            var endpointsDefinedByAttributes = this.endpointNameAttributes
                .Select(y => y.EndpointName)
                .Union(this.apiEndpointModels.Select(x => x.EndpointName))
                .Distinct();

            // Create ApiEndpoint records from the ApiEndpointAttribute
            foreach (var endpointName in endpointsDefinedByAttributes)
            {
                var endpointDefiners = this.GetEndpointDefiners(endpointName);

                // If the endpoint corresponds to an IDefineEndpoint, then the endpoints will be created / transformed from it instead.
                if (endpointDefiners.Any())
                {
                    // It is possible for the endpointDefiner to create multiple endpoints from a single name (such as when tenancy is used, same endpoint, different tenant)
                    foreach (var ed in endpointDefiners)
                    {
                        var apiEndpoints = ed.DefineEndpoints(endpointName);

                        await foreach (var ep in apiEndpoints)
                        {
                            // Ensure the endpoint name is set as this is an internal field
                            ep.Name = endpointName;

                            yield return ep;
                        }
                    }
                }

                // Otherwise create a single endpoint from the name, using all defaults
                else
                {
                    yield return new ApiEndpoint(endpointName);
                }
            }
        }

        private IEnumerable<IDefineEndpoints> GetEndpointDefiners(string endpointName)
        {
            foreach (var ed in this.endpointDefiners)
            {
                if (ed.RespondsToEndpointName(endpointName))
                    yield return ed;
            }
        }
    }
}
