using Hangfire;
using Hangfire.MAMQSqlExtension;
using Hangfire.SqlServer;
using System;
using System.Collections.Generic;
using System.Text;

namespace MIFCore.Hangfire
{
    public class JobStorageFactory
    {
        public JobStorage Create(string connectionString, params string[] queues)
        {
            var options = new SqlServerStorageOptions
            {
                SchemaName = HangfireConfig.SchemaName,
                PrepareSchemaIfNecessary = true
            };

            var jobStorage = new MAMQSqlServerStorage(connectionString, options, queues);
            return jobStorage;
        }
    }
}
