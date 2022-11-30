using System;
using System.Collections.Generic;

namespace MIFCore.Hangfire.APIETL.Load
{
    public class ApiEndpointModelProperty
    {
        public string SourceName { get; set; }
        public string DestinationName { get; set; }

        public bool IsKey { get; set; }

        public HashSet<Type> SourceType { get; set; }
        public string DestinationType { get; set; }
    }
}
