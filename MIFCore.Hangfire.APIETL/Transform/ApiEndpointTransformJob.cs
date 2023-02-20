using Humanizer;
using MIFCore.Hangfire.APIETL.Extract;
using MIFCore.Hangfire.APIETL.Load;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace MIFCore.Hangfire.APIETL.Transform
{
    internal class ApiEndpointTransformJob : IApiEndpointTransformJob
    {
        private readonly IApiEndpointTransformPipeline transformPipeline;
        private readonly IEnumerable<ApiEndpointModel> apiEndpointModels;
        private readonly IGetDestinationType getDestinationType;
        private readonly IApiEndpointLoadJob apiEndpointLoadJob;

        public ApiEndpointTransformJob(
            IApiEndpointTransformPipeline transformPipeline,
            IEnumerable<ApiEndpointModel> apiEndpointModels,
            IGetDestinationType getDestinationType,
            IApiEndpointLoadJob apiEndpointLoadJob)
        {
            this.transformPipeline = transformPipeline;
            this.apiEndpointModels = apiEndpointModels;
            this.getDestinationType = getDestinationType;
            this.apiEndpointLoadJob = apiEndpointLoadJob;
        }

        public async Task Transform(ApiEndpoint endpoint, ApiData apiData, ExtractArgs extractArgs)
        {
            await this.transformPipeline.OnHandleResponse(new HandleResponseArgs(endpoint, apiData, extractArgs));
            var parsedData = await this.transformPipeline.OnParse(new ParseResponseArgs(endpoint, apiData, extractArgs));

            if (parsedData != null)
            {
                // Get all the different object sets and group them by the ParentKey
                var graphObjectSets = parsedData.ExtractDistinctGraphObjectSets(
                    new ExtractDistinctGraphObjectSetsExtensions.ExtractDistinctGraphObjectSetsArgs
                    {
                        Transform = async (args) =>
                        {
                            await this.transformPipeline.OnTransformModel(new TransformModelArgs(endpoint, apiData, extractArgs, args));
                        }
                    })
                    .GroupBy(y => y.ParentKey)
                    .ToList();

                foreach (var set in graphObjectSets)
                {
                    // Has the user of the library defined any endpoint models for this object set?
                    var model = this.apiEndpointModels.
                        FirstOrDefault(y => y.EndpointName == endpoint.Name && y.InputPath == set.Key);

                    // If they haven't, do nothing
                    if (model is null)
                        continue;

                    // There may be properties in the API response that we haven't mapped.
                    // Automatically generate properties from the response so they can load into the destination
                    await this.AutoMapModelPropertiesGraphObjectSet(set, model);

                    // Generate a flat list of items to load
                    var dataToLoad = set.SelectMany(y => y.Objects).ToList();

                    // The parsedData from the API may need to be parsed into .net CLR types as defined by the ApiEndpointModelProperty SourceTypes
                    var propertiesWithClrType = model.MappedProperties
                        .Values
                        .Where(y => y.SourceType.Where(y => y != null).Any())
                        .ToList();

                    foreach (var prop in propertiesWithClrType)
                    {
                        // Get the clr type the property should have
                        var clrType = prop.SourceType.First(y => y != null);
                        var typeConverter = TypeDescriptor.GetConverter(clrType);

                        foreach (var data in dataToLoad)
                        {
                            // Try to get the property from the data object
                            if (data.TryGetValue(prop.SourceName, out var propValue))
                            {
                                // Do nothing if its null
                                if (propValue is null)
                                    continue;

                                var propValueType = propValue.GetType();

                                // Make sure the propValueType is different from the clrType, otherwise a conversion is not necessary
                                if (propValueType != clrType
                                    && typeConverter.CanConvertFrom(propValueType))
                                {
                                    data[prop.SourceName] = typeConverter.ConvertFrom(propValue);
                                }
                            }
                            else
                            {
                                // Ensure all the dataToLoad items have all the properties defined in the model, even if just null.
                                data[prop.SourceName] = null;
                            }
                        }
                    }

                    await this.apiEndpointLoadJob.Load(endpoint, model, dataToLoad);
                }
            }
        }

        private async Task AutoMapModelPropertiesGraphObjectSet(IGrouping<string, GraphObjectSet> set, ApiEndpointModel apiEndpointModel)
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
                // The key could have already been added to the model via ApiEndpointModel API
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

                // If it has already been added, the destination type may be unknown. If so we can try to determine it from the API response types.
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
