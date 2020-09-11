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

        public IIntegrationHostBuilder ConfigureServices(Action<IServiceCollection> configureDelegate)
        {
            configureDelegate(this.serviceDescriptors);
            return this;
        }

        public IHost Build()
        {
            this.BuildSettingsFile();
            this.InvokeStartupConfigureServices();

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

        private void ConfigureAspNetCore(AspNetCoreConfig aspNetCoreConfig, IHostBuilder hostBuilder)
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

        private void BuildSettingsFile()
        {
            var settingsPath = Path.Combine(Globals.BaseDirectory, "settings.json");
            var defaultSettings = new FileInfo("settings.default.json");
            var settings = new FileInfo(settingsPath);

            if (!settings.Exists)
            {
                if (defaultSettings.Exists)
                {
                    defaultSettings.CopyTo(settingsPath);
                }
                else
                {
                    settings.Create().Dispose();
                }
            }
        }

        private void InvokeStartupConfigureServices()
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

        public IIntegrationHostBuilder UseHangfire() => this.UseHangfire(null);
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

        public IIntegrationHostBuilder UseAppInsights()
        {
            this.ConfigureServices(y => y.AddAppInsights());

            return this;
        }


    }
}
