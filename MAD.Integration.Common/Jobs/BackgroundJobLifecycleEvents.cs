using Autofac;
using Autofac.Util;
using Hangfire;
using Hangfire.Common;
using Hangfire.Server;
using MAD.Integration.Common.Jobs.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace MAD.Integration.Common.Jobs
{
    internal class BackgroundJobLifecycleEvents : JobFilterAttribute, IServerFilter
    {
        public void OnPerforming(PerformingContext filterContext)
        {
            var backgroundJobType = filterContext.BackgroundJob.Job.Type;

            ThreadStaticValue<Type>.Current = backgroundJobType;
            ThreadStaticValue<ILifetimeScope>.OnCurrentChanged = OnScopeBeginning;
        }

        public void OnPerformed(PerformedContext filterContext)
        {
            ThreadStaticValue<Type>.Current = null;
            ThreadStaticValue<ILifetimeScope>.OnCurrentChanged = null;
        }

        private void OnScopeBeginning(ILifetimeScope obj)
        {
            var jobInstance = obj.Resolve(ThreadStaticValue<Type>.Current);
            var init = jobInstance as IJobInitialize;

            try
            {
                init?.Initialized();
            }
            finally
            {
                obj.CurrentScopeEnding += Obj_CurrentScopeEnding;

                void Obj_CurrentScopeEnding(object sender, Autofac.Core.Lifetime.LifetimeScopeEndingEventArgs e)
                {
                    obj.CurrentScopeEnding -= Obj_CurrentScopeEnding;
                    init?.Deinitialized();
                }
            }
        }
    }
}
