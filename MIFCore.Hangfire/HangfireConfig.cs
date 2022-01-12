using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MIFCore.Hangfire
{
    public class HangfireConfig
    {
        public const string SchemaName = "job";

        public string ConnectionString { get; set; }
        public string[] Queues { get; set; }
    }
}
