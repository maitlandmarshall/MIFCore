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
        public static PerformContext Current
        {
            get => AsyncLocalValue<PerformContext>.Current;
            set => AsyncLocalValue<PerformContext>.Current = value;
        }

        void IServerFilter.OnPerformed(PerformedContext filterContext)
        {
            Current = filterContext;
        }

        void IServerFilter.OnPerforming(PerformingContext filterContext)
        {
            Current = filterContext;
        }
    }
}
