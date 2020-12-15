using Hangfire;
using Hangfire.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace MAD.Integration.Common.Jobs
{
    public static class JobFactory
    {
        public static void CreateRecurringJob<T>(string jobName, Expression<Func<T, Task>> methodCall, string cronSchedule = null)
        {
            cronSchedule ??= Cron.Daily(22, 30);

            var connection = JobStorage.Current.GetConnection();

            RecurringJob.AddOrUpdate<T>(
                recurringJobId: jobName,
                methodCall: methodCall,
                cronExpression: cronSchedule,
                timeZone: TimeZoneInfo.Local);

            RecurringJobDto newJob = connection.GetRecurringJobs(new string[] { jobName }).First();

            if (newJob.LastExecution is null)
                RecurringJob.Trigger(jobName);
        }

        public static void CreateRecurringJob(string jobName, Expression<Func<Task>> methodCall, string cronSchedule = null)
        {
            cronSchedule ??= Cron.Daily(22, 30);

            var connection = JobStorage.Current.GetConnection();

            RecurringJob.AddOrUpdate(
                recurringJobId: jobName,
                methodCall: methodCall,
                cronExpression: cronSchedule,
                timeZone: TimeZoneInfo.Local);

            RecurringJobDto newJob = connection.GetRecurringJobs(new string[] { jobName }).First();

            if (newJob.LastExecution is null)
                RecurringJob.Trigger(jobName);
        }
    }
}
