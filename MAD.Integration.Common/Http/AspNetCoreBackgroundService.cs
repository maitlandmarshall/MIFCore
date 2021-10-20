using Autofac;
using Autofac.Extensions.DependencyInjection;
using Hangfire;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace MAD.Integration.Common.Http
{
    internal class AspNetCoreBackgroundService : BackgroundService
    {
        private readonly AspNetCoreConfig aspNetCoreConfig;
        private readonly StartupHandler startupHandler;

        public AspNetCoreBackgroundService(AspNetCoreConfig aspNetCoreConfig, StartupHandler startupHandler)
        {
            this.aspNetCoreConfig = aspNetCoreConfig;
            this.startupHandler = startupHandler;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await this.startupHandler.WaitForPostConfigure();

            var hostBuilder = Host.CreateDefaultBuilder();
            hostBuilder.ConfigureWebHostDefaults(webHost =>
            {
                webHost.Configure(this.Configure);
                webHost.ConfigureServices(svc =>
                {
                    svc.AddHangfire(_ => { });
                    svc.AddMvcCore().AddApplicationPart(Assembly.GetEntryAssembly());
                    this.startupHandler.ConfigureServices(svc);
                });

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

            await hostBuilder.Build().RunAsync(stoppingToken);
        }


        private void Configure(IApplicationBuilder app)
        {
            var dashboardOptions = new DashboardOptions
            {
                DashboardTitle = $"{Assembly.GetEntryAssembly().GetName().Name} Dashboard"
            };

            if (!string.IsNullOrEmpty(aspNetCoreConfig.BindingPath)) app.UsePathBase($"/{aspNetCoreConfig.BindingPath}");

            app.UseRouting();
            app.UseEndpoints(cfg => {
                cfg.MapControllers();
                cfg.MapHangfireDashboard(dashboardOptions);
            });

            app.UseHangfireDashboard(options: dashboardOptions);
        }
    }
}
