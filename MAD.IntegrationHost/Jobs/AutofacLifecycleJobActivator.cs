using Autofac;
using Hangfire;
using Hangfire.Annotations;
using System;

namespace MAD.Integration.Common.Jobs
{
    public class AutofacLifecycleJobActivator : JobActivator
    {
        public const string LifetimeScopeTag = "BackgroundJobScope";

        private readonly ILifetimeScope lifetimeScope;

        public AutofacLifecycleJobActivator([NotNull] ILifetimeScope lifetimeScope)
        {
            this.lifetimeScope = lifetimeScope;
        }

        public override object ActivateJob(Type jobType)
        {
            var jobInstance = this.lifetimeScope.Resolve(jobType);

            if (jobInstance is IJobActivated jobInitialize)
            {
                jobInitialize.Activated();
            }

            return jobInstance;
        }

        public override JobActivatorScope BeginScope(JobActivatorContext context)
        {
            return new AutofacScope(this.lifetimeScope.BeginLifetimeScope(LifetimeScopeTag));
        }

        private class AutofacScope : JobActivatorScope
        {
            private readonly ILifetimeScope lifetimeScope;

            public AutofacScope(ILifetimeScope lifetimeScope)
            {
                this.lifetimeScope = lifetimeScope;
            }

            public override object Resolve(Type type)
            {
                var jobInstance = this.lifetimeScope.Resolve(type);

                if (jobInstance is IJobActivated jobInitialize)
                {
                    jobInitialize.Activated();
                }

                return jobInstance;
            }

            public override void DisposeScope()
            {
                this.lifetimeScope.Dispose();
            }
        }
    }
}
