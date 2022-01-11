using MIFCore.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace MIFCore.Hangfire.Analytics
{
    public static class AppInsightsServiceCollectionExtensions
    {
        public static IServiceCollection AddAppInsights(this IServiceCollection serviceDescriptors, Action<AppInsightsConfig> configureDelegate = null)
        {
            var appInsightsConfig = new AppInsightsConfig();

            Globals.DefaultConfiguration.Bind(appInsightsConfig);
            configureDelegate?.Invoke(appInsightsConfig);

            serviceDescriptors.AddSingleton(appInsightsConfig);

            return serviceDescriptors;
        }
    }
}
