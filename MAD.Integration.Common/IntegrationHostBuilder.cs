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
        private object startupRef;

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
            this.AddStartupConfigureServices();
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
            
            this.InvokeStartupConfigure(host.Services.GetService<IServiceProvider>());

            // Configure HangfireStorage after Startup.Configure so the library consumer can choose the storage if they want
            // if they haven't and JobStorage.Current is null, configure it.
            var hangfireConfig = host.Services.GetService<HangfireConfig>();

            if (hangfireConfig != null
                && JobStorage.Current == null)
                this.ConfigureHangfireStorage(hangfireConfig);

            return host;
        }

        public IIntegrationHostBuilder ConfigureServices(Action<IServiceCollection> configureDelegate)
        {
            this.configureServiceActions.Add(configureDelegate);
            return this;
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
                    .Select(y => startupScope.ServiceProvider.GetRequiredService(y.ParameterType));

                object invokeResult = configureMethod.Invoke(this.startupRef, paramsToInject.ToArray());

                if (invokeResult is Task t)
                    t.Wait();
            }
        }

        private void ConfigureHangfireStorage(HangfireConfig hangfireConfig)
        {
            var jobStorage = new MAMQSqlServerStorage(hangfireConfig.ConnectionString, new SqlServerStorageOptions
            {
                SchemaName = "job"
            }, hangfireConfig.Queues ?? JobQueue.Queues);

            JobStorage.Current = jobStorage;
        }
    }
}
