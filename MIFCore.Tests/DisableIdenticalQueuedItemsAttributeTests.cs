using Hangfire;
using Hangfire.MemoryStorage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MIFCore.Hangfire;
using System;
using System.Threading.Tasks;

namespace MIFCore.Tests
{
    [TestClass]
    public class DisableIdenticalQueuedItemsAttributeTests
    {
        public static async Task SomeJob()
        {
            await Task.Delay(TimeSpan.FromMinutes(2));
        }

        [TestMethod]
        public void IdenticalItemsNoParameters_IgnoresDuplicate()
        {
            var host = IntegrationHost.CreateDefaultBuilder()
               .UseHangfire(cfg => cfg.UseMemoryStorage().UseFilter(new DelegatedQueueAttribute("test")).UseFilter(new DisableIdenticalQueuedItemsAttribute { IncludeFailedJobs = true }))
               .Build();

            var backgroundJobClient = host.Services.GetService<IBackgroundJobClient>();

            JobFactory.CreateRecurringJob("someJob", () => SomeJob(), Cron.Minutely());
            string jobId2 = BackgroundJob.Enqueue(() => SomeJob());

            Assert.IsNull(jobId2);
        }
    }
}
