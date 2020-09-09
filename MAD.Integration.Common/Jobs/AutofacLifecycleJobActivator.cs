using Autofac;
using Hangfire.Annotations;
using Hangfire.Autofac;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MAD.Integration.Common.Jobs
{
    public class AutofacLifecycleJobActivator : AutofacJobActivator
    {
        public AutofacLifecycleJobActivator([NotNull] ILifetimeScope lifetimeScope, bool useTaggedLifetimeScope = true) : base(lifetimeScope, useTaggedLifetimeScope)
        {
        }

        public override object ActivateJob(Type jobType)
        {
            var jobInstance = base.ActivateJob(jobType);

            if (jobInstance is IJobActivated jobInitialize)
            {
                jobInitialize.Activated();
            }

            return jobInstance;
        }
    }
}
