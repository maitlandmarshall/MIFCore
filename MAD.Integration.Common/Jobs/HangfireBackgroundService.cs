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
    public class HangfireBackgroundService : BackgroundService
    {
        private readonly ILifetimeScope rootScope;
        private readonly IGlobalConfiguration globalConfig;
        private readonly HangfireConfig hangfireConfig;

        internal static ILifetimeScope ServiceScope { get; private set; }

        public HangfireBackgroundService(ILifetimeScope rootScope, IGlobalConfiguration config, HangfireConfig hangfireConfig)
        {
            this.rootScope = rootScope;
            this.globalConfig = config;
            this.hangfireConfig = hangfireConfig;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (string.IsNullOrEmpty(hangfireConfig.ConnectionString))
                throw new ArgumentNullException(nameof(this.hangfireConfig.ConnectionString));

            var queues = this.hangfireConfig.Queues ?? new[] { JobQueue.Alpha, JobQueue.Beta, JobQueue.Default, JobQueue.Low };
            var appInsights = this.rootScope.ResolveOptional<AppInsightsConfig>();
            var childScope = ServiceScope = this.rootScope.BeginLifetimeScope("HangfireServiceScope");
            var activator = new AutofacLifecycleJobActivator(childScope);
            var options = new BackgroundJobServerOptions()
            {
                Activator = activator,
                Queues = queues
            };
            var jobStorage = new MAMQSqlServerStorage(this.hangfireConfig.ConnectionString, new SqlServerStorageOptions
            {
                SchemaName = "job"
            }, queues);

            if (!string.IsNullOrEmpty(appInsights?.InstrumentationKey))
            {
                var telemetryClient = new TelemetryClient(TelemetryConfigurationFactory.Create(appInsights));
                this.globalConfig.UseFilter(new AppInsightsEventsFilter(telemetryClient));
            }

            this.CreateDatabaseIfNotExist(this.hangfireConfig.ConnectionString);

            using (var server = new BackgroundJobServer(options, jobStorage))
            {
                await server.WaitForShutdownAsync(stoppingToken);
            }
        }

        private void CreateDatabaseIfNotExist(string connectionString)
        {
            var connectionStringBuilder = new SqlConnectionStringBuilder(connectionString);
            var dbName = connectionStringBuilder.InitialCatalog;

            connectionStringBuilder.InitialCatalog = "master";

            using var sqlConnection = new SqlConnection(connectionStringBuilder.ToString());
            using var cmd = sqlConnection.CreateCommand();

            cmd.CommandText = @$"IF NOT EXISTS (SELECT name FROM master.sys.databases WHERE name = N'{dbName}') CREATE DATABASE [{dbName}]";

            sqlConnection.Open();
            cmd.ExecuteNonQuery();
        }

    }
}
