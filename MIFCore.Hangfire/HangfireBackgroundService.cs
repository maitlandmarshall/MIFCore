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
        private readonly JobStorageFactory jobStorageFactory;

        internal static ILifetimeScope ServiceScope { get; private set; }

        public HangfireBackgroundService(ILifetimeScope rootScope, HangfireConfig hangfireConfig, StartupHandler startupHandler, JobStorageFactory jobStorageFactory)
        {
            this.rootScope = rootScope;
            this.hangfireConfig = hangfireConfig;
            this.startupHandler = startupHandler;
            this.jobStorageFactory = jobStorageFactory;
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(this.hangfireConfig.ConnectionString))
                throw new ArgumentNullException(nameof(this.hangfireConfig.ConnectionString));

            // .NET 10 runs ExecuteAsync entirely in the background. Complete database
            // initialization and schedule registration before reporting successful startup.
            await this.InitializeStorage();
            this.startupHandler.PostConfigure(this.rootScope as IServiceProvider);
            await base.StartAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var childScope = ServiceScope = this.rootScope.BeginLifetimeScope("HangfireServiceScope");
            var activator = new AutofacLifecycleJobActivator(childScope);
            var options = new BackgroundJobServerOptions()
            {
                Activator = activator,
                Queues = this.hangfireConfig.Queues ?? JobQueue.Queues
            };

            using (var server = new BackgroundJobServer(options))
            {
                await server.WaitForShutdownAsync(stoppingToken);
            }
        }

        private async Task InitializeStorage()
        {
            await this.startupHandler.CreateDatabaseIfNotExist(this.hangfireConfig.ConnectionString);

            var jobStorage = this.jobStorageFactory.Create(this.hangfireConfig.ConnectionString, this.hangfireConfig.Queues ?? JobQueue.Queues);
            JobStorage.Current = jobStorage;
        }
    }
}
