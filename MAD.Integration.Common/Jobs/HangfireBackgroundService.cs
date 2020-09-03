using Autofac;
using Hangfire;
using Hangfire.Autofac;
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
            var hangfireRootScope = this.lifetimeScope.BeginLifetimeScope();
            hangfireRootScope.ChildLifetimeScopeBeginning += RootScope_ChildLifetimeScopeBeginning;

            this.config.UseFilter<BackgroundJobContext>(new BackgroundJobContext());
            this.config.UseFilter<BackgroundJobLifecycleEvents>(new BackgroundJobLifecycleEvents());
            this.activator = new AutofacJobActivator(hangfireRootScope);

            BackgroundJobServerOptions options = new BackgroundJobServerOptions()
            {
                Activator = this.activator
            };

            using (BackgroundJobServer server = new BackgroundJobServer(options))
            {
                await server.WaitForShutdownAsync(stoppingToken);
            }
        }

        private void RootScope_ChildLifetimeScopeBeginning(object sender, Autofac.Core.Lifetime.LifetimeScopeBeginningEventArgs e)
        {
            ThreadStaticScope<ILifetimeScope>.Current = e.LifetimeScope;
        }
    }
}
