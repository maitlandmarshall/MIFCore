using System.Collections.Generic;
using System.Threading.Tasks;

namespace MIFCore.Hangfire.APIETL.Load
{
    internal class ApiEndpointLoadJob : IApiEndpointLoadJob
    {
        private readonly IApiEndpointLoadPipeline loadPipeline;

        public ApiEndpointLoadJob(IApiEndpointLoadPipeline loadPipeline)
        {
            this.loadPipeline = loadPipeline;
        }

        public async Task Load(ApiEndpoint apiEndpoint, ApiEndpointModel model, List<IDictionary<string, object>> dataToLoad)
        {
            // Ensure the destination is created
            await this.loadPipeline.OnCreateDestination(new CreateDestinationArgs(apiEndpoint, model));

            // Now load the data into the destination
            await this.loadPipeline.OnLoadData(new LoadDataArgs(apiEndpoint, model, dataToLoad));
        }
    }
}
