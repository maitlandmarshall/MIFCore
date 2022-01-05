using Hangfire;
using Hangfire.Client;
using Hangfire.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MAD.Integration.Common.Jobs
{
    public static class BatchContextExtensions
    {
        public static PerformContext SetBatchParameter(this PerformContext ctx, string name, object value)
        {
            SetBatchParameterImpl(name, value, ctx);
            return ctx;
        }

        internal static CreateContext SetBatchParameter(this CreatingContext ctx, string name, object value)
        {
            SetBatchParameterImpl(name, value, ctx);
            return ctx;
        }

        private static void SetBatchParameterImpl(string name, object value, object ctx)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));

            name = $"ctx:{name}";

            if (ctx is PerformContext performContext)
            {
                performContext.Items[name] = value;
                performContext.SetJobParameter(name, value);
            }
            else if (ctx is CreatingContext creatingContext)
            {
                creatingContext.Items[name] = value;
                creatingContext.SetJobParameter(name, value);
            }
            else throw new NotSupportedException();
        }

        public static T GetBatchParameter<T>(this PerformContext job, string name)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));

            name = $"ctx:{name}";

            try
            {
                var connection = JobStorage.Current.GetConnection();
                var param = connection.GetJobParameter(job.BackgroundJob.Id, name);

                return job.GetJobParameter<T>(name);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Could not get a value of the job parameter `{name}`. See inner exception for details.", ex);
            }
        }
    }
}
