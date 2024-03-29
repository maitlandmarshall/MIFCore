﻿using Hangfire;
using Hangfire.Dashboard;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MIFCore.Common;
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

                var webServer = this.GetWebServer();

                if (webServer == WebServer.HttpSys)
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

        private WebServer GetWebServer()
        {
            // If the user has specified a server, use that value
            if (this.aspNetCoreConfig.BindingServer.HasValue)
                return this.aspNetCoreConfig.BindingServer.Value;

            // Otherwise by default, on ports 80 / 443, use HttpSys so the ports can be shared
            if (this.aspNetCoreConfig.BindingPort == 80
                || this.aspNetCoreConfig.BindingPort == 443)
            {
                return WebServer.HttpSys;
            }
            else
            {
                return WebServer.Kestrel;
            }
        }

        private void Configure(IApplicationBuilder app)
        {
            var hangfireConfig = Globals.DefaultConfiguration.GetSection("Hangfire");
            var dashboardTitle = hangfireConfig.GetValue("DashboardTitle", Assembly.GetEntryAssembly()?.GetName().Name ?? "Hangfire");
            var enableRemoteAuth = hangfireConfig.GetValue("EnableRemoteAuth", false);
            var enableDarkMode = hangfireConfig.GetValue("EnableDarkMode", true);

            var dashboardOptions = new DashboardOptions
            {
                DashboardTitle = dashboardTitle,
                DarkModeEnabled = enableDarkMode,
            };

            // If enableRemoteAuth, set the authorization array to null to bypass the default LocalRequestsOnlyAuthorizationFilter
            // https://github.com/HangfireIO/Hangfire/blob/master/src/Hangfire.Core/DashboardOptions.cs#L23
            if (enableRemoteAuth)
            {
                dashboardOptions.Authorization = new IDashboardAuthorizationFilter[] { };
            }

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
