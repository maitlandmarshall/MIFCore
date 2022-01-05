using Hangfire;
using MAD.Integration.Common.Analytics;
using MAD.Integration.Common.Http;
using MAD.Integration.Common.Jobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Text;

namespace MAD.Integration.Common
{
    public interface IIntegrationHostBuilder : IHostBuilder
    {
        IIntegrationHostBuilder UseStartup<TStartup>() where TStartup : class, new();
        IIntegrationHostBuilder ConfigureServices(Action<IServiceCollection> configureDelegate);

        IIntegrationHostBuilder UseHangfire();
        IIntegrationHostBuilder UseHangfire(Action<IGlobalConfiguration> configureDelegate);
        IIntegrationHostBuilder UseHangfire(Action<IGlobalConfiguration, HangfireConfig> configureDelegate);

        IIntegrationHostBuilder UseAspNetCore();
        IIntegrationHostBuilder UseAspNetCore(Action<AspNetCoreConfig> configureDelegate);

        IIntegrationHostBuilder UseAppInsights();
        IIntegrationHostBuilder UseAppInsights(Action<AppInsightsConfig> configureDelegate);
    }
}
