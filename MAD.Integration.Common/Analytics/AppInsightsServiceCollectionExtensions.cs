using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace MAD.Integration.Common.Analytics
{
    public static class AppInsightsServiceCollectionExtensions
    {
        public static IServiceCollection AddAppInsights(this IServiceCollection serviceDescriptors, Action<AppInsightsConfig> configureDelegate = null)
        {
            var appInsightsConfig = new AppInsightsConfig();

            IntegrationHost.DefaultConfiguration.Bind(appInsightsConfig);
            configureDelegate?.Invoke(appInsightsConfig);

            serviceDescriptors.AddSingleton<AppInsightsConfig>(appInsightsConfig);

            return serviceDescriptors;
        }
    }
}
