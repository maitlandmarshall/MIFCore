using Hangfire;
using MIFCore.Common;
using MIFCore.Hangfire;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("MIFCore.Tests")]
namespace MIFCore
{
    public class IntegrationHost
    {
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
