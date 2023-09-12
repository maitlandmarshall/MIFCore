using System;

namespace MIFCore.Hangfire.APIETL
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class ApiEndpointAttribute : Attribute
    {
        public ApiEndpointAttribute(string endpointName)
        {
            this.EndpointName = endpointName;
        }

        public ApiEndpointAttribute(string endpointName, string inputPath)
        {
            this.EndpointName = endpointName;
            this.InputPath = inputPath;
        }

        public string EndpointName { get; }
        public string HttpClientName { get; set; }
        public string InputPath { get; }
    }
}
