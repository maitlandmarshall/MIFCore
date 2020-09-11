using Hangfire;
using MAD.Integration.Common.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Hangfire.SqlServer;
using System.Threading;
using System.Threading.Tasks;

[assembly: InternalsVisibleTo("MAD.Integration.Common.Tests")]
namespace MAD.Integration.Common
{
    public class IntegrationHost
    {
        internal static IConfiguration DefaultConfiguration { get; } = new ConfigurationBuilder().UseSettingsFile().Build();

        public static IIntegrationHostBuilder CreateDefaultBuilder()
        {
            return new IntegrationHostBuilder()
                .UseHangfire((gc, cfg) =>
                {
                    gc
                        .UseSqlServerStorage(cfg.ConnectionString, new SqlServerStorageOptions
                        {
                            SchemaName = "jobs"
                        })
                        .UseRecommendedSerializerSettings();
                });
        }
    }
}
