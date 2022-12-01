using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MIFCore.Hangfire.APIETL.Load
{
    internal class ApiEndpointLoadPipeline : IApiEndpointLoadPipeline
    {
        private readonly IEnumerable<ICreateDestination> createDestinations;
        private readonly IEnumerable<ILoadData> loadDatas;

        public ApiEndpointLoadPipeline(IEnumerable<ICreateDestination> createDestinations, IEnumerable<ILoadData> loadDatas)
        {
            this.createDestinations = createDestinations;
            this.loadDatas = loadDatas;
        }

        public async Task OnCreateDestination(CreateDestinationArgs args)
        {
            var relatedCreateDestination = this.createDestinations
                .LastOrDefault(y => y.RespondsToEndpointName(args.ApiEndpoint.Name) || y.IsDefaultResponder());

            if (relatedCreateDestination is null)
                return;

            await relatedCreateDestination.OnCreateDestination(args);
        }

        public async Task OnLoadData(LoadDataArgs args)
        {
            var relatedLoadDatas = this.loadDatas
                .Where(y => y.RespondsToEndpointName(args.ApiEndpoint.Name) || y.IsDefaultResponder())
                .ToList();

            foreach (var r in relatedLoadDatas)
            {
                await r.OnLoadData(args);
            }
        }
    }
}
