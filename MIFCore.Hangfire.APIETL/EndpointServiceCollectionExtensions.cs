using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MIFCore.Hangfire.APIETL
{
    public static class EndpointServiceCollectionExtensions
    {
        public static IServiceCollection AddEndpoints(this IServiceCollection serviceDescriptors, Assembly assembly = null)
        {
            assembly ??= Assembly.GetCallingAssembly();

            // Find all endpoint related types in the assembl
            var endpoints = assembly
                .GetTypes()
                .Where(y =>
                    y.GetCustomAttribute<ApiEndpointNameAttribute>() != null
                    || y.GetCustomAttribute<ApiEndpointSelectorAttribute>() != null);

            return serviceDescriptors.AddEndpoints(endpoints);
        }

        public static IServiceCollection AddEndpoints(this IServiceCollection serviceDescriptors, IEnumerable<Type> endpoints)
        {
            // Register the services used to register jobs and create ApiEndpoint definitions
            serviceDescriptors.TryAddSingleton<ApiEndpointRegister>();
            serviceDescriptors.TryAddTransient<IApiEndpointAttributeFactory, ApiEndpointAttributeFactory>();

            foreach (var t in endpoints)
            {
                var endpointNameAttribute = t.GetCustomAttribute<ApiEndpointNameAttribute>();
                var endpointSelectorAttribute = t.GetCustomAttribute<ApiEndpointSelectorAttribute>();

                if (endpointNameAttribute == null
                    && endpointSelectorAttribute == null)
                    throw new ArgumentException($"The type {t.FullName} does not have an {nameof(ApiEndpointNameAttribute)} or {nameof(ApiEndpointSelectorAttribute)} attribute.");

                if (typeof(IDefineEndpoints).IsAssignableFrom(t))
                    serviceDescriptors.AddScoped(typeof(IDefineEndpoints), t);

                if (typeof(IPrepareRequest).IsAssignableFrom(t))
                    serviceDescriptors.AddScoped(typeof(IPrepareRequest), t);

                if (typeof(IPrepareNextRequest).IsAssignableFrom(t))
                    serviceDescriptors.AddScoped(typeof(IPrepareNextRequest), t);

                // Register the endpoint name attribute, so an ApiEndpoint is created from it
                if (endpointNameAttribute != null)
                    serviceDescriptors.AddSingleton(endpointNameAttribute);
            }

            return serviceDescriptors;
        }
    }
}
