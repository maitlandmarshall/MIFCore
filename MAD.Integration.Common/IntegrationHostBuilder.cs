using Autofac;
using Autofac.Extensions.DependencyInjection;
using Hangfire;
using MAD.Integration.Common.Analytics;
using MAD.Integration.Common.Hangfire;
using MAD.Integration.Common.Http;
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

        public IIntegrationHostBuilder ConfigureServices(Action<IServiceCollection> configureDelegate)
        {
            configureDelegate(this.serviceDescriptors);
            return this;
        }

        public IHost Build()
        {
            this.HandleStartupConfigureServices();

            ContainerBuilder containerBuilder = new ContainerBuilder();
            containerBuilder.Populate(this.serviceDescriptors);

            var rootContainer = containerBuilder.Build();
            var hostBuilder = Host.CreateDefaultBuilder()
                .UseWindowsService()
                .ConfigureLogging(cfg =>
                {
                    cfg.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);
                    cfg.AddFilter("Hangfire", LogLevel.Error);
                })
                .UseServiceProviderFactory(new AutofacChildLifetimeScopeServiceProviderFactory(rootContainer));

            if (rootContainer.TryResolve<AspNetCoreConfig>(out AspNetCoreConfig aspNetCoreConfig))
            {
                hostBuilder.ConfigureWebHostDefaults(webHost =>
                {
                    webHost.UseStartup<WebHostStartup>();

                    if (aspNetCoreConfig.BindingPort == 80 || aspNetCoreConfig.BindingPort == 443)
                    {
                        webHost.UseHttpSys(options =>
                        {
                            options.UrlPrefixes.Add($"http://*:{aspNetCoreConfig.BindingPort}");
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

            IHost host = hostBuilder.Build();

            this.HandleStartupConfigure(host.Services.GetService<IServiceProvider>());

            return host;
        }

        private void HandleStartupConfigureServices()
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

        private void HandleStartupConfigure(IServiceProvider serviceProvider)
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

        public IIntegrationHostBuilder UseHangfire() => this.UseHangfire(null);
        public IIntegrationHostBuilder UseHangfire(Action<IGlobalConfiguration> configureDelegate)
        {
            configureDelegate?.Invoke(GlobalConfiguration.Configuration);
            this.ConfigureServices(y => y.AddHangfire());

            return this;
        }

        public IIntegrationHostBuilder UseAspNetCore() => this.UseAspNetCore(null);
        public IIntegrationHostBuilder UseAspNetCore(Action<AspNetCoreConfig> configureDelegate)
        {
            var config = new AspNetCoreConfig();
            configureDelegate?.Invoke(config);

            this.ConfigureServices(y => y.AddAspNetCore(config));

            return this;
        }

        public IIntegrationHostBuilder UseAppInsights()
        {
            this.ConfigureServices(y => y.AddAppInsights());

            return this;
        }


    }
}
