using Autofac;
using Hangfire;
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
        private readonly IGlobalConfiguration config;

        internal static ILifetimeScope ServiceScope { get; private set; }

        public HangfireBackgroundService(ILifetimeScope rootScope, IGlobalConfiguration config)
        {
            this.rootScope = rootScope;
            this.config = config;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var appInsights = this.rootScope.Resolve<AppInsightsConfig>();
            var childScope = ServiceScope = this.rootScope.BeginLifetimeScope("HangfireServiceScope");
            var activator = new AutofacLifecycleJobActivator(childScope);
            var options = new BackgroundJobServerOptions()
            {
                Activator = activator,
                Queues = new[] { JobQueue.Alpha, JobQueue.Beta, JobQueue.Default, JobQueue.Low }
            };

            this.config.UseFilter<BackgroundJobContext>(new BackgroundJobContext());

            if (!string.IsNullOrEmpty(appInsights?.InstrumentationKey))
            {
                var teleConfig = new TelemetryConfiguration(appInsights.InstrumentationKey);
                var teleBuilder = teleConfig.TelemetryProcessorChainBuilder;

                QuickPulseTelemetryProcessor quickPulseTelemetryProcessor = null;
                teleBuilder.Use(next =>
                {
                    quickPulseTelemetryProcessor = new QuickPulseTelemetryProcessor(next);
                    return quickPulseTelemetryProcessor;
                });

                var quickPulse = new QuickPulseTelemetryModule();
                quickPulse.Initialize(teleConfig);
                quickPulse.RegisterTelemetryProcessor(quickPulseTelemetryProcessor);

                var depModule = this.BuildDependencyTrackingTelemetryModule();
                depModule.Initialize(teleConfig);

                var teleclient = new TelemetryClient(teleConfig);
                this.config.UseFilter<AppInsightsEventsFilter>(new AppInsightsEventsFilter(teleclient));
            }

            using (var server = new BackgroundJobServer(options))
            {
                await server.WaitForShutdownAsync(stoppingToken);
            }
        }

        private DependencyTrackingTelemetryModule BuildDependencyTrackingTelemetryModule()
        {
            List<string> excludeComponentCorrelationHttpHeadersOnDomains = new List<string>
            {
                "core.windows.net",
                "core.chinacloudapi.cn",
                "core.cloudapi.de",
                "core.usgovcloudapi.net",
                "localhost",
                "127.0.0.1"
            };

            DependencyTrackingTelemetryModule depModule = new DependencyTrackingTelemetryModule();

            foreach (string excludeComponent in excludeComponentCorrelationHttpHeadersOnDomains)
                depModule.ExcludeComponentCorrelationHttpHeadersOnDomains.Add(excludeComponent);

            depModule.IncludeDiagnosticSourceActivities.Add("Microsoft.Azure.ServiceBus");
            depModule.IncludeDiagnosticSourceActivities.Add("Microsoft.Azure.EventHubs");

            return depModule;
        }
    }
}
