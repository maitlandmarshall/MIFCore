using Hangfire;
using Hangfire.SqlServer;
using MAD.Integration.Common.Settings;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("MAD.Integration.Common.Tests")]
namespace MAD.Integration.Common
{
    public class IntegrationHost
    {
        internal static IConfiguration DefaultConfiguration { get; } = new ConfigurationBuilder().UseSettingsFile().Build();

        public static IIntegrationHostBuilder CreateDefaultBuilder(string[] args)
        {
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };

            return new IntegrationHostBuilder(args)
                .UseHangfire((globalHangfireConfig, hangfireServiceConfig) =>
                {
                    globalHangfireConfig.UseRecommendedSerializerSettings();
                });
        }

        public static IIntegrationHostBuilder CreateDefaultBuilder() => CreateDefaultBuilder(null);
    }
}
