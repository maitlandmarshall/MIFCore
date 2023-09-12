using System;

namespace MIFCore.Hangfire.APIETL.Load
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class ApiEndpointModelPropertyAttribute : Attribute
    {
        public ApiEndpointModelPropertyAttribute(string sourceName, string destinationName)
        {
            this.SourceName = sourceName;
            this.DestinationName = destinationName;
        }

        public ApiEndpointModelPropertyAttribute(string sourceName)
        {
            this.SourceName = sourceName;
        }

        public ApiEndpointModelPropertyAttribute() { }

        public string SourceName { get; }

        public string DestinationName { get; }
        public string DestinationType { get; set; }
    }
}
