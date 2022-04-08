using Hangfire;
using MIFCore.Hangfire;
using Microsoft.Extensions.DependencyInjection;

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
            recurringJobManager.CreateRecurringJob<SomeJob>("some-job", y => y.DoTheJob(), Cron.Monthly());
            recurringJobManager.CreateRecurringJob<SomeJob>("some-job-triggered", y => y.TriggeredJobAction(), Cron.Monthly());

            backgroundJobClient.Enqueue<SomeJob>(y => y.DoTheJob());
            backgroundJobClient.Enqueue<SomeJob>(y => y.DoTheJobButError());
        }
    }
}