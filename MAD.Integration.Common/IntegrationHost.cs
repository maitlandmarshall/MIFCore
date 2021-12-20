using Hangfire;
using MAD.Integration.Common.Jobs;
using MAD.Integration.Common.Settings;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("MAD.Integration.Common.Tests")]
namespace MAD.Integration.Common
{
    public class IntegrationHost
    {
        public static IConfiguration DefaultConfiguration { get; } = new ConfigurationBuilder().UseSettingsFile().Build();

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
                    globalHangfireConfig.UseFilter(new BackgroundJobContext());
                });
        }

        public static IIntegrationHostBuilder CreateDefaultBuilder() => CreateDefaultBuilder(null);
    }
}
