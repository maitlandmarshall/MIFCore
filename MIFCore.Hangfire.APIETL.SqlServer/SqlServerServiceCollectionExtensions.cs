using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MIFCore.Hangfire.APIETL.Load;
using System;

namespace MIFCore.Hangfire.APIETL.SqlServer
{
    public static class SqlServerServiceCollectionExtensions
    {
        public static IServiceCollection AddApiEndpointsSqlServerDestination(this IServiceCollection serviceDescriptors, Action<SqlServerConfig> configAction = null)
        {
            serviceDescriptors.TryAddTransient<ICreateDestination, DefaultSqlServerCreateDestination>();
            serviceDescriptors.TryAddTransient<ILoadData, DefaultSqlServerLoadData>();
            serviceDescriptors.TryAddTransient<ISqlConnectionManagerFactory, SqlConnectionManagerFactory>();
            serviceDescriptors.TryAddTransient<ITableDefinitionFactory, TableDefinitionFactory>();
            serviceDescriptors.TryAddTransient<IGetDestinationType, SqlServerGetDestinationType>();

            serviceDescriptors.AddSingleton<SqlServerConfig>((svc) =>
            {
                var sqlServerConfig = new SqlServerConfig();

                if (configAction != null)
                {
                    configAction(sqlServerConfig);
                }
                else
                {
                    // If no configAction is supplied, default to the Hangfire config.
                    var hangfireConfig = svc.GetRequiredService<HangfireConfig>();
                    sqlServerConfig.DestinationConnectionString = hangfireConfig.ConnectionString;
                }

                return sqlServerConfig;
            });

            return serviceDescriptors;
        }
    }
}
