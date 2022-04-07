using Hangfire;
using Hangfire.Annotations;
using Hangfire.Common;
using Hangfire.Storage;
using Microsoft.Extensions.Configuration;
using MIFCore.Common;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace MIFCore.Hangfire
{
    public class MIFCoreRecurringJobManager : IRecurringJobManager
    {
        private readonly IRecurringJobManager recurringJobManager;
        private readonly JobStorage jobStorage;

        public MIFCoreRecurringJobManager(JobStorage jobStorage)
        {
            this.recurringJobManager = new RecurringJobManager(jobStorage);
            this.jobStorage = jobStorage;
        }

        public void CreateRecurringJob<T>(string jobName, Expression<Func<T, Task>> methodCall, string cronSchedule = null, string queue = "default", bool triggerIfNeverExecuted = false)
        {
            cronSchedule = this.GetCronSchedule(jobName, cronSchedule);

            var job = Job.FromExpression(methodCall);            

            this.recurringJobManager.AddOrUpdate<T>(
                recurringJobId: jobName,
                methodCall: methodCall,
                cronExpression: cronSchedule,
                timeZone: TimeZoneInfo.Local,
                queue: queue);            

            if (triggerIfNeverExecuted)
                this.TriggerRecurringJobIfNeverExecuted(jobName);
        }

        public void CreateRecurringJob(string jobName, Expression<Func<Task>> methodCall, string cronSchedule = null, string queue = "default", bool triggerIfNeverExecuted = false)
        {
            cronSchedule = this.GetCronSchedule(jobName, cronSchedule);

            this.recurringJobManager.AddOrUpdate(
               recurringJobId: jobName,
               methodCall: methodCall,
               cronExpression: cronSchedule,
               timeZone: TimeZoneInfo.Local,
               queue: queue);

            if (triggerIfNeverExecuted)
                this.TriggerRecurringJobIfNeverExecuted(jobName);
        }

        public string GetCronSchedule(string jobName, string cronSchedule = null)
        {
            // override if jobName is available in the settings file.
            var cronOverride = this.GetCronFromConfig(jobName);

            if (string.IsNullOrWhiteSpace(cronOverride))
            {
                return cronSchedule ?? Cron.Daily();
            }
            else
            {
                return cronOverride;
            }
        }

        private string GetCronFromConfig(string jobName)
        {
            var section = Globals.DefaultConfiguration.GetSection(jobName);
            return section.Exists() ? section.Value : null;
        }

        private void TriggerRecurringJobIfNeverExecuted(string jobName)
        {            
            var connection = this.jobStorage.GetConnection();
            var newJob = connection.GetRecurringJobs(new string[] { jobName }).First();

            if (newJob.LastExecution is null)
                RecurringJob.Trigger(jobName);
        }

        public void RemoveIfExists([NotNull] string recurringJobId)
        {
            RecurringJob.RemoveIfExists(recurringJobId);
        }

        public void Trigger([NotNull] string recurringJobId)
        {
            RecurringJob.Trigger(recurringJobId);
        }

        public void AddOrUpdate([NotNull] string recurringJobId, [NotNull] Job job, [NotNull] string cronExpression, [NotNull] RecurringJobOptions options)
        {
            cronExpression = GetCronSchedule(recurringJobId, cronExpression);

            this.recurringJobManager.AddOrUpdate(recurringJobId: recurringJobId, job: job, cronExpression: cronExpression, options: options);            
        }
    }
}
