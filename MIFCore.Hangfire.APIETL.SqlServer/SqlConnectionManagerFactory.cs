using ETLBox.Connection;

namespace MIFCore.Hangfire.APIETL.SqlServer
{
    internal class SqlConnectionManagerFactory
    {
        private readonly SqlServerConfig sqlServerConfig;

        public SqlConnectionManagerFactory(SqlServerConfig sqlServerConfig)
        {
            this.sqlServerConfig = sqlServerConfig;
        }

        public SqlConnectionManager Create()
        {
            return new SqlConnectionManager(this.sqlServerConfig.DestinationConnectionString);
        }
    }
}
