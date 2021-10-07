using Hangfire;
using MAD.Integration.Common.Jobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace MAD.Integration.Common.Hangfire
{
    internal static class HangfireServiceCollectionExtensions
    {
        public static IServiceCollection AddHangfire(this IServiceCollection serviceDescriptors, Action<IGlobalConfiguration, HangfireConfig> configureDelegate = null)
        {
            serviceDescriptors.AddHostedService<HangfireBackgroundService>();
            serviceDescriptors.AddTransient<IRecurringJobFactory, RecurringJobFactory>();

            var hangfireConfig = new HangfireConfig();
            IntegrationHost.DefaultConfiguration.Bind(hangfireConfig);
            serviceDescriptors.AddSingleton(hangfireConfig);

            serviceDescriptors.AddHangfire(configuration: cfg => configureDelegate?.Invoke(cfg, hangfireConfig));

            return serviceDescriptors;
        }
    }
}
