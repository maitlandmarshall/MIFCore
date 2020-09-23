﻿using Autofac;
using Hangfire;
using Hangfire.Common;
using MAD.Integration.Common.Analytics;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DependencyCollector;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.QuickPulse;
using Microsoft.Extensions.Hosting;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MAD.Integration.Common.Jobs
{
    public class HangfireBackgroundService : BackgroundService
    {
        private readonly ILifetimeScope rootScope;
        private readonly IGlobalConfiguration globalConfig;

        internal static ILifetimeScope ServiceScope { get; private set; }

        public HangfireBackgroundService(ILifetimeScope rootScope, IGlobalConfiguration config)
        {
            this.rootScope = rootScope;
            this.globalConfig = config;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var appInsights = this.rootScope.ResolveOptional<AppInsightsConfig>();
            var childScope = ServiceScope = this.rootScope.BeginLifetimeScope("HangfireServiceScope");
            var activator = new AutofacLifecycleJobActivator(childScope);
            var options = new BackgroundJobServerOptions()
            {
                Activator = activator,
                Queues = new[] { JobQueue.Alpha, JobQueue.Beta, JobQueue.Default, JobQueue.Low }
            };

            this.globalConfig
                .UseFilter(new BackgroundJobContext());

            if (!string.IsNullOrEmpty(appInsights?.InstrumentationKey))
            {
                var telemetryClient = new TelemetryClient(TelemetryConfigurationFactory.Create(appInsights));
                this.globalConfig.UseFilter(new AppInsightsEventsFilter(telemetryClient));
            }

            using (var server = new BackgroundJobServer(options))
            {
                await server.WaitForShutdownAsync(stoppingToken);
            }
        }
    }
}
