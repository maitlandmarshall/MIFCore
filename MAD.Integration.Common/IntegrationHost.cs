using Hangfire;
using Hangfire.SqlServer;
using MAD.Integration.Common.Settings;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Data.SqlClient;
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
                    if (string.IsNullOrEmpty(hangfireServiceConfig.ConnectionString))
                        return;

                    CreateDatabaseIfNotExist(hangfireServiceConfig.ConnectionString);

                    globalHangfireConfig
                        .UseSqlServerStorage(hangfireServiceConfig.ConnectionString, new SqlServerStorageOptions
                        {
                            SchemaName = "job"
                        })
                        .UseRecommendedSerializerSettings();
                });
        }

        public static IIntegrationHostBuilder CreateDefaultBuilder() => CreateDefaultBuilder(null);

        private static void CreateDatabaseIfNotExist(string connectionString)
        {
            var connectionStringBuilder = new SqlConnectionStringBuilder(connectionString);
            var dbName = connectionStringBuilder.InitialCatalog;

            connectionStringBuilder.InitialCatalog = "master";

            using var sqlConnection = new SqlConnection(connectionStringBuilder.ToString());
            using var cmd = sqlConnection.CreateCommand();

            cmd.CommandText = @$"IF NOT EXISTS (SELECT name FROM master.sys.databases WHERE name = N'{dbName}') CREATE DATABASE [{dbName}]";

            sqlConnection.Open();
            cmd.ExecuteNonQuery();
        }
    }
}
