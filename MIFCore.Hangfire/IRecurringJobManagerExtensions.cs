using Hangfire;
using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace MIFCore.Hangfire
{
    public static class IRecurringJobManagerExtensions
    {
        public static void CreateRecurringJob(this IRecurringJobManager recurringJobManager, string jobName, Expression<Func<Task>> methodCall, string cronSchedule = null, string queue = "default", bool triggerIfNeverExecuted = false)
        {
            var manager = recurringJobManager as MIFCoreRecurringJobManager;

            if (manager is null)
                throw new ArgumentException("Parameter 'recurringJobManager' is not of type 'MIFCore.Hangfire.MIFCoreRecurringJobManager'");
            
            manager.CreateRecurringJob(jobName: jobName, methodCall: methodCall, cronSchedule: cronSchedule, queue: queue, triggerIfNeverExecuted: triggerIfNeverExecuted);
        }

        public static void CreateRecurringJob<T>(this IRecurringJobManager recurringJobManager, string jobName, Expression<Func<T, Task>> methodCall, string cronSchedule = null, string queue = "default", bool triggerIfNeverExecuted = false)
        {
            var manager = recurringJobManager as MIFCoreRecurringJobManager;

            if (manager is null)
                throw new ArgumentException("Parameter 'recurringJobManager' is not of type 'MIFCore.Hangfire.MIFCoreRecurringJobManager'");

            manager.CreateRecurringJob<T>(jobName: jobName, methodCall: methodCall, cronSchedule: cronSchedule, queue: queue, triggerIfNeverExecuted: triggerIfNeverExecuted);
        }
    }
}
