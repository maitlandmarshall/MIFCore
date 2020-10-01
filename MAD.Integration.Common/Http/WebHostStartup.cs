using Autofac;
using Autofac.Extensions.DependencyInjection;
using Hangfire;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace MAD.Integration.Common.Http
{
    public class WebHostStartup
    {
        public void ConfigureServices(IServiceCollection serviceDescriptors)
        {
            serviceDescriptors.AddHangfire(_ => { });

            serviceDescriptors.AddMvcCore()
                .AddApplicationPart(Assembly.GetEntryAssembly());
        }

        public void Configure(IApplicationBuilder app, AspNetCoreConfig aspNetCoreConfig)
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
