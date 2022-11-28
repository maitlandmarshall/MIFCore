﻿using System.Collections.Generic;
using System.Threading.Tasks;

namespace MIFCore.Hangfire.APIETL.Extract
{
    public class ApiEndpoint
    {
        public ApiEndpoint(string jobName, string httpClientName = null)
        {
            this.JobName = jobName;
            this.HttpClientName = httpClientName;
        }

        internal ApiEndpoint(string name)
        {
            this.Name = name;
            this.JobName = name;
        }

        public string Name { get; internal set; }

        public string JobName { get; }
        public string HttpClientName { get; }

        public IDictionary<string, string> AdditionalHeaders { get; } = new Dictionary<string, string>();

        public delegate Task PrepareRequestDelegate(PrepareRequestArgs args);
        public delegate Task<IDictionary<string, object>> PrepareNextRequestDelegate(PrepareNextRequestArgs args);
    }
}