namespace MIFCore.Hangfire.APIETL.Load
{
    public class CreateDestinationArgs
    {
        public CreateDestinationArgs(ApiEndpoint apiEndpoint, ApiEndpointModel apiEndpointModel)
        {
            this.ApiEndpoint = apiEndpoint;
            this.ApiEndpointModel = apiEndpointModel;
        }

        public ApiEndpoint ApiEndpoint { get; }
        public ApiEndpointModel ApiEndpointModel { get; }
    }
}