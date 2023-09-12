using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace MIFCore.Hangfire.APIETL
{
    public class ApiEndpoint
    {
        private readonly Lazy<IEnumerable<string>> routeParameters;

        public ApiEndpoint(string jobName, string httpClientName = null) : this()
        {
            this.HttpClientName = httpClientName;
            this.JobName = jobName;
        }

        internal ApiEndpoint(string name) : this()
        {
            this.Name = name;
            this.JobName = name;
        }

        private ApiEndpoint()
        {
            this.routeParameters = new Lazy<IEnumerable<string>>(() =>
            {
                // Match on {something123}
                var parameterRegex = new Regex(@"\{([A-Za-z0-9_]+)\}");
                var matches = parameterRegex.Matches(this.Name);

                // Return the names of the groups, i.e path/to/{id}/{otherId}, extract: id, otherId
                // Select the second item in the group [1] as it is the "id" or "otherId" instead of "{id}", etc.
                return matches.Select(y => y.Groups[1].Value).ToList();
            });
        }

        public string Name { get; internal set; }
        public string JobName { get; internal set; }

        public IEnumerable<string> RouteParameters => this.routeParameters.Value;
        public IDictionary<string, string> AdditionalHeaders { get; } = new Dictionary<string, string>();

        public string HttpClientName { get; }
    }
}
