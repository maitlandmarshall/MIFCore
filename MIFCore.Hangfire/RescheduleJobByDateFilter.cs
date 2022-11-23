using Hangfire.States;

namespace MIFCore.Hangfire
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
                context.CandidateState = new ScheduledState(exception.RescheduleDate.ToUniversalTime())
                {
                    Reason = $"Job has been rescheduled for {exception.RescheduleDate.ToLocalTime():dd/MM/yyyy HH:mm:ss}"
                };
            }
        }
    }
}
