using Autofac;
using Hangfire;
using Hangfire.MAMQSqlExtension;
using Hangfire.SqlServer;
using MIFCore.Common;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("MIFCore.Tests")]
namespace MIFCore.Hangfire
{
    internal class HangfireBackgroundService : BackgroundService
    {
        private readonly ILifetimeScope rootScope;
        private readonly HangfireConfig hangfireConfig;
        private readonly StartupHandler startupHandler;

        internal static ILifetimeScope ServiceScope { get; private set; }

        public HangfireBackgroundService(ILifetimeScope rootScope, HangfireConfig hangfireConfig, StartupHandler startupHandler)
        {
            this.rootScope = rootScope;
            this.hangfireConfig = hangfireConfig;
            this.startupHandler = startupHandler;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (string.IsNullOrEmpty(this.hangfireConfig.ConnectionString))
                throw new ArgumentNullException(nameof(this.hangfireConfig.ConnectionString));

            var childScope = ServiceScope = this.rootScope.BeginLifetimeScope("HangfireServiceScope");
            var activator = new AutofacLifecycleJobActivator(childScope);
            var options = new BackgroundJobServerOptions()
            {
                Activator = activator,
                Queues = this.hangfireConfig.Queues ?? JobQueue.Queues
            };

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

            var jobStorage = new MAMQSqlServerStorage(this.hangfireConfig.ConnectionString, options, this.hangfireConfig.Queues ?? JobQueue.Queues);
            JobStorage.Current = jobStorage;
        }
    }
}
