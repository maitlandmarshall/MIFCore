using Hangfire;
using MIFCore.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace MIFCore.Http
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

                if (this.aspNetCoreConfig.BindingPort == 80 || this.aspNetCoreConfig.BindingPort == 443)
                {
                    webHost.UseHttpSys(options =>
                    {
                        options.UrlPrefixes.Add($"http://*:{this.aspNetCoreConfig.BindingPort}/{this.aspNetCoreConfig.BindingPath}");
                    });
                }
                else
                {
                    webHost.UseKestrel(options =>
                    {
                        options.ListenAnyIP(this.aspNetCoreConfig.BindingPort);
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

            if (!string.IsNullOrEmpty(this.aspNetCoreConfig.BindingPath)) app.UsePathBase($"/{this.aspNetCoreConfig.BindingPath}");

            app.UseRouting();
            app.UseEndpoints(cfg =>
            {
                cfg.MapControllers();
                cfg.MapHangfireDashboard(dashboardOptions);
            });

            app.UseHangfireDashboard(options: dashboardOptions);
        }
    }
}
