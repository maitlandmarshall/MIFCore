using Autofac;
using Autofac.Extensions.DependencyInjection;
using Hangfire;
using Hangfire.SqlServer;
using MAD.Integration.Common.Jobs;
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

        public void Configure(IApplicationBuilder app, AspNetCoreConfig aspNetCoreConfig, HangfireConfig hangfireConfig)
        {
            var dashboardOptions = new DashboardOptions
            {
                DashboardTitle = $"{Assembly.GetEntryAssembly().GetName().Name} Dashboard"
            };

            if (!string.IsNullOrEmpty(aspNetCoreConfig.BindingPath)) app.UsePathBase($"/{aspNetCoreConfig.BindingPath}");

            var jobStorage = new SqlServerStorage(hangfireConfig.ConnectionString, new SqlServerStorageOptions
            {
                SchemaName = "job"
            });

            app.UseRouting();
            app.UseEndpoints(cfg => {
                cfg.MapControllers();
                cfg.MapHangfireDashboard(dashboardOptions, jobStorage);
            });

            app.UseHangfireDashboard(options: dashboardOptions, storage: jobStorage);
        }
    }
}
