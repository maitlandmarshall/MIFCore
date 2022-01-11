using Hangfire;
using Hangfire.States;
using Microsoft.EntityFrameworkCore;
using MIFCore.Hangfire.JobActions.Database;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;

namespace MIFCore.Hangfire.JobActions
{
    internal class JobActionFilter : IElectStateFilter
    {
        private readonly IDbContextFactory<JobActionDbContext> dbContextFactory;

        public JobActionFilter(IDbContextFactory<JobActionDbContext> dbContextFactory)
        {
            this.dbContextFactory = dbContextFactory;
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
            catch(Exception ex)
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
                        var command = connection.CreateCommand();
                        command.Transaction = transaction;
                        command.CommandText = ja.Action;
                        command.ExecuteNonQuery();
                    }
                    catch(Exception ex)
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
    }
}
