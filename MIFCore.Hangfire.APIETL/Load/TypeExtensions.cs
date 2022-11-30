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
                .Select(y => new KeyValuePair<PropertyInfo, Attribute>(y, y.GetCustomAttribute<ApiEndpointModelPropertyAttribute>() as Attribute ?? y.GetCustomAttribute<KeyAttribute>()))
                .Where(y => y.Value != null)
                .ToDictionary(
                    keySelector: y => y.Key,
                    elementSelector: y => y.Value);

            foreach (var (propertyInfo, propertyAttribute) in modelProperties)
            {
                var property = new ApiEndpointModelProperty
                {
                    IsKey = propertyInfo.GetCustomAttribute<KeyAttribute>() != null,
                    SourceType = new HashSet<Type> { propertyInfo.PropertyType },
                };

                if (propertyAttribute is ApiEndpointModelPropertyAttribute modelPropertyAttribute)
                {
                    property.SourceName = modelPropertyAttribute.SourceName;
                    property.DestinationName = modelPropertyAttribute.DestinationName;
                }

                if (string.IsNullOrWhiteSpace(property.SourceName))
                {
                    property.SourceName = propertyInfo.Name;
                    property.DestinationName = propertyInfo.Name;
                }

                model.MappedProperties.Add(property.SourceName, property);
            }

            return model;
        }
    }
}
