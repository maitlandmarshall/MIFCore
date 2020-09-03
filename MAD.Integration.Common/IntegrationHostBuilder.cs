using Autofac;
using Autofac.Extensions.DependencyInjection;
using MAD.Integration.Common.Hangfire;
using MAD.Integration.Common.Http;
using MAD.Integration.Common.Jobs;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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

            IContainer rootContainer = containerBuilder.Build();

            IHostBuilder hostBuilder = Host.CreateDefaultBuilder()
                .UseWindowsService()
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

            MethodInfo configure = this.startupRef.GetType().GetMethod("Configure");

            if (configure != null)
            {
                IEnumerable<object> paramsToInject = configure.GetParameters()
                    .Select(y => serviceProvider.GetService(y.ParameterType));

                object invokeResult = configure.Invoke(this.startupRef, paramsToInject.ToArray());

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

        public IIntegrationHostBuilder UseAspNetCore()
        {
            this.ConfigureServices(y => y.AddAspNetCore());

            return this;
        }
    }
}
