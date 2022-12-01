namespace MIFCore.Hangfire.APIETL.Load
{
    public class CreateDestinationArgs
    {
        public CreateDestinationArgs(ApiEndpointModel apiEndpointModel)
        {
            this.ApiEndpointModel = apiEndpointModel;
        }

        public ApiEndpointModel ApiEndpointModel { get; }
    }
}