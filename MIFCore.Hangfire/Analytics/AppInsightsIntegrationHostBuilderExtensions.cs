using MIFCore.Common;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.DependencyInjection;
using System;
using Hangfire;

namespace MIFCore.Hangfire.Analytics
{
    public static class AppInsightsIntegrationHostBuilderExtensions
    {
        public static IIntegrationHostBuilder UseAppInsights(this IIntegrationHostBuilder integrationHostBuilder)
        {
            return integrationHostBuilder.UseAppInsights(null);
        }

        public static IIntegrationHostBuilder UseAppInsights(this IIntegrationHostBuilder integrationHostBuilder, Action<AppInsightsConfig> configureDelegate)
        {
            integrationHostBuilder.ConfigureServices(y => y.AddAppInsights(configureDelegate));
            integrationHostBuilder.StartupHandler.PostConfigureActions += StartupHandler_PostConfigureActions;

            return integrationHostBuilder;
        }

        private static void StartupHandler_PostConfigureActions(IServiceProvider serviceProvider)
        {
            var appInsights = serviceProvider.GetService<AppInsightsConfig>();

            if (!string.IsNullOrEmpty(appInsights?.InstrumentationKey))
            {
                var globalConfig = serviceProvider.GetRequiredService<IGlobalConfiguration>();
                var telemetryClient = new TelemetryClient(TelemetryConfigurationFactory.Create(appInsights));

                globalConfig.UseFilter(new AppInsightsEventsFilter(telemetryClient));
            }
        }
    }
}
