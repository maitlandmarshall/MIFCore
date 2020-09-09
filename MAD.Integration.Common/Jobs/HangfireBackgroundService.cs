using Autofac;
using Autofac.Core.Lifetime;
using Hangfire;
using Hangfire.Autofac;
using MAD.Integration.Common.Jobs.Utils;
using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;

namespace MAD.Integration.Common.Jobs
{
    public class HangfireBackgroundService : BackgroundService
    {
        private readonly ILifetimeScope rootScope;
        private readonly IGlobalConfiguration config;

        internal static ILifetimeScope ServiceScope { get; private set; }

        public HangfireBackgroundService(ILifetimeScope rootScope, IGlobalConfiguration config)
        {
            this.rootScope = rootScope;
            this.config = config;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var childScope = ServiceScope = this.rootScope.BeginLifetimeScope("HangfireServiceScope");
            var activator = new AutofacLifecycleJobActivator(childScope);
            var options = new BackgroundJobServerOptions()
            {
                Activator = activator,
                Queues = new[] { JobQueue.Alpha, JobQueue.Beta, JobQueue.Default, JobQueue.Low }
            };

            this.config.UseFilter<BackgroundJobContext>(new BackgroundJobContext());

            using (var server = new BackgroundJobServer(options))
            {
                await server.WaitForShutdownAsync(stoppingToken);
            }
        }
    }
}
