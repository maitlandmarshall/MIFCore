using Hangfire;
using Hangfire.Common;
using Hangfire.Server;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace MAD.Integration.Common.Jobs
{
    public class BackgroundJobContext : JobFilterAttribute, IServerFilter
    {
        [ThreadStatic]
        private static BackgroundJob currentJob;

        public static BackgroundJob CurrentJob
        {
            get => currentJob;
            set
            {
                currentJob = value;
            }
        }

        public void OnPerformed(PerformedContext filterContext) { }
        public void OnPerforming(PerformingContext filterContext)
        {
            CurrentJob = filterContext.BackgroundJob;
        }
    }
}
