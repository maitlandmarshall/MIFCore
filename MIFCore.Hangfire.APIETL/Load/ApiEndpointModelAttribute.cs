using System;

namespace MIFCore.Hangfire.APIETL.Load
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class ApiEndpointModelAttribute : Attribute
    {
        public ApiEndpointModelAttribute(string endpointName)
        {
            this.EndpointName = endpointName;
        }

        public ApiEndpointModelAttribute(string endpointName, string destinationName)
        {
            this.EndpointName = endpointName;
            this.DestinationName = destinationName;
        }

        public string EndpointName { get; }
        public string DestinationName { get; }

        public string InputPath { get; set; }
    }
}
