using Hangfire;
using MIFCore.Hangfire.APIETL.Transform;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace MIFCore.Hangfire.APIETL.Extract
{
    internal class ApiEndpointExtractJob : IApiEndpointExtractJob
    {
        private readonly IHttpClientFactory httpClientFactory;
        private readonly IApiEndpointRegister apiEndpointRegister;
        private readonly IBackgroundJobClient backgroundJobClient;
        private readonly IApiEndpointExtractPipeline endpointExtractPipeline;
        private readonly IApiEndpointTransformJob endpointTransformJob;

        public ApiEndpointExtractJob(
            IHttpClientFactory httpClientFactory,
            IApiEndpointRegister apiEndpointRegister,
            IBackgroundJobClient backgroundJobClient,
            IApiEndpointExtractPipeline endpointExtractPipeline,
            IApiEndpointTransformJob endpointTransformJob)
        {
            this.httpClientFactory = httpClientFactory;
            this.apiEndpointRegister = apiEndpointRegister;
            this.backgroundJobClient = backgroundJobClient;
            this.endpointExtractPipeline = endpointExtractPipeline;
            this.endpointTransformJob = endpointTransformJob;
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

            // Default to string.Empty to avoid an error if the endpoint.HttpClientName is null.
            var httpClient = this.httpClientFactory.CreateClient(endpoint.HttpClientName ?? string.Empty);

            var request = await this.CreateRequest(httpClient.BaseAddress, endpoint, extractArgs);
            var apiData = await this.ExecuteRequest(endpoint, httpClient, request, extractArgs);

            var nextRequestData = await this.endpointExtractPipeline.OnPrepareNextRequest(new PrepareNextRequestArgs(endpoint: endpoint, apiData: apiData, data: extractArgs.RequestData));
            var isLastRequest = nextRequestData.Keys.Any() == false;

            try
            {
                // If OnPrepareNextRequest returns an empty dict, then we're done with this endpoint.
                if (isLastRequest)
                    return;

                // Otherwise we need to schedule another job to continue extracting this endpoint.
                this.backgroundJobClient.Enqueue<IApiEndpointExtractJob>(y => y.Extract(endpoint.Name, new ExtractArgs(nextRequestData, apiData.Id)));
            }
            finally
            {
                await this.endpointTransformJob.Transform(endpoint, apiData);
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
            // Create a new request, using endpoint.SourceName as the relative uri
            // i.e endpoint.SourceName = "getStuff" and httpClient.BaseAddress = "https://someapi/api/"
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
    }
}
