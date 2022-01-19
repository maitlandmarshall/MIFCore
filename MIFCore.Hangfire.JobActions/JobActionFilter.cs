using Hangfire;
using Hangfire.SqlServer;
using Hangfire.States;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MIFCore.Hangfire.JobActions.Database;
using System;
using System.Data.Common;
using System.Linq;
using System.Transactions;

namespace MIFCore.Hangfire.JobActions
{
    internal class JobActionFilter : IElectStateFilter
    {
        private const string RecurringJobTriggerPrefix = "recurring-job:";

        private readonly IDbContextFactory<JobActionDbContext> dbContextFactory;
        private readonly IRecurringJobManagerFactory recurringJobManagerFactory;
        private readonly HangfireConfig hangfireConfig;
        private readonly JobStorageFactory jobStorageFactory;

        public JobActionFilter(
            IDbContextFactory<JobActionDbContext> dbContextFactory,
            IRecurringJobManagerFactory recurringJobManagerFactory,
            HangfireConfig hangfireConfig,
            JobStorageFactory jobStorageFactory)
        {
            this.dbContextFactory = dbContextFactory;
            this.recurringJobManagerFactory = recurringJobManagerFactory;
            this.hangfireConfig = hangfireConfig;
            this.jobStorageFactory = jobStorageFactory;
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

            // Start a transaction scope so all transactions, including cross-database transactions are included
            using var transactionScope = new TransactionScope();

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
                            this.TriggerSqlCommand(ja);
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new JobActionFailedException(ja, ex);
                    }
                }

                transactionScope.Complete();
            }
            catch (Exception)
            {
                throw;
            }
        }

        private void TriggerSqlCommand(JobAction jobAction)
        {
            using var connection = this.GetOpenConnection(jobAction.Database);

            var command = connection.CreateCommand();
            command.CommandText = jobAction.Action;
            command.ExecuteNonQuery();
        }

        private void TriggerRecurringJob(JobAction jobAction)
        {
            var recurringJobManager = this.GetRecurringJobManager(jobAction.Database);
            var recurringJobId = jobAction.Action.Substring(jobAction.Action.IndexOf(RecurringJobTriggerPrefix) + RecurringJobTriggerPrefix.Length);

            recurringJobManager.Trigger(recurringJobId);
        }

        private DbConnection GetOpenConnection(string databaseName = null)
        {
            var connection = new SqlConnection(this.GetConnectionString(databaseName));
            connection.Open();

            return connection;
        }

        private string GetConnectionString(string databaseName = null)
        {
            var builder = new SqlConnectionStringBuilder(this.hangfireConfig.ConnectionString);

            // If the databaseName is blank, use the default connectionString provided in the config
            if (string.IsNullOrWhiteSpace(databaseName) == false)
            {
                builder.InitialCatalog = databaseName;
            }

            return builder.ConnectionString;
        }

        IRecurringJobManager GetRecurringJobManager(string databaseName = null)
        {
            var connectionString = this.GetConnectionString(databaseName);
            var jobStorage = this.jobStorageFactory.Create(connectionString, "default");

            return this.recurringJobManagerFactory.GetManager(jobStorage);
        }
    }
}
