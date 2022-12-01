using ETLBox.ControlFlow.Tasks;
using MIFCore.Hangfire.APIETL.Load;
using System.Threading.Tasks;

namespace MIFCore.Hangfire.APIETL.SqlServer
{
    internal class DefaultSqlServerCreateDestination : ICreateDestination
    {
        private readonly SqlConnectionManagerFactory sqlConnectionManagerFactory;
        private readonly TableDefinitionFactory tableDefinitionFactory;

        public DefaultSqlServerCreateDestination(SqlConnectionManagerFactory sqlConnectionManagerFactory, TableDefinitionFactory tableDefinitionFactory)
        {
            this.sqlConnectionManagerFactory = sqlConnectionManagerFactory;
            this.tableDefinitionFactory = tableDefinitionFactory;
        }

        public async Task OnCreateDestination(CreateDestinationArgs args)
        {
            var apiEndpointModel = args.ApiEndpointModel;
            var connMan = this.sqlConnectionManagerFactory.Create();

            if (IfTableOrViewExistsTask.IsExisting(connMan, apiEndpointModel.DestinationName) == false)
            {
                var tableDefinition = await this.tableDefinitionFactory.Create(apiEndpointModel);

                await Task.Run(() =>
                {
                    CreateTableTask.Create(connMan, tableDefinition);
                });
            }
        }
    }
}
