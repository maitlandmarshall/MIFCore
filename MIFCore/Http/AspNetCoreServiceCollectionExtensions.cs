using MIFCore.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace MIFCore.Http
{
    internal static class AspNetCoreServiceCollectionExtensions
    {
        public static IServiceCollection AddAspNetCore(this IServiceCollection serviceCollection, Action<AspNetCoreConfig> configureDelegate = null)
        {
            var aspNetCoreConfig = new AspNetCoreConfig();

            Globals.DefaultConfiguration.Bind(aspNetCoreConfig);
            configureDelegate?.Invoke(aspNetCoreConfig);

            serviceCollection.AddSingleton(aspNetCoreConfig);
            serviceCollection.AddHostedService<AspNetCoreBackgroundService>();

            return serviceCollection;
        }
    }
}
