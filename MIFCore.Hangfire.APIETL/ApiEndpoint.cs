using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

[assembly: InternalsVisibleTo("MIFCore.Hangfire.APIETL.Tests")]
namespace MIFCore.Hangfire.APIETL
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
