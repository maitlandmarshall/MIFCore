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

        public void CreateRecurringJob<T>(string jobName, Expression<Func<T, Task>> methodCall, string cronSchedule = null, string queue = "default", bool triggerIfNeverExecuted = false, TimeZoneInfo timeZone = null)
        {
            timeZone = this.GetTimeZoneInfo(jobName, timeZone);
            cronSchedule = this.GetCronSchedule(jobName, cronSchedule);

            this.recurringJobManager.AddOrUpdate<T>(
                recurringJobId: jobName,
                methodCall: methodCall,
                cronExpression: cronSchedule,
                timeZone: timeZone,
                queue: queue
            );

            if (triggerIfNeverExecuted)
                this.TriggerRecurringJobIfNeverExecuted(jobName);
        }

        public void CreateRecurringJob(string jobName, Expression<Func<Task>> methodCall, string cronSchedule = null, string queue = "default", bool triggerIfNeverExecuted = false, TimeZoneInfo timeZone = null)
        {
            timeZone = this.GetTimeZoneInfo(jobName, timeZone);
            cronSchedule = this.GetCronSchedule(jobName, cronSchedule);

            this.recurringJobManager.AddOrUpdate(
                recurringJobId: jobName,
                methodCall: methodCall,
                cronExpression: cronSchedule,
                timeZone: timeZone,
                queue: queue
            );

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

            // check if the config value has sub keys for "cron" instead of being set directly on the jobName key.
            var cronSubKey = section.GetSection("cron");
            if (cronSubKey.Exists())
            {
                return cronSubKey.Value;
            }

            return section.Exists() ? section.Value : null;
        }

        /// <summary>
        /// Retreives the TimeZoneInfo for the jobName from the config file or returns the passed value if not found.
        /// If no value is passed, the default TimeZoneInfo.Local is returned.
        /// </summary>
        /// <param name="jobName"></param>
        /// <param name="timeZoneInfo"></param>
        /// <returns></returns>
        public TimeZoneInfo GetTimeZoneInfo(string jobName, TimeZoneInfo timeZoneInfo = null)
        {
            // try get config value
            var configOverride = this.GetTimeZoneInfoFromConfig(jobName);

            // prefer config > code configured tz > default tz
            return configOverride ?? timeZoneInfo ?? TimeZoneInfo.Local;
        }

        /// <summary>
        /// Check for the existence of a timezone config value for the jobName.
        /// in .Net 5 the only supported timzone Ids can be found with "tzutil /l" on command line for windows;
        /// </summary>
        /// <param name="jobName"></param>
        /// <returns></returns>
        private TimeZoneInfo GetTimeZoneInfoFromConfig(string jobName)
        {
            var section = Globals.DefaultConfiguration.GetSection(jobName);
            var tzInfoSubKey = section.GetSection("timezone");

            // exit early if key doesn't exist or has an empty value.
            if (!tzInfoSubKey.Exists() || string.IsNullOrWhiteSpace(tzInfoSubKey.Value))
            {
                return null;
            }

            return TimeZoneInfo.FindSystemTimeZoneById(tzInfoSubKey.Value);
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
