using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace MIFCore.Hangfire.APIETL.Load
{
    public static class TypeExtensions
    {
        public static ApiEndpointModel GetApiEndpointModel(this Type type)
        {
            var attribute = type.GetCustomAttribute<ApiEndpointModelAttribute>();

            if (attribute is null)
                return null;

            var model = new ApiEndpointModel
            {
                DestinationName = attribute.DestinationName,
                EndpointName = attribute.EndpointName,
                InputPath = attribute.InputPath
            };

            if (string.IsNullOrWhiteSpace(model.DestinationName))
            {
                model.DestinationName = type.Name;
            }

            var modelProperties = type.GetProperties()
                .Select(y => new KeyValuePair<PropertyInfo, ApiEndpointModelPropertyAttribute>(y, y.GetCustomAttribute<ApiEndpointModelPropertyAttribute>()))
                .Where(y => y.Value != null)
                .ToDictionary(
                    keySelector: y => y.Key,
                    elementSelector: y => y.Value);

            foreach (var (propertyInfo, propertyAttribute) in modelProperties)
            {
                var sourceName = propertyAttribute.SourceName;
                var destinationName = propertyAttribute.DestinationName;

                if (string.IsNullOrWhiteSpace(sourceName))
                {
                    sourceName = propertyInfo.Name;
                    destinationName = sourceName;
                }

                var property = new ApiEndpointModelProperty
                {
                    SourceName = sourceName,
                    DestinationName = destinationName,
                    IsKey = propertyInfo.GetCustomAttribute<KeyAttribute>() != null,
                    SourceType = new HashSet<Type> { propertyInfo.PropertyType },
                    DestinationType = propertyAttribute.DestinationType
                };

                model.MappedProperties.Add(sourceName, property);
            }

            return model;
        }
    }
}
