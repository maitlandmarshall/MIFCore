using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MAD.Integration.Common.Jobs
{
    public class HangfireConfig
    {
        public string ConnectionString { get; set; }
        public string[] Queues { get; set; }
    }
}
