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

        public EndpointExtractJob(
            IHttpClientFactory httpClientFactory,
            ApiEndpointRegister apiEndpointRegister,
            IBackgroundJobClient backgroundJobClient)
        {
            this.httpClientFactory = httpClientFactory;
            this.apiEndpointRegister = apiEndpointRegister;
            this.backgroundJobClient = backgroundJobClient;
        }

        [DisableIdenticalQueuedItems]
        public async Task Extract(string endpointName, ExtractArgs extractArgs = null)
        {
            // If there aren't any endpoints registered, then we can't do anything. So let's just reschedule this job for later.
            if (this.apiEndpointRegister.Endpoints.Any() == false)
            {
                throw new RescheduleJobException(DateTime.Now.AddMinutes(1));
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

            var httpClient = this.httpClientFactory.CreateClient(endpoint.HttpClientName);
            var request = await CreateRequest(endpoint, extractArgs);
            var apiData = await this.ExecuteRequest(endpoint, httpClient, request, extractArgs);

            if (endpoint is IPrepareNextRequest prepareNextRequest)
            {
                // If OnPrepareNextRequest returns null or an empty dict, then we're done with this endpoint.
                var nextRequestData = await prepareNextRequest.OnPrepareNextRequest(new PrepareNextRequestArgs(apiData: apiData, data: extractArgs?.RequestData));

                if (nextRequestData == default(IDictionary<string, object>)
                    || nextRequestData.Keys.Any() == false)
                    return;

                // Otherwise we need to schedule another job to continue extracting this endpoint.
                this.backgroundJobClient.Enqueue<EndpointExtractJob>(y => y.Extract(endpoint.Name, new ExtractArgs(nextRequestData, apiData.ParentId)));
            }
        }

        private async Task<ApiData> ExecuteRequest(ApiEndpoint endpoint, HttpClient httpClient, HttpRequestMessage request, ExtractArgs extractArgs = null)
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
                ParentId = extractArgs?.ParentApiDataId
            };

            // TODO: Push apiData to a another service for any additional handling

            return apiData;
        }

        private static async Task<HttpRequestMessage> CreateRequest(ApiEndpoint endpoint, ExtractArgs extractArgs = null)
        {
            // Create a new request, using endpoint.Name as the relative uri
            // i.e endpoint.Name = "getStuff" and httpClient.BaseAddress = "https://someapi/api/"
            var request = new HttpRequestMessage
            {
                RequestUri = new Uri(endpoint.Name, UriKind.Relative)
            };

            // Stitch on any additional headers
            foreach (var h in endpoint.AdditionalHeaders)
            {
                request.Headers.Add(h.Key, h.Value);
            }

            if (endpoint is IPrepareRequest prepareRequest)
            {
                // OnPrepareRequest after this system has finished up with the request
                await prepareRequest.OnPrepareRequest(new PrepareRequestArgs(request, extractArgs?.RequestData));
            }

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
