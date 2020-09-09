using Hangfire;
using MAD.Integration.Common.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Text;

namespace MAD.Integration.Common
{
    public interface IIntegrationHostBuilder
    {
        IIntegrationHostBuilder UseStartup<TStartup>() where TStartup : class, new();
        IIntegrationHostBuilder ConfigureServices(Action<IServiceCollection> configureDelegate);

        IIntegrationHostBuilder UseHangfire();
        IIntegrationHostBuilder UseHangfire(Action<IGlobalConfiguration> configureDelegate);

        IIntegrationHostBuilder UseAspNetCore();
        IIntegrationHostBuilder UseAspNetCore(Action<AspNetCoreConfig> configureDelegate);

        IIntegrationHostBuilder UseAppInsights();

        IHost Build();
    }
}
