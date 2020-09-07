using Hangfire;
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
        IIntegrationHostBuilder UseAspNetCore();
        IIntegrationHostBuilder UseAppInsights();

        IHost Build();
    }
}
