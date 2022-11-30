using System.Collections.Generic;

namespace MIFCore.Hangfire.APIETL.Load
{
    public class ApiEndpointModel
    {
        public string EndpointName { get; set; }
        public string DestinationName { get; set; }

        public string InputPath { get; set; }

        public IDictionary<string, ApiEndpointModelProperty> MappedProperties { get; } = new Dictionary<string, ApiEndpointModelProperty>();
    }
}
