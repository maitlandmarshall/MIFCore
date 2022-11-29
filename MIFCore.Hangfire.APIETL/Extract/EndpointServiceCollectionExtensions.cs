using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MIFCore.Hangfire.APIETL.Extract
{
    public static class EndpointServiceCollectionExtensions
    {
        public static IServiceCollection AddApiEndpointsToExtract(this IServiceCollection serviceDescriptors, Assembly assembly = null)
        {
            assembly ??= Assembly.GetCallingAssembly();

            // Find all endpoint related types in the assembl
            var endpoints = assembly
                .GetTypes()
                .Where(y =>
                    y.GetCustomAttributes<ApiEndpointAttribute>().Any()
                    || y.GetCustomAttributes<ApiEndpointSelectorAttribute>().Any());

            return serviceDescriptors.AddApiEndpointsToExtract(endpoints);
        }

        public static IServiceCollection AddApiEndpointsToExtract(this IServiceCollection serviceDescriptors, IEnumerable<Type> endpoints)
        {
            // Register the services used to register jobs and create ApiEndpoint definitions
            serviceDescriptors.TryAddSingleton<IApiEndpointRegister, ApiEndpointRegister>();
            serviceDescriptors.TryAddTransient<IApiEndpointFactory, ApiEndpointFactory>();
            serviceDescriptors.TryAddTransient<IEndpointExtractPipeline, EndpointExtractPipeline>();
            serviceDescriptors.TryAddScoped<EndpointExtractJob>();

            foreach (var t in endpoints)
            {
                var endpointNameAttributes = t.GetCustomAttributes<ApiEndpointAttribute>();
                var endpointSelectorAttribute = t.GetCustomAttributes<ApiEndpointSelectorAttribute>();

                if (endpointNameAttributes.Any() == false
                    && endpointSelectorAttribute.Any() == false)
                    throw new ArgumentException($"The type {t.FullName} does not have an {nameof(ApiEndpointAttribute)} or {nameof(ApiEndpointSelectorAttribute)} attribute.");

                if (typeof(IDefineEndpoints).IsAssignableFrom(t))
                    serviceDescriptors.AddScoped(typeof(IDefineEndpoints), t);

                if (typeof(IPrepareRequest).IsAssignableFrom(t))
                    serviceDescriptors.AddScoped(typeof(IPrepareRequest), t);

                if (typeof(IPrepareNextRequest).IsAssignableFrom(t))
                    serviceDescriptors.AddScoped(typeof(IPrepareNextRequest), t);

                if (typeof(IHandleResponse).IsAssignableFrom(t))
                    serviceDescriptors.AddScoped(typeof(IHandleResponse), t);

                // Register the endpoint name attribute, so an ApiEndpoint is created from it
                if (endpointNameAttributes.Any())
                {
                    foreach (var en in endpointNameAttributes)
                    {
                        serviceDescriptors.AddSingleton(en);
                    }
                }
            }

            return serviceDescriptors;
        }
    }
}
