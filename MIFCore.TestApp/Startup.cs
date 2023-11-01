using Hangfire;
using MIFCore.Hangfire;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace MIFCore.TestApp
{
    internal class Startup
    {
        public void ConfigureServices(IServiceCollection serviceDescriptors)
        {
            serviceDescriptors.AddScoped<SomeJob>();
            serviceDescriptors.AddControllers();

            
        }

        public void Configure()
        {

        }

        public void PostConfigure(IBackgroundJobClient backgroundJobClient, IRecurringJobManager recurringJobManager)
        {
            recurringJobManager.CreateRecurringJob<SomeJob>("some-job", y => y.DoTheJob(), cronSchedule: "*/5 * * * *");
            recurringJobManager.CreateRecurringJob<SomeJob>("some-goober-boy", y => y.DingleDog("goober"), cronSchedule: "*/5 * * * *");
            
            // This doesn't seem to run due to the queue?
            recurringJobManager.CreateRecurringJob<SomeJob>("some-bigboi", y => y.DingleDog("biggest-boi"), cronSchedule: "*/5 * * * *", queue: "bigboi");
            

            recurringJobManager.CreateRecurringJob<SomeJob>("some-job-triggered", y => y.TriggeredJobAction(), Cron.Monthly());

            backgroundJobClient.Enqueue<SomeJob>(y => y.DoTheJob());
            backgroundJobClient.Enqueue<SomeJob>(y => y.DoTheJobButError());
        }
    }
}