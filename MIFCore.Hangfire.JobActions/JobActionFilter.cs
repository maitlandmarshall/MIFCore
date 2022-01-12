using Hangfire;
using Hangfire.States;
using Microsoft.EntityFrameworkCore;
using MIFCore.Hangfire.JobActions.Database;
using System;
using System.Data.Common;
using System.Linq;

namespace MIFCore.Hangfire.JobActions
{
    internal class JobActionFilter : IElectStateFilter
    {
        private const string RecurringJobTriggerPrefix = "recurring-job:";

        private readonly IDbContextFactory<JobActionDbContext> dbContextFactory;
        private readonly IRecurringJobManager recurringJobManager;

        public JobActionFilter(IDbContextFactory<JobActionDbContext> dbContextFactory, IRecurringJobManager recurringJobManager)
        {
            this.dbContextFactory = dbContextFactory;
            this.recurringJobManager = recurringJobManager;
        }

        public void OnStateElection(ElectStateContext context)
        {
            try
            {
                if (context.CandidateState is ProcessingState processingState)
                {
                    this.OnAction(context.BackgroundJob, JobActionTiming.BEFORE);
                }
                else if (context.CandidateState is SucceededState succeededState)
                {
                    this.OnAction(context.BackgroundJob, JobActionTiming.AFTER);
                }
            }
            catch (Exception ex)
            {
                context.CandidateState = new FailedState(ex);
            }
        }

        private void OnAction(BackgroundJob backgroundJob, JobActionTiming timing)
        {
            using var dbContext = this.dbContextFactory.CreateDbContext();

            var jobName = backgroundJob.GetJobName();
            var jobActions = dbContext.JobActions
                .Where(y => y.JobName == jobName)
                .Where(y => y.Timing == timing)
                .Where(y => y.IsEnabled)
                .OrderBy(y => y.Order).ThenBy(y => y.JobName)
                .ToList();

            if (jobActions.Any() == false)
                return;

            var connection = dbContext.Database.GetDbConnection();
            connection.Open();

            using var transaction = connection.BeginTransaction();

            try
            {
                foreach (var ja in jobActions)
                {
                    try
                    {
                        if (ja.Action.StartsWith(RecurringJobTriggerPrefix))
                        {
                            this.TriggerRecurringJob(ja);
                        }
                        else
                        {
                            this.TriggerSqlCommand(ja, connection, transaction);
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new JobActionFailedException(ja, ex);
                    }
                }

                transaction.Commit();
            }
            catch (Exception)
            {
                transaction.Rollback();
                throw;
            }
        }

        private void TriggerSqlCommand(JobAction jobAction, DbConnection connection, DbTransaction transaction)
        {
            var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = jobAction.Action;
            command.ExecuteNonQuery();
        }

        private void TriggerRecurringJob(JobAction jobAction)
        {
            var recurringJobId = jobAction.Action.Substring(jobAction.Action.IndexOf(RecurringJobTriggerPrefix) + RecurringJobTriggerPrefix.Length);
            this.recurringJobManager.Trigger(recurringJobId);
        }
    }
}
