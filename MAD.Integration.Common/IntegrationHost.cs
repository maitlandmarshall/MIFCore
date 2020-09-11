using Hangfire;
using Hangfire.SqlServer;
using MAD.Integration.Common.Settings;
using Microsoft.Extensions.Configuration;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("MAD.Integration.Common.Tests")]
namespace MAD.Integration.Common
{
    public class IntegrationHost
    {
        internal static IConfiguration DefaultConfiguration { get; } = new ConfigurationBuilder().UseSettingsFile().Build();

        public static IIntegrationHostBuilder CreateDefaultBuilder()
        {
            return new IntegrationHostBuilder()
                .UseHangfire((globalHangfireConfig, hangfireServiceConfig) =>
                {
                    if (string.IsNullOrEmpty(hangfireServiceConfig.ConnectionString))
                        return;

                    globalHangfireConfig
                        .UseSqlServerStorage(hangfireServiceConfig.ConnectionString, new SqlServerStorageOptions
                        {
                            SchemaName = "job"
                        })
                        .UseRecommendedSerializerSettings();
                });
        }
    }
}
