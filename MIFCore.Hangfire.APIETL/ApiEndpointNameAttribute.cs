using System;

namespace MIFCore.Hangfire.APIETL
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class ApiEndpointNameAttribute : Attribute
    {
        public ApiEndpointNameAttribute(string endpointName)
        {
            this.EndpointName = endpointName;
        }

        public string EndpointName { get; }
    }
}
