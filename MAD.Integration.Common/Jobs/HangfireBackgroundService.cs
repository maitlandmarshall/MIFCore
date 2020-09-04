using Autofac;
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
        private readonly ILifetimeScope lifetimeScope;
        private readonly IGlobalConfiguration config;

        private AutofacJobActivator activator;

        public HangfireBackgroundService(ILifetimeScope lifetimeScope, IGlobalConfiguration config)
        {
            this.lifetimeScope = lifetimeScope;
            this.config = config;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var childScope = this.lifetimeScope.BeginLifetimeScope();
            var options = new BackgroundJobServerOptions()
            {
                Activator = this.activator,
                Queues = new[] { JobQueue.Alpha, JobQueue.Beta, JobQueue.Default, JobQueue.Low }
            };

            childScope.ChildLifetimeScopeBeginning += this.RootScope_ChildLifetimeScopeBeginning;

            this.config.UseFilter<BackgroundJobContext>(new BackgroundJobContext());
            this.config.UseFilter<BackgroundJobLifecycleEvents>(new BackgroundJobLifecycleEvents());

            this.activator = new AutofacJobActivator(childScope);

            using (var server = new BackgroundJobServer(options))
            {
                await server.WaitForShutdownAsync(stoppingToken);
            }
        }

        private void RootScope_ChildLifetimeScopeBeginning(object sender, Autofac.Core.Lifetime.LifetimeScopeBeginningEventArgs e)
        {
            ThreadStaticValue<ILifetimeScope>.Current = e.LifetimeScope;
        }
    }
}
