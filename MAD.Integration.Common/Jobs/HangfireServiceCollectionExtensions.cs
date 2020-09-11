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
            serviceDescriptors.AddSingleton<IGlobalConfiguration>(sp => GlobalConfiguration.Configuration);

            var hangfireConfig = new HangfireConfig();
            IntegrationHost.DefaultConfiguration.Bind(hangfireConfig);
            serviceDescriptors.AddSingleton(hangfireConfig);

            configureDelegate?.Invoke(GlobalConfiguration.Configuration, hangfireConfig);

            return serviceDescriptors;
        }
    }
}
