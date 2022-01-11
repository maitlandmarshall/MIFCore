using Hangfire.Common;
using Hangfire.States;
using System;

namespace MAD.Integration.Common.Jobs
{
    public class RescheduleJobByDateOnExceptionAttribute : JobFilterAttribute, IElectStateFilter
    {
        public RescheduleJobByDateOnExceptionAttribute()
        {
        }

        public void OnStateElection(ElectStateContext context)
        {
            var failedState = context.CandidateState as FailedState;

            if (failedState is null)
                return;

            if (failedState.Exception is RescheduleJobException exception)
            {
                var rescheduleJobDate = exception.RescheduleDate;
                var ticks = (rescheduleJobDate - DateTime.Now).Ticks;
                var rescheduleSpan = TimeSpan.FromTicks(ticks);

                context.SetJobParameter("RetryCount", 0);

                context.CandidateState = new ScheduledState(rescheduleSpan) { Reason = $"Job has been rescheduled for {rescheduleJobDate.ToLocalTime():dd/MM/yyyy HH:mm:ss}" };
            }
        }
    }
}
