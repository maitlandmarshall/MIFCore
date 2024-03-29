﻿using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace MIFCore.Hangfire
{
    [Obsolete("IRecurringJobFactory is obsolete. Please use Hangfire.IRecurringJobManager.")]
    public interface IRecurringJobFactory
    {
        void CreateRecurringJob(string jobName, Expression<Func<Task>> methodCall, string cronSchedule = null, string queue = "default", bool triggerIfNeverExecuted = false);
        void CreateRecurringJob<T>(string jobName, Expression<Func<T, Task>> methodCall, string cronSchedule = null, string queue = "default", bool triggerIfNeverExecuted = false);
    }
}