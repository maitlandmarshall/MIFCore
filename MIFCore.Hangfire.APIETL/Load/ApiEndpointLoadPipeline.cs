using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MIFCore.Hangfire.APIETL.Load
{
    internal class ApiEndpointLoadPipeline : IApiEndpointLoadPipeline
    {
        private readonly IEnumerable<ICreateDestination> createDestinations;

        public ApiEndpointLoadPipeline(IEnumerable<ICreateDestination> createDestinations)
        {
            this.createDestinations = createDestinations;
        }

        public async Task OnCreateDestination(CreateDestinationArgs args)
        {
            var relatedCreateDestination = this.createDestinations
                .LastOrDefault(y => y.RespondsToEndpointName(args.ApiEndpoint.Name) || y.IsDefaultResponder());

            if (relatedCreateDestination is null)
                return;

            await relatedCreateDestination.OnCreateDestination(args);
        }
    }
}
