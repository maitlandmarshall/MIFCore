using Humanizer;
using MIFCore.Hangfire.APIETL.Extract;
using MIFCore.Hangfire.APIETL.Load;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MIFCore.Hangfire.APIETL.Transform
{
    internal class ApiEndpointTransformJob : IApiEndpointTransformJob
    {
        private readonly IApiEndpointTransformPipeline transformPipeline;
        private readonly IEnumerable<ApiEndpointModel> apiEndpointModels;
        private readonly IGetDestinationType getDestinationType;

        public ApiEndpointTransformJob(IApiEndpointTransformPipeline transformPipeline, IEnumerable<ApiEndpointModel> apiEndpointModels, IGetDestinationType getDestinationType)
        {
            this.transformPipeline = transformPipeline;
            this.apiEndpointModels = apiEndpointModels;
            this.getDestinationType = getDestinationType;
        }

        public async Task Transform(ApiEndpoint endpoint, ApiData apiData)
        {
            await this.transformPipeline.OnHandleResponse(new HandleResponseArgs(endpoint, apiData));
            var data = await this.transformPipeline.OnParse(new ParseResponseArgs(endpoint, apiData));

            if (data != null)
            {
                // Get all the different object sets and group them by the ParentKey
                var graphObjectSets = data.ExtractDistinctGraphObjectSets(
                    new ExtractDistinctGraphObjectSetsExtensions.ExtractDistinctGraphObjectSetsArgs
                    {
                        Transform = async (args) =>
                        {
                            await this.transformPipeline.OnTransformModel(new TransformModelArgs(endpoint, apiData, args));
                        }
                    })
                    .GroupBy(y => y.ParentKey)
                    .ToList();

                foreach (var set in graphObjectSets)
                {
                    var model = this.apiEndpointModels.
                        FirstOrDefault(y => y.EndpointName == endpoint.Name && y.InputPath == set.Key);

                    if (model is null)
                        continue;

                    await this.AutoMapModelPropertiesFromApi(set, model);
                }
            }
        }

        private async Task AutoMapModelPropertiesFromApi(IGrouping<string, GraphObjectSet> set, ApiEndpointModel apiEndpointModel)
        {
            var allKeyTypes = set.GetKeyTypes();

            // Get the validKeyTypes and types from all objects with the same ParentKey
            var validKeyTypes = allKeyTypes

                // Filter out the validKeyTypes which have collections
                .Where(y => y.Value.Any(y => typeof(IEnumerable<object>).IsAssignableFrom(y)) == false)

                // Filter out the validKeyTypes which only have null values
                .Where(y => y.Value.All(y => y is null) == false)
                .ToList();

            // Now we have validKeyTypes which represent the destination table schema
            foreach (var (key, types) in validKeyTypes)
            {
                if (apiEndpointModel.MappedProperties.TryGetValue(key, out var apiEndpointModelProperty) == false)
                {
                    apiEndpointModelProperty = new ApiEndpointModelProperty
                    {
                        SourceName = key,
                        DestinationType = await this.getDestinationType.GetDestinationType(apiEndpointModel, key, types),
                        SourceType = types,
                        IsKey = false
                    };

                    apiEndpointModel.MappedProperties.Add(key, apiEndpointModelProperty);
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(apiEndpointModelProperty.DestinationType))
                    {
                        apiEndpointModelProperty.DestinationType = await this.getDestinationType.GetDestinationType(apiEndpointModel, key, types);
                    }
                }

                if (string.IsNullOrWhiteSpace(apiEndpointModelProperty.DestinationName))
                {
                    var underscoreReplacement = "****";

                    apiEndpointModelProperty.DestinationName = apiEndpointModelProperty
                        .SourceName

                        // Pascalize removes underscores, and spaces, so replace them with a placeholder for now
                        // Add spaces so pascalize can capitalize words separated by underscores properly
                        .Replace("_", $" {underscoreReplacement} ")
                        .Pascalize()

                        // Since spaces are removed, only the actual replacement characters should be left.
                        // Conver them back to underscores
                        .Replace(underscoreReplacement, "_");
                }
            }
        }
    }
}
