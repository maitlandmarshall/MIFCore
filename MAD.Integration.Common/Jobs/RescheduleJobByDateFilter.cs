using Hangfire.Common;
using Hangfire.States;
using System;

namespace MAD.Integration.Common.Jobs
{
    public class RescheduleJobByDateFilter : IElectStateFilter
    {
        public void OnStateElection(ElectStateContext context)
        {
            var failedState = context.CandidateState as FailedState;

            if (failedState is null)
                return;

            if (failedState.Exception is RescheduleJobException exception)
            {
                context.SetJobParameter("RetryCount", 0);
                context.CandidateState = new ScheduledState(exception.RescheduleDate) { Reason = $"Job has been rescheduled for {exception.RescheduleDate.ToLocalTime():dd/MM/yyyy HH:mm:ss}" };
            }
        }
    }
}
