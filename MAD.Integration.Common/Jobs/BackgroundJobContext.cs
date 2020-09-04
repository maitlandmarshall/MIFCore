using Autofac;
using Hangfire;
using Hangfire.Common;
using Hangfire.Server;
using MAD.Integration.Common.Jobs.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace MAD.Integration.Common.Jobs
{
    public class BackgroundJobContext : JobFilterAttribute, IServerFilter
    {
        public static BackgroundJob CurrentJob
        {
            get => ThreadStaticValue<BackgroundJob>.Current;
            set => ThreadStaticValue<BackgroundJob>.Current = value;
        }

        public static IServiceProvider CurrentJobServices
        {
            get => ThreadStaticValue<ILifetimeScope>.Current as IServiceProvider;
        }

        public void OnPerformed(PerformedContext filterContext) { }
        public void OnPerforming(PerformingContext filterContext)
        {
            CurrentJob = filterContext.BackgroundJob;
        }
    }
}
