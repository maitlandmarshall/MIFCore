using Hangfire;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MIFCore.Hangfire.APIETL
{
    public class ApiEndpointRegister : IApiEndpointRegister
    {
        private readonly IDictionary<string, ApiEndpoint> endpoints = new Dictionary<string, ApiEndpoint>();
        private readonly IRecurringJobManager recurringJobManager;
        private readonly IApiEndpointAttributeFactory apiEndpointAttributeFactory;

        public ApiEndpointRegister(IRecurringJobManager recurringJobManager, IApiEndpointAttributeFactory apiEndpointAttributeFactory)
        {
            this.recurringJobManager = recurringJobManager;
            this.apiEndpointAttributeFactory = apiEndpointAttributeFactory;
        }

        public IEnumerable<ApiEndpoint> Endpoints { get => this.endpoints.Values.ToArray(); }

        public ApiEndpointRegister Register(ApiEndpoint endpoint)
        {
            this.endpoints.Add(endpoint.Name, endpoint);

            this.recurringJobManager.AddOrUpdate<EndpointExtractJob>(
                endpoint.JobName,
                job => job.Extract(endpoint.Name, null),
                Cron.Daily()
            );

            return this;
        }

        /// <summary>
        /// Automatically registers all endpoints defined using the Attributes API as recurring jobs.
        /// </summary>
        /// <returns></returns>
        public async Task Register()
        {
            // If there are already endpoints registered, then we don't need to do anything otherwise it will cause double ups.
            if (this.Endpoints.Any())
                return;

            var endpoints = this.apiEndpointAttributeFactory.Create();

            await foreach (var ep in endpoints)
            {
                this.Register(ep);
            }
        }

        public ApiEndpoint Get(string endpointName)
        {
            return this.endpoints[endpointName];
        }
    }
}
