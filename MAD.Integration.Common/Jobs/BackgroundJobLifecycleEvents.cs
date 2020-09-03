using Autofac;
using Autofac.Util;
using Hangfire;
using Hangfire.Common;
using Hangfire.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace MAD.Integration.Common.Jobs
{
    internal static class ThreadStaticScope<T>
    {
        [ThreadStatic]
        private static T current;

        public static T Current
        {
            get => current;
            set
            {
                current = value;
                onCurrentChanged?.Invoke(value);
            }
        }

        [ThreadStatic]
        private static Action<T> onCurrentChanged;
        public static Action<T> OnCurrentChanged
        {
            get => onCurrentChanged;
            set => onCurrentChanged = value;
        }
    }

    internal class BackgroundJobLifecycleEvents : JobFilterAttribute, IServerFilter
    {
        public void OnPerforming(PerformingContext filterContext)
        {
            var backgroundJobType = filterContext.BackgroundJob.Job.Type;

            ThreadStaticScope<Type>.Current = backgroundJobType;
            ThreadStaticScope<ILifetimeScope>.OnCurrentChanged = OnScopeBeginning;
        }

        public void OnPerformed(PerformedContext filterContext)
        {
            ThreadStaticScope<Type>.Current = null;
            ThreadStaticScope<ILifetimeScope>.OnCurrentChanged = null;
        }

        private void OnScopeBeginning(ILifetimeScope obj)
        {
            var jobInstance = obj.Resolve(ThreadStaticScope<Type>.Current);
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
