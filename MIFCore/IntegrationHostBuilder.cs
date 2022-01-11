using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MIFCore.Common;
using System;
using System.Collections.Generic;

namespace MIFCore
{
    public partial class IntegrationHostBuilder : IIntegrationHostBuilder
    {
        private readonly IServiceCollection serviceDescriptors = new ServiceCollection();
        private readonly List<Action<IServiceCollection>> configureServiceActions = new List<Action<IServiceCollection>>();
        private readonly IHostBuilder hostBuilder;

        public StartupHandler StartupHandler { get; } = new StartupHandler();

        public IntegrationHostBuilder(string[] args)
        {
            if (args != null)
            {
                this.hostBuilder = Host.CreateDefaultBuilder(args);
            }
            else
            {
                this.hostBuilder = Host.CreateDefaultBuilder();
            }
        }

        public IntegrationHostBuilder() : this(null)
        {

        }

        public IDictionary<object, object> Properties { get; }

        public IHost Build()
        {
            this.ConfigureServices(y => y.AddSingleton(this.StartupHandler));
            this.StartupHandler.ConfigureServices(this.serviceDescriptors);

            this.InvokeConfigureServiceActions();

            var serviceProviderFactory = new AutofacServiceProviderFactory(builder => builder.Populate(this.serviceDescriptors));
            this.hostBuilder
                .UseWindowsService()
                .ConfigureLogging(cfg =>
                {
                    cfg.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);
                    cfg.AddFilter("Hangfire", LogLevel.Error);
                })
                .UseServiceProviderFactory(serviceProviderFactory)
                .ConfigureAppConfiguration(cfg =>
                {
                    cfg.Sources.Clear();
                    cfg.AddConfiguration(Globals.DefaultConfiguration);
                });

            var host = this.hostBuilder.Build();

            this.StartupHandler.Configure(host.Services.GetService<IServiceProvider>());

            return host;
        }

        public IIntegrationHostBuilder ConfigureServices(Action<IServiceCollection> configureDelegate)
        {
            this.configureServiceActions.Add(configureDelegate);
            return this;
        }

        public IIntegrationHostBuilder UseStartup<TStartup>() where TStartup : class, new()
        {
            this.StartupHandler.SetStartup<TStartup>();
            return this;
        }

        public IHostBuilder ConfigureHostConfiguration(Action<IConfigurationBuilder> configureDelegate)
        {
            this.hostBuilder.ConfigureHostConfiguration(configureDelegate);
            return this;
        }

        public IHostBuilder ConfigureAppConfiguration(Action<HostBuilderContext, IConfigurationBuilder> configureDelegate)
        {
            this.hostBuilder.ConfigureAppConfiguration(configureDelegate);
            return this;
        }

        public IHostBuilder ConfigureServices(Action<HostBuilderContext, IServiceCollection> configureDelegate)
        {
            var hostBuilderContext = new HostBuilderContext(this.Properties);
            this.configureServiceActions.Add((svc) => configureDelegate(hostBuilderContext, svc));
            return this;
        }

        public IHostBuilder UseServiceProviderFactory<TContainerBuilder>(IServiceProviderFactory<TContainerBuilder> factory)
        {
            throw new NotSupportedException();
        }

        public IHostBuilder UseServiceProviderFactory<TContainerBuilder>(Func<HostBuilderContext, IServiceProviderFactory<TContainerBuilder>> factory)
        {
            throw new NotSupportedException();
        }

        public IHostBuilder ConfigureContainer<TContainerBuilder>(Action<HostBuilderContext, TContainerBuilder> configureDelegate)
        {
            this.hostBuilder.ConfigureContainer(configureDelegate);
            return this;
        }

        private void InvokeConfigureServiceActions()
        {
            foreach (var configServices in this.configureServiceActions)
            {
                configServices(this.serviceDescriptors);
            }
        }
    }
}
