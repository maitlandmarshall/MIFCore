using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

namespace MIFCore.Common
{
    public interface IIntegrationHostBuilder : IHostBuilder
    {
        StartupHandler StartupHandler { get; }

        IIntegrationHostBuilder UseStartup<TStartup>() where TStartup : class, new();
        IIntegrationHostBuilder ConfigureServices(Action<IServiceCollection> configureDelegate);
    }
}
