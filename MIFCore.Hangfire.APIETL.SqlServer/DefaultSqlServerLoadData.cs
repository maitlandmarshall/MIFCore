using ETLBox.DataFlow;
using ETLBox.DataFlow.Connectors;
using MIFCore.Hangfire.APIETL.Load;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;

namespace MIFCore.Hangfire.APIETL.SqlServer
{
    internal class DefaultSqlServerLoadData : ILoadData
    {
        private readonly ISqlConnectionManagerFactory sqlConnectionManagerFactory;
        private readonly ITableDefinitionFactory tableDefinitionFactory;

        public DefaultSqlServerLoadData(ISqlConnectionManagerFactory sqlConnectionManagerFactory, ITableDefinitionFactory tableDefinitionFactory)
        {
            this.sqlConnectionManagerFactory = sqlConnectionManagerFactory;
            this.tableDefinitionFactory = tableDefinitionFactory;
        }

        public async Task OnLoadData(LoadDataArgs args)
        {
            var apiEndpointModel = args.ApiEndpointModel;
            var tableDefinition = await this.tableDefinitionFactory.Create(args.ApiEndpointModel.DestinationName);
            var connMan = this.sqlConnectionManagerFactory.Create();

            var source = new MemorySource(this.GetDataToLoad(args));
            var merge = new DbMerge(connMan, apiEndpointModel.DestinationName)
            {
                DestinationTableDefinition = tableDefinition,
                MergeMode = MergeMode.InsertsAndUpdates,
                ColumnMapping = apiEndpointModel
                    .MappedProperties
                    .Values
                    .Select(y => new ColumnMap
                    {
                        PropertyName = y.SourceName,
                        DbColumnName = y.DestinationName
                    })
                    .ToList(),
                MergeProperties =
                    {
                        IdColumns = apiEndpointModel
                            .MappedProperties
                            .Values
                            .Where(y => y.IsKey)
                            .Select(y => new IdColumn { IdPropertyName = y.SourceName })
                            .ToList()
                    }
            };

            source.LinkTo(merge);
            await source.ExecuteAsync();
        }

        private List<ExpandoObject> GetDataToLoad(LoadDataArgs args)
        {
            var data = args.DataToLoad;
            return data.Cast<ExpandoObject>().ToList();
        }
    }
}
