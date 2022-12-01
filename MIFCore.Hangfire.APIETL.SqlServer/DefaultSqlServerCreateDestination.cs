using ETLBox.Connection;
using ETLBox.ControlFlow;
using ETLBox.ControlFlow.Tasks;
using MIFCore.Hangfire.APIETL.Load;
using System.Linq;
using System.Threading.Tasks;

namespace MIFCore.Hangfire.APIETL.SqlServer
{
    internal class DefaultSqlServerCreateDestination : ICreateDestination
    {
        private readonly HangfireConfig hangfireConfig;

        public DefaultSqlServerCreateDestination(HangfireConfig hangfireConfig)
        {
            this.hangfireConfig = hangfireConfig;
        }

        public async Task OnCreateDestination(CreateDestinationArgs args)
        {
            var apiEndpointModel = args.ApiEndpointModel;
            var connMan = new SqlConnectionManager(this.hangfireConfig.ConnectionString);

            if (IfTableOrViewExistsTask.IsExisting(connMan, apiEndpointModel.DestinationName) == false)
            {
                var tableDefinition = new TableDefinition(
                    name: apiEndpointModel.DestinationName,
                    columns: apiEndpointModel.MappedProperties.Select(kvp =>
                    {
                        var (key, value) = kvp;
                        return new TableColumn(value.DestinationName, value.DestinationType, value.IsKey == false, value.IsKey, false);
                    }).ToList());

                await Task.Run(() =>
                {
                    CreateTableTask.Create(connMan, tableDefinition);
                });
            }
        }
    }
}
