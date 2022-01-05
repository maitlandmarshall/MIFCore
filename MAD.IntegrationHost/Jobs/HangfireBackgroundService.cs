using Autofac;
using Hangfire;
using Hangfire.Common;
using Hangfire.MAMQSqlExtension;
using Hangfire.SqlServer;
using MAD.Integration.Common.Analytics;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DependencyCollector;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.QuickPulse;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;

namespace MAD.Integration.Common.Jobs
{
    internal class HangfireBackgroundService : BackgroundService
    {
        private readonly ILifetimeScope rootScope;
        private readonly IGlobalConfiguration globalConfig;
        private readonly HangfireConfig hangfireConfig;
        private readonly StartupHandler startupHandler;

        internal static ILifetimeScope ServiceScope { get; private set; }

        public HangfireBackgroundService(ILifetimeScope rootScope, IGlobalConfiguration config, HangfireConfig hangfireConfig, StartupHandler startupHandler)
        {
            this.rootScope = rootScope;
            this.globalConfig = config;
            this.hangfireConfig = hangfireConfig;
            this.startupHandler = startupHandler;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (string.IsNullOrEmpty(hangfireConfig.ConnectionString))
                throw new ArgumentNullException(nameof(this.hangfireConfig.ConnectionString));

            var appInsights = this.rootScope.ResolveOptional<AppInsightsConfig>();
            var childScope = ServiceScope = this.rootScope.BeginLifetimeScope("HangfireServiceScope");
            var activator = new AutofacLifecycleJobActivator(childScope);
            var options = new BackgroundJobServerOptions()
            {
                Activator = activator,
                Queues = this.hangfireConfig.Queues ?? JobQueue.Queues
            };

            if (!string.IsNullOrEmpty(appInsights?.InstrumentationKey))
            {
                var telemetryClient = new TelemetryClient(TelemetryConfigurationFactory.Create(appInsights));
                this.globalConfig.UseFilter(new AppInsightsEventsFilter(telemetryClient));
            }

            await this.InitializeStorage();
            this.startupHandler.PostConfigure(this.rootScope as IServiceProvider);

            using (var server = new BackgroundJobServer(options))
            {
                await server.WaitForShutdownAsync(stoppingToken);
            }
        }

        private async Task InitializeStorage()
        {
            await this.startupHandler.CreateDatabaseIfNotExist(this.hangfireConfig.ConnectionString);

            var options = new SqlServerStorageOptions
            {
                SchemaName = "job",
                PrepareSchemaIfNecessary = true
            };

            var jobStorage = new MAMQSqlServerStorage(hangfireConfig.ConnectionString, options, hangfireConfig.Queues ?? JobQueue.Queues);
            JobStorage.Current = jobStorage;
        }
    }
}
