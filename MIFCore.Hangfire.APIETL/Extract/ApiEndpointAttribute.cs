using System;

namespace MIFCore.Hangfire.APIETL.Extract
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class ApiEndpointAttribute : Attribute
    {
        public ApiEndpointAttribute(string endpointName, string httpClientName)
        {
            this.EndpointName = endpointName;
            this.HttpClientName = httpClientName;
        }

        public string EndpointName { get; }
        public string HttpClientName { get; set; }
    }
}
