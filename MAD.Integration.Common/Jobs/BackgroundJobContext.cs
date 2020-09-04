using Autofac;
using Autofac.Core.Lifetime;
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

        internal static ILifetimeScope CurrentLifetimeScope
        {
            get => ThreadStaticValue<ILifetimeScope>.Current;
            set => ThreadStaticValue<ILifetimeScope>.Current = value;
        }

        internal static Action<ILifetimeScope> CurrentLifetimeScopeChanged
        {
            get => ThreadStaticValue<ILifetimeScope>.OnCurrentChanged;
            set => ThreadStaticValue<ILifetimeScope>.OnCurrentChanged = value;
        }

        internal static LifetimeScope ParentBackgroundJobScope
        {
            get => ThreadStaticValue<LifetimeScope>.Current;
            set => ThreadStaticValue<LifetimeScope>.Current = value;
        }

        void IServerFilter.OnPerformed(PerformedContext filterContext) { }
        void IServerFilter.OnPerforming(PerformingContext filterContext)
        {
            CurrentJob = filterContext.BackgroundJob;
        }
    }
}
