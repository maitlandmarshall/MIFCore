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

        public void Configure(IApplicationBuilder app)
        {
            app.UseRouting();
            app.UseEndpoints(cfg => {
                cfg.MapControllers();
                cfg.MapHangfireDashboard();
            });

            app.UseHangfireDashboard();
        }
    }
}
