using Hangfire.Client;
using Hangfire.Common;
using Hangfire.States;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MIFCore.Hangfire
{
    public class DelegatedQueueAttribute : JobFilterAttribute, IClientFilter, IElectStateFilter
    {
        public const string JobParameterKey = "DelegatedQueue";

        public DelegatedQueueAttribute(string queue)
        {
            Queue = queue;
        }

        public string Queue { get; }

        public void OnCreating(CreatingContext filterContext)
        {
            filterContext.SetJobParameter(JobParameterKey, this.Queue);
        }

        public void OnStateElection(ElectStateContext context)
        {
            if (context.CandidateState is EnqueuedState enqueuedState)
            {
                var delegatedQueue = context.GetJobParameter<string>(JobParameterKey);

                if (string.IsNullOrWhiteSpace(delegatedQueue))
                    return;

                enqueuedState.Queue = String.Format(delegatedQueue, context.BackgroundJob.Job.Args.ToArray());
            }
        }

        public void OnCreated(CreatedContext filterContext) { }
    }
}
