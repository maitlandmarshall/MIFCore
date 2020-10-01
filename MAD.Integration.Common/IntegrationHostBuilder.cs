using Autofac;
using Autofac.Extensions.DependencyInjection;
using Hangfire;
using MAD.Integration.Common.Analytics;
using MAD.Integration.Common.Hangfire;
using MAD.Integration.Common.Http;
using MAD.Integration.Common.Jobs;
using MAD.Integration.Common.Settings;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace MAD.Integration.Common
{
    public partial class IntegrationHostBuilder : IIntegrationHostBuilder
    {
        private object startupRef;

        private readonly IServiceCollection serviceDescriptors = new ServiceCollection();
        private readonly List<Action<IServiceCollection>> configureServiceActions = new List<Action<IServiceCollection>>();

        public IIntegrationHostBuilder ConfigureServices(Action<IServiceCollection> configureDelegate)
        {
            this.configureServiceActions.Add(configureDelegate);
            return this;
        }

        public IHost Build()
        {
            this.AddStartupConfigureServices();
            this.InvokeConfigureServiceActions();

            var containerBuilder = new ContainerBuilder();
            containerBuilder.Populate(this.serviceDescriptors);

            var rootContainer = containerBuilder.Build();
            var hostBuilder = Host.CreateDefaultBuilder()
                .UseWindowsService()
                .ConfigureLogging(cfg =>
                {
                    cfg.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);
                    cfg.AddFilter("Hangfire", LogLevel.Error);
                })
                .UseServiceProviderFactory(new AutofacChildLifetimeScopeServiceProviderFactory(rootContainer))
                .ConfigureAppConfiguration(cfg =>
                {
                    cfg.Sources.Clear();
                    cfg.AddConfiguration(IntegrationHost.DefaultConfiguration);
                });

            if (rootContainer.TryResolve<AspNetCoreConfig>(out AspNetCoreConfig aspNetCoreConfig))
            {
                this.ConfigureAspNetCore(aspNetCoreConfig, hostBuilder);
            }

            IHost host = hostBuilder.Build();

            this.InvokeStartupConfigure(host.Services.GetService<IServiceProvider>());

            return host;
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

        private void AddStartupConfigureServices()
        {
            if (this.startupRef is null)
                return;

            MethodInfo configureServices = this.startupRef.GetType().GetMethod("ConfigureServices", new[] { typeof(IServiceCollection) });

            if (configureServices != null)
            {
                this.ConfigureServices(sc =>
                {
                    configureServices.Invoke(this.startupRef, new[] { sc });
                });
            }
        }

        private void InvokeStartupConfigure(IServiceProvider serviceProvider)
        {
            if (this.startupRef is null)
                return;

            var configureMethod = this.startupRef.GetType().GetMethod("Configure");
            using var startupScope = serviceProvider.CreateScope();

            if (configureMethod != null)
            {
                IEnumerable<object> paramsToInject = configureMethod.GetParameters()
                    .Select(y => startupScope.ServiceProvider.GetService(y.ParameterType));

                object invokeResult = configureMethod.Invoke(this.startupRef, paramsToInject.ToArray());

                if (invokeResult is Task t)
                    t.Wait();
            }
        }

        public IIntegrationHostBuilder UseStartup<TStartup>() where TStartup : class, new()
        {
            this.startupRef = new TStartup();
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

        public IIntegrationHostBuilder UseAspNetCore() => this.UseAspNetCore(null);
        public IIntegrationHostBuilder UseAspNetCore(Action<AspNetCoreConfig> configureDelegate)
        {
            this.ConfigureServices(y => y.AddAspNetCore(configureDelegate));

            return this;
        }

        public IIntegrationHostBuilder UseAppInsights() => this.UseAppInsights(null);

        public IIntegrationHostBuilder UseAppInsights(Action<AppInsightsConfig> configureDelegate)
        {
            this.ConfigureServices(y => y.AddAppInsights(configureDelegate));

            return this;
        }


    }
}
