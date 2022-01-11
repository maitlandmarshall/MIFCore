using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MIFCore.Hangfire
{
    public static class JobQueue
    {
        public const string Alpha = "alpha";
        public const string Default = "default";
        public const string Beta = "e-beta";
        public const string Low = "z";

        public static string[] Queues
        {
            get => new[] { JobQueue.Alpha, JobQueue.Beta, JobQueue.Default, JobQueue.Low };
        }
    }
}
