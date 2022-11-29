using System;

namespace MIFCore.Hangfire.APIETL.Extract
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class ApiEndpointAttribute : Attribute
    {
        public ApiEndpointAttribute(string endpointName)
        {
            this.EndpointName = endpointName;
        }

        public string EndpointName { get; }
        public string HttpClientName { get; set; }
    }
}
