using Hangfire;
using MIFCore.Common;
using MIFCore.Hangfire;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace MIFCore.Hangfire
{
    internal static class HangfireServiceCollectionExtensions
    {
        public static IServiceCollection AddHangfire(this IServiceCollection serviceDescriptors, Action<IGlobalConfiguration, HangfireConfig> configureDelegate = null)
        {
            serviceDescriptors.AddHostedService<HangfireBackgroundService>();
            serviceDescriptors.AddTransient<IRecurringJobFactory, RecurringJobFactory>();

            var hangfireConfig = new HangfireConfig();
            Globals.DefaultConfiguration.Bind(hangfireConfig);
            serviceDescriptors.AddSingleton(hangfireConfig);

            serviceDescriptors.AddHangfire(configuration: cfg => configureDelegate?.Invoke(cfg, hangfireConfig));

            return serviceDescriptors;
        }
    }
}
