using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MIFCore.Hangfire.APIETL.Extract;
using MIFCore.Hangfire.APIETL.Load;
using MIFCore.Hangfire.APIETL.Transform;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MIFCore.Hangfire.APIETL
{
    public static class APIETLServiceCollectionExtensions
    {
        public static IServiceCollection AddApiEndpointsToExtract(this IServiceCollection serviceDescriptors, Assembly assembly = null)
        {
            assembly ??= Assembly.GetCallingAssembly();

            // Find all endpoint related types in the assembly
            var endpoints = assembly
                .GetTypes()
                .Where(y =>
                    y.GetCustomAttributes<ApiEndpointAttribute>().Any()
                    || y.GetCustomAttributes<ApiEndpointSelectorAttribute>().Any()
                    || y.GetCustomAttributes<ApiEndpointModelAttribute>().Any());

            return serviceDescriptors.AddApiEndpointsToExtract(endpoints);
        }

        public static IServiceCollection AddApiEndpointsToExtract(this IServiceCollection serviceDescriptors, IEnumerable<Type> types)
        {
            // Register the services used to register jobs and create ApiEndpoint definitions
            serviceDescriptors.TryAddSingleton<IApiEndpointRegister, ApiEndpointRegister>();
            serviceDescriptors.TryAddTransient<IApiEndpointFactory, ApiEndpointFactory>();

            // Register the extract jobs & pipelines
            serviceDescriptors.TryAddTransient<IApiEndpointExtractPipeline, ApiEndpointExtractPipeline>();
            serviceDescriptors.TryAddScoped<IApiEndpointExtractJob, ApiEndpointExtractJob>();

            // Register the transform jobs & pipelines
            serviceDescriptors.TryAddTransient<IApiEndpointTransformPipeline, ApiEndpointTransformPipeline>();
            serviceDescriptors.TryAddScoped<IApiEndpointTransformJob, ApiEndpointTransformJob>();

            // Register the load jobs & pipelines
            serviceDescriptors.TryAddTransient<IApiEndpointLoadPipeline, ApiEndpointLoadPipeline>();
            serviceDescriptors.TryAddScoped<IApiEndpointLoadJob, ApiEndpointLoadJob>();


            foreach (var t in types)
            {
                var endpointNameAttributes = t.GetCustomAttributes<ApiEndpointAttribute>();
                var endpointSelectorAttribute = t.GetCustomAttributes<ApiEndpointSelectorAttribute>();
                var endpointModelAttributes = t.GetCustomAttributes<ApiEndpointModelAttribute>();

                if (endpointNameAttributes.Any() == false
                    && endpointSelectorAttribute.Any() == false
                    && endpointModelAttributes.Any() == false)
                    throw new ArgumentException($"The type {t.FullName} does not have an {nameof(ApiEndpointAttribute)}, {nameof(ApiEndpointSelectorAttribute)} or {nameof(ApiEndpointModel)} attributes.");

                // Register the extract services
                if (typeof(IDefineEndpoints).IsAssignableFrom(t))
                    serviceDescriptors.AddScoped(typeof(IDefineEndpoints), t);

                if (typeof(IPrepareRequest).IsAssignableFrom(t))
                    serviceDescriptors.AddScoped(typeof(IPrepareRequest), t);

                if (typeof(IPrepareNextRequest).IsAssignableFrom(t))
                    serviceDescriptors.AddScoped(typeof(IPrepareNextRequest), t);

                // Register the transform services
                if (typeof(IHandleResponse).IsAssignableFrom(t))
                    serviceDescriptors.AddScoped(typeof(IHandleResponse), t);

                if (typeof(IParseResponse).IsAssignableFrom(t))
                    serviceDescriptors.AddScoped(typeof(IParseResponse), t);

                if (typeof(ITransformModel).IsAssignableFrom(t))
                    serviceDescriptors.AddScoped(typeof(ITransformModel), t);

                // Register the load services
                if (typeof(ILoadData).IsAssignableFrom(t))
                    serviceDescriptors.AddScoped(typeof(ILoadData), t);

                if (endpointModelAttributes.Any())
                {
                    var endpointModel = t.GetApiEndpointModel();
                    serviceDescriptors.AddSingleton<ApiEndpointModel>(endpointModel);
                }

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
