using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MIFCore.Common;
using MIFCore.Hangfire.JobActions.Database;
using System;
using System.Collections.Generic;
using System.Text;
using Hangfire;

namespace MIFCore.Hangfire.JobActions
{
    public static class JobActionsIntegrationHostExtensions
    {
        public static IIntegrationHostBuilder UseJobActions(this IIntegrationHostBuilder integrationHostBuilder)
        {
            integrationHostBuilder.ConfigureServices(services => services.AddJobActions());
            integrationHostBuilder.StartupHandler.PostConfigureActions += StartupHandler_PostConfigureActions;

            return integrationHostBuilder;
        }

        private static void StartupHandler_PostConfigureActions(IServiceProvider serviceProvider)
        {
            var globalConfig = serviceProvider.GetRequiredService<IGlobalConfiguration>();
            globalConfig.UseFilter(serviceProvider.GetRequiredService<JobActionFilter>());

            var dbContext = serviceProvider.GetRequiredService<JobActionDbContext>();
            dbContext.Database.Migrate();
        }
    }
}
