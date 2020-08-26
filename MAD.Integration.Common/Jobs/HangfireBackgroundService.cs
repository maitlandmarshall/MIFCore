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

        public HangfireBackgroundService(ILifetimeScope lifetimeScope)
        {
            this.lifetimeScope = lifetimeScope;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            BackgroundJobServerOptions options = new BackgroundJobServerOptions()
            {
                Activator = new AutofacJobActivator(this.lifetimeScope.BeginLifetimeScope())
            };

            using (BackgroundJobServer server = new BackgroundJobServer(options))
            {
                await server.WaitForShutdownAsync(stoppingToken);
            }
        }
    }
}
