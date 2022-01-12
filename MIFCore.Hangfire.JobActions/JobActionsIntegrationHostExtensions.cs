using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MIFCore.Common;
using MIFCore.Hangfire.JobActions.Database;
using System;
using System.Collections.Generic;
using System.Text;
using Hangfire;
using Autofac;
using Autofac.Extensions.DependencyInjection;

namespace MIFCore.Hangfire.JobActions
{
    public static class JobActionsIntegrationHostExtensions
    {
        public static IIntegrationHostBuilder UseJobActions(this IIntegrationHostBuilder integrationHostBuilder)
        {
            integrationHostBuilder.StartupHandler.PostConfigureActions += StartupHandler_PostConfigureActions;

            return integrationHostBuilder;
        }

        private static void StartupHandler_PostConfigureActions(IServiceProvider serviceProvider)
        {
            var lifetimeScope = serviceProvider as ILifetimeScope;
            var jobActionsScope = CreateJobActionsLifetimeScope(lifetimeScope);

            // Add the JobActionFilter to the Hangfire filter pipeline, so that the job actions may run for every job
            var globalConfig = jobActionsScope.Resolve<IGlobalConfiguration>();
            globalConfig.UseFilter(jobActionsScope.Resolve<JobActionFilter>());

            // Ensure the JobAction database is up to date and migrated
            var dbContext = jobActionsScope.Resolve<JobActionDbContext>();
            dbContext.Database.Migrate();
        }

        private static ILifetimeScope CreateJobActionsLifetimeScope(ILifetimeScope parentScope)
        {
            // Register a child scope, so the services are not accessible anyone else
            // This prevents EFCore throwing the below error if this library's consumer is also using EFCore:
            // When registering multiple DbContext types, make sure that the constructor for each context type has a DbContextOptions<TContext> parameter rather than a non-generic DbContextOptions parameter.
            var jobActionsScope = parentScope.BeginLifetimeScope("JobActionsScope", cfg =>
            {
                var services = new ServiceCollection();
                services.AddJobActions();

                cfg.Populate(services);
            });

            return jobActionsScope;
        }
    }
}
