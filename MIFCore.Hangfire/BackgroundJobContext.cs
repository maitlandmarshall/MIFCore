using Autofac;
using Autofac.Core.Lifetime;
using Hangfire;
using Hangfire.Common;
using Hangfire.Server;
using MIFCore.Hangfire.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace MIFCore.Hangfire
{
    public class BackgroundJobContext : IServerFilter
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
