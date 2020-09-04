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

        public HangfireBackgroundService(ILifetimeScope rootScope, IGlobalConfiguration config)
        {
            this.rootScope = rootScope;
            this.config = config;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var childScope = this.rootScope.BeginLifetimeScope("HangfireServiceScope");
            var activator = new AutofacJobActivator(childScope);
            var options = new BackgroundJobServerOptions()
            {
                Activator = activator,
                Queues = new[] { JobQueue.Alpha, JobQueue.Beta, JobQueue.Default, JobQueue.Low }
            };

            childScope.ChildLifetimeScopeBeginning += this.RootScope_ChildLifetimeScopeBeginning;

            this.config.UseFilter<BackgroundJobContext>(new BackgroundJobContext());
            this.config.UseFilter<BackgroundJobLifecycleEvents>(new BackgroundJobLifecycleEvents());

            using (var server = new BackgroundJobServer(options))
            {
                await server.WaitForShutdownAsync(stoppingToken);
            }
        }

        private void RootScope_ChildLifetimeScopeBeginning(object sender, LifetimeScopeBeginningEventArgs e)
        {
            BackgroundJobContext.ParentBackgroundJobScope = sender as LifetimeScope;
            BackgroundJobContext.CurrentLifetimeScope = e.LifetimeScope;
        }
    }
}
