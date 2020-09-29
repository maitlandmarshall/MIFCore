using Hangfire;
using Hangfire.MemoryStorage;
using MAD.Integration.Common.Jobs;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MAD.Integration.Common.Tests
{
    [TestClass]
    public class AppInsightsEventFilterTests
    {
        private static readonly TaskCompletionSource<bool> taskCompletionSource = new TaskCompletionSource<bool>();

        [AutomaticRetry(Attempts = 0)]
        public static async Task AppInsightsFailTest()
        {
            throw new Exception();
        }

        [TestMethod]
        public async Task Fail_AllEventsTracked()
        {
            var host = IntegrationHost.CreateDefaultBuilder()
                .UseHangfire(cfg => cfg.UseMemoryStorage())
                .UseAppInsights(cfg => cfg.InstrumentationKey = "test")
                .Build();

            BackgroundJob.Enqueue(() => AppInsightsFailTest());

            _ = host.RunAsync();

            var result = await taskCompletionSource.Task;
            Assert.IsTrue(result);
        }
    }
}
