﻿using ETLBox.ControlFlow;
using MIFCore.Hangfire.APIETL.Load;
using System.Linq;
using System.Threading.Tasks;

namespace MIFCore.Hangfire.APIETL.SqlServer
{
    internal class TableDefinitionFactory : ITableDefinitionFactory
    {
        private readonly ISqlConnectionManagerFactory sqlConnectionManagerFactory;

        public TableDefinitionFactory(ISqlConnectionManagerFactory sqlConnectionManagerFactory)
        {
            this.sqlConnectionManagerFactory = sqlConnectionManagerFactory;
        }

        public async Task<TableDefinition> Create(string tableName)
        {
            if (tableName.Contains(".") == false)
            {
                tableName = $"dbo.{tableName}";
            }

            var connMan = this.sqlConnectionManagerFactory.Create();
            return await Task.Run(() => TableDefinition.FromTableName(connMan, tableName));
        }

        public Task<TableDefinition> Create(ApiEndpointModel apiEndpointModel)
        {
            var tableDefinition = new TableDefinition(
                   name: apiEndpointModel.DestinationName,
                   columns: apiEndpointModel.MappedProperties.Select(kvp =>
                   {
                       var (key, value) = kvp;
                       return new TableColumn(value.DestinationName, value.DestinationType, value.IsKey == false, value.IsKey, false);
                   }).ToList());

            return Task.FromResult(tableDefinition);
        }
    }
}
