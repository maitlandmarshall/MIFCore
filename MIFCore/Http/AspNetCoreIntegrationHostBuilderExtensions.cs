using MIFCore.Common;
using System;

namespace MIFCore.Http
{
    public static class AspNetCoreIntegrationHostBuilderExtensions
    {
        public static IIntegrationHostBuilder UseAspNetCore(this IIntegrationHostBuilder integrationHostBuilder)
        {
            return integrationHostBuilder.UseAspNetCore(null);
        }

        public static IIntegrationHostBuilder UseAspNetCore(this IIntegrationHostBuilder integrationHostBuilder, Action<AspNetCoreConfig> configureDelegate)
        {
            integrationHostBuilder.ConfigureServices(y => y.AddAspNetCore(configureDelegate));

            return integrationHostBuilder;
        }
    }
}
