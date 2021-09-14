using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace MAD.Integration.Common
{
    internal class StartupHandler
    {
        public object Startup { get; private set; }

        public void SetStartup<T>() where T : class, new()
        {
            this.Startup = new T();
        }

        public void ConfigureServices(IIntegrationHostBuilder integrationHostBuilder)
        {
            if (this.Startup is null)
                return;

            var configureServices = this.Startup.GetType().GetMethod(nameof(ConfigureServices), new[] { typeof(IServiceCollection) });

            if (configureServices != null)
            {
                integrationHostBuilder.ConfigureServices(sc =>
                {
                    configureServices.Invoke(this.Startup, new[] { sc });
                });
            }
        }

        public void Configure(IServiceProvider serviceProvider)
        {
            if (this.Startup is null)
                return;

            this.RunMethodAndInject(serviceProvider, nameof(Configure));
        }

        public void PostConfigure(IServiceProvider serviceProvider)
        {
            if (this.Startup is null)
                return;

            this.RunMethodAndInject(serviceProvider, nameof(PostConfigure));
        }

        public async Task CreateDatabaseIfNotExist(string connectionString)
        {
            var connectionStringBuilder = new SqlConnectionStringBuilder(connectionString);
            var dbName = connectionStringBuilder.InitialCatalog;

            connectionStringBuilder.InitialCatalog = "master";

            using var sqlConnection = new SqlConnection(connectionStringBuilder.ToString());
            using var cmd = sqlConnection.CreateCommand();

            cmd.CommandText = @$"IF NOT EXISTS (SELECT name FROM master.sys.databases WHERE name = N'{dbName}') CREATE DATABASE [{dbName}]";

            await sqlConnection.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }

        private void RunMethodAndInject(IServiceProvider serviceProvider, string methodName)
        {
            var methodToRun = this.Startup.GetType().GetMethod(methodName);

            if (methodToRun is null)
                return;

            using var startupScope = serviceProvider.CreateScope();
            var paramsToInject = methodToRun.GetParameters()
                .Select(y => startupScope.ServiceProvider.GetRequiredService(y.ParameterType));

            object invokeResult = methodToRun.Invoke(this.Startup, paramsToInject.ToArray());

            if (invokeResult is Task t)
                t.Wait();
        }
    }
}
