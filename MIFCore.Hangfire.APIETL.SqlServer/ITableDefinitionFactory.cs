using ETLBox.ControlFlow;
using MIFCore.Hangfire.APIETL.Load;
using System.Threading.Tasks;

namespace MIFCore.Hangfire.APIETL.SqlServer
{
    public interface ITableDefinitionFactory
    {
        Task<TableDefinition> Create(ApiEndpointModel apiEndpointModel);
        Task<TableDefinition> Create(string tableName);
    }
}