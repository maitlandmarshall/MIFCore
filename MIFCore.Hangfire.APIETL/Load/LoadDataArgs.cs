using System.Collections.Generic;

namespace MIFCore.Hangfire.APIETL.Load
{
    public class LoadDataArgs
    {
        public LoadDataArgs(ApiEndpoint apiEndpoint, ApiEndpointModel apiEndpointModel, List<IDictionary<string, object>> dataToLoad)
        {
            this.ApiEndpoint = apiEndpoint;
            this.ApiEndpointModel = apiEndpointModel;
            this.DataToLoad = dataToLoad;
        }

        public ApiEndpoint ApiEndpoint { get; }
        public ApiEndpointModel ApiEndpointModel { get; }
        public List<IDictionary<string, object>> DataToLoad { get; }
    }
}
