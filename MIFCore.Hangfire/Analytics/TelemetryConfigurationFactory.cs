using Microsoft.ApplicationInsights.DependencyCollector;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.QuickPulse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MIFCore.Hangfire.Analytics
{
    internal class TelemetryConfigurationFactory
    {
        public static TelemetryConfiguration Create(AppInsightsConfig appInsightsConfig)
        {
            var teleConfig = new TelemetryConfiguration(appInsightsConfig.InstrumentationKey);
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

            return teleConfig;
        }

        private static DependencyTrackingTelemetryModule BuildDependencyTrackingTelemetryModule()
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
