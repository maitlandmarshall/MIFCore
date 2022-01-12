using Hangfire;
using MIFCore.Common;
using System;

namespace MIFCore.Hangfire
{
    public static class HangfireIntegrationHostBuilderExtensions
    {
        public static IIntegrationHostBuilder UseHangfire(this IIntegrationHostBuilder integrationHostBuilder)
        {
            integrationHostBuilder.ConfigureServices(y => y.AddHangfire());

            return integrationHostBuilder;
        }

        public static IIntegrationHostBuilder UseHangfire(this IIntegrationHostBuilder integrationHostBuilder, Action<IGlobalConfiguration> configureDelegate)
        {
            integrationHostBuilder.ConfigureServices(y => y.AddHangfire((cfg, svg) => configureDelegate(cfg)));

            return integrationHostBuilder;
        }
        public static IIntegrationHostBuilder UseHangfire(this IIntegrationHostBuilder integrationHostBuilder, Action<IGlobalConfiguration, HangfireConfig> configureDelegate)
        {
            integrationHostBuilder.ConfigureServices(y => y.AddHangfire(configureDelegate));

            return integrationHostBuilder;
        }
    }
}
