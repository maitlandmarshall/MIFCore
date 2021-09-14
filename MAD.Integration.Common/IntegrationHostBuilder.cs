using Autofac.Extensions.DependencyInjection;
using Hangfire;
using Hangfire.MAMQSqlExtension;
using Hangfire.SqlServer;
using MAD.Integration.Common.Analytics;
using MAD.Integration.Common.Hangfire;
using MAD.Integration.Common.Http;
using MAD.Integration.Common.Jobs;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace MAD.Integration.Common
{
    public partial class IntegrationHostBuilder : IIntegrationHostBuilder
    {
        private StartupHandler startupHandler = new StartupHandler();

        private readonly IServiceCollection serviceDescriptors = new ServiceCollection();
        private readonly List<Action<IServiceCollection>> configureServiceActions = new List<Action<IServiceCollection>>();
        private readonly IHostBuilder hostBuilder;

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
            this.ConfigureServices(y => y.AddSingleton(startupHandler));
            this.startupHandler.ConfigureServices(this);

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
                    cfg.AddConfiguration(IntegrationHost.DefaultConfiguration);
                });

            // Has AspNetCoreConfig been added through IntegrationHostBuilder.UseAspNetCore()?
            // If it has, extract the singleton instance & extend the host builder with WebHostDefaults
            var aspNetCoreConfigDescriptor = this.serviceDescriptors.FirstOrDefault(y => y.ServiceType == typeof(AspNetCoreConfig));
            if (aspNetCoreConfigDescriptor != null) this.ConfigureAspNetCore(aspNetCoreConfigDescriptor.ImplementationInstance as AspNetCoreConfig, this.hostBuilder);

            var host = this.hostBuilder.Build();
            var hangfireConfig = host.Services.GetService<HangfireConfig>();

            this.startupHandler.Configure(host.Services.GetService<IServiceProvider>());

            return host;
        }

        public IIntegrationHostBuilder ConfigureServices(Action<IServiceCollection> configureDelegate)
        {
            this.configureServiceActions.Add(configureDelegate);
            return this;
        }

        public IIntegrationHostBuilder UseStartup<TStartup>() where TStartup : class, new()
        {
            this.startupHandler.SetStartup<TStartup>();
            return this;
        }

        public IIntegrationHostBuilder UseHangfire()
        {
            this.ConfigureServices(y => y.AddHangfire());

            return this;
        }

        public IIntegrationHostBuilder UseHangfire(Action<IGlobalConfiguration> configureDelegate)
        {
            this.ConfigureServices(y => y.AddHangfire((cfg, svg) => configureDelegate(cfg)));

            return this;
        }
        public IIntegrationHostBuilder UseHangfire(Action<IGlobalConfiguration, HangfireConfig> configureDelegate)
        {
            this.ConfigureServices(y => y.AddHangfire(configureDelegate));

            return this;
        }

        public IIntegrationHostBuilder UseAspNetCore()
        {
            return this.UseAspNetCore(null);
        }

        public IIntegrationHostBuilder UseAspNetCore(Action<AspNetCoreConfig> configureDelegate)
        {
            this.ConfigureServices(y => y.AddAspNetCore(configureDelegate));

            return this;
        }

        public IIntegrationHostBuilder UseAppInsights()
        {
            return this.UseAppInsights(null);
        }

        public IIntegrationHostBuilder UseAppInsights(Action<AppInsightsConfig> configureDelegate)
        {
            this.ConfigureServices(y => y.AddAppInsights(configureDelegate));

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
            this.hostBuilder.ConfigureContainer<TContainerBuilder>(configureDelegate);
            return this;
        }

        private void InvokeConfigureServiceActions()
        {
            foreach (var configServices in this.configureServiceActions)
            {
                configServices(this.serviceDescriptors);
            }
        }

        private void ConfigureAspNetCore(AspNetCoreConfig aspNetCoreConfig, IHostBuilder hostBuilder)
        {
            hostBuilder.ConfigureWebHostDefaults(webHost =>
            {
                webHost.UseStartup<WebHostStartup>();

                if (aspNetCoreConfig.BindingPort == 80 || aspNetCoreConfig.BindingPort == 443)
                {
                    webHost.UseHttpSys(options =>
                    {
                        options.UrlPrefixes.Add($"http://*:{aspNetCoreConfig.BindingPort}/{aspNetCoreConfig.BindingPath}");
                    });
                }
                else
                {
                    webHost.UseKestrel(options =>
                    {
                        options.ListenAnyIP(aspNetCoreConfig.BindingPort);
                    });
                }
            });
        }

    }
}
