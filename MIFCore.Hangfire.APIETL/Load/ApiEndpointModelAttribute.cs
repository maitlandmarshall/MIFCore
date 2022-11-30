using System;

namespace MIFCore.Hangfire.APIETL.Load
{
    public class ApiEndpointModelAttribute : Attribute
    {
        public ApiEndpointModelAttribute(string endpointName)
        {
            this.EndpointName = endpointName;
        }

        public string EndpointName { get; }
        public string ModelPath { get; set; }
    }
}
