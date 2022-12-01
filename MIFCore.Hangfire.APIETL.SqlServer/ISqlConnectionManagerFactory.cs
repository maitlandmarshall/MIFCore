using ETLBox.Connection;

namespace MIFCore.Hangfire.APIETL.SqlServer
{
    public interface ISqlConnectionManagerFactory
    {
        SqlConnectionManager Create();
    }
}