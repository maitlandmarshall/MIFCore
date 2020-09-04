using Autofac;
using Autofac.Util;
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
    [AttributeUsage(AttributeTargets.Method)]
    public class ConsumerAttribute : Attribute { }
    public class EnableBackgroundJobResultConsumersFilter : JobFilterAttribute, IServerFilter
    {
        private Lazy<IEnumerable<MethodInfo>> consumers;
        private IEnumerable<MethodInfo> Consumers
        {
            get => this.consumers.Value;
        }

        public EnableBackgroundJobResultConsumersFilter()
        {
            this.consumers = new Lazy<IEnumerable<MethodInfo>>(() =>
            {
                return Assembly.GetEntryAssembly().GetLoadableTypes()
                    .SelectMany(t => t.GetMethods().Where(y => y.GetCustomAttribute<ConsumerAttribute>() != null))
                    .AsEnumerable();
            });
        }

        public void OnPerformed(PerformedContext filterContext)
        {
            var result = filterContext.Result;

            if (result != null)
            {
                var scope = BackgroundJobContext.ParentBackgroundJobScope;
                var resultType = result.GetType();
                var resultConsumers = this.Consumers.Where(y => y.GetParameters().Any(z => z.ParameterType.IsAssignableFrom(resultType)));

                foreach (var consumer in resultConsumers)
                {
                    var consumerInstance = scope.Resolve(consumer.DeclaringType);
                    consumer.Invoke(consumerInstance, new[] { result });
                }
            }
        }

        public void OnPerforming(PerformingContext filterContext)
        {
            
        }
    }
}
