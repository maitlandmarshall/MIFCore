using Hangfire;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace MIFCore.Hangfire.APIETL
{
    internal class EndpointExtractJob
    {
        private readonly IHttpClientFactory httpClientFactory;
        private readonly ApiEndpointRegister apiEndpointRegister;
        private readonly IBackgroundJobClient backgroundJobClient;
        private readonly IEndpointExtractPipeline endpointExtractPipeline;

        public EndpointExtractJob(
            IHttpClientFactory httpClientFactory,
            ApiEndpointRegister apiEndpointRegister,
            IBackgroundJobClient backgroundJobClient,
            IEndpointExtractPipeline endpointExtractPipeline)
        {
            this.httpClientFactory = httpClientFactory;
            this.apiEndpointRegister = apiEndpointRegister;
            this.backgroundJobClient = backgroundJobClient;
            this.endpointExtractPipeline = endpointExtractPipeline;
        }

        [DisableIdenticalQueuedItems]
        public async Task Extract(string endpointName, ExtractArgs extractArgs = null)
        {
            // If there aren't any endpoints registered, then we can't do anything. So let's just reschedule this job for later.
            if (this.apiEndpointRegister.Endpoints.Any() == false)
            {
                throw new RescheduleJobException(DateTime.Now.AddSeconds(5));
            }

            var endpoint = this.apiEndpointRegister.Get(endpointName);
            await this.ExtractEndpoint(endpoint, extractArgs);
        }

        private async Task ExtractEndpoint(ApiEndpoint endpoint, ExtractArgs extractArgs = null)
        {
            if (endpoint is null)
            {
                throw new ArgumentNullException(nameof(endpoint));
            }

            extractArgs ??= new ExtractArgs(new Dictionary<string, object>(), null);

            var httpClient = this.httpClientFactory.CreateClient(endpoint.HttpClientName);
            var request = await this.CreateRequest(httpClient.BaseAddress, endpoint, extractArgs);
            var apiData = await this.ExecuteRequest(endpoint, httpClient, request, extractArgs);

            var nextRequestData = await this.endpointExtractPipeline.OnPrepareNextRequest(new PrepareNextRequestArgs(endpoint: endpoint, apiData: apiData, data: extractArgs.RequestData));

            try
            {
                // If OnPrepareNextRequest returns an empty dict, then we're done with this endpoint.
                if (nextRequestData.Keys.Any() == false)
                    return;

                // Otherwise we need to schedule another job to continue extracting this endpoint.
                this.backgroundJobClient.Enqueue<EndpointExtractJob>(y => y.Extract(endpoint.Name, new ExtractArgs(nextRequestData, apiData.Id)));
            }
            finally
            {
                await this.endpointExtractPipeline.OnHandleResponse(new HandleResponseArgs(endpoint, apiData));
            }
        }

        private async Task<ApiData> ExecuteRequest(ApiEndpoint endpoint, HttpClient httpClient, HttpRequestMessage request, ExtractArgs extractArgs)
        {
            // Get the response payload as a string
            var response = await httpClient.SendAsync(request);
            var data = await response.Content.ReadAsStringAsync();

            // Turn the payload response into an ApiData instance
            var apiData = new ApiData
            {
                Endpoint = endpoint.Name,
                Uri = response.RequestMessage.RequestUri.ToString(),
                Data = data,
                ParentId = extractArgs.ParentApiDataId
            };

            return apiData;
        }

        private async Task<HttpRequestMessage> CreateRequest(Uri baseAddress, ApiEndpoint endpoint, ExtractArgs extractArgs)
        {
            // Create a new request, using endpoint.Name as the relative uri
            // i.e endpoint.Name = "getStuff" and httpClient.BaseAddress = "https://someapi/api/"
            var request = new HttpRequestMessage
            {
                RequestUri = new Uri(baseAddress, endpoint.Name)
            };

            // Stitch on any additional headers
            foreach (var h in endpoint.AdditionalHeaders)
            {
                request.Headers.Add(h.Key, h.Value);
            }

            // OnPrepareRequest after this system has finished up with the request
            await this.endpointExtractPipeline.OnPrepareRequest(new PrepareRequestArgs(endpoint, request, extractArgs.RequestData));

            return request;
        }

        public class ExtractArgs
        {
            public ExtractArgs(IDictionary<string, object> requestData, Guid? parentApiDataId)
            {
                this.RequestData = requestData;
                this.ParentApiDataId = parentApiDataId;
            }

            public IDictionary<string, object> RequestData { get; set; }
            public Guid? ParentApiDataId { get; set; }
        }
    }
}
