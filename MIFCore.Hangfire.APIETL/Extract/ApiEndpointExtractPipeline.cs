﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MIFCore.Hangfire.APIETL.Extract
{
    internal class ApiEndpointExtractPipeline : IApiEndpointExtractPipeline
    {
        private readonly IEnumerable<IPrepareRequest> prepareRequests;
        private readonly IEnumerable<IPrepareNextRequest> prepareNextRequests;

        public ApiEndpointExtractPipeline(
            IEnumerable<IPrepareRequest> prepareRequests,
            IEnumerable<IPrepareNextRequest> prepareNextRequests)
        {
            this.prepareRequests = prepareRequests;
            this.prepareNextRequests = prepareNextRequests;
        }

        public async Task OnPrepareRequest(PrepareRequestArgs args)
        {
            var relatedPrepareRequests = this.prepareRequests
                .Where(y => y.RespondsToEndpointName(args.Endpoint.Name));

            foreach (var prepareRequest in relatedPrepareRequests)
            {
                await prepareRequest.OnPrepareRequest(args);
            }
        }

        public async Task<IDictionary<string, object>> OnPrepareNextRequest(PrepareNextRequestArgs args)
        {
            var relatedPreparedNextRequests = this.prepareNextRequests
                .Where(y => y.RespondsToEndpointName(args.Endpoint.Name));

            var result = new Dictionary<string, object>();

            foreach (var prepareNextRequest in relatedPreparedNextRequests)
            {
                // Get the data from the prepareNextRequest
                var data = await prepareNextRequest.OnPrepareNextRequest(args);

                if (data == default(IDictionary<string, object>))
                    continue;

                // Merge it with the resulting dictionary
                result = result.Union(data).ToDictionary(x => x.Key, x => x.Value);
            }

            return result;
        }
    }
}
