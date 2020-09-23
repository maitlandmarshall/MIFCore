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
    public class LastSuccessAttributeTests
    {
        private static readonly TaskCompletionSource<bool> taskCompletionSource = new TaskCompletionSource<bool>();

        [TrackLastSuccessAttribute("Test")]
        public static async Task LastSuccess_Job()
        {
            try
            {
                await Task.Delay(5);

                var lastSuccess = BackgroundJobContext.Current.BackgroundJob.GetLastSuccess();

                if (lastSuccess.HasValue) taskCompletionSource.SetResult(true);
            }
            catch (Exception)
            {
                taskCompletionSource.SetResult(false);
            }
        }

        [TestMethod]
        public async Task LastSuccess_CanAccessFromJobParameters_Ok()
        {
            var host = IntegrationHost.CreateDefaultBuilder()
                .UseHangfire(cfg => cfg.UseMemoryStorage())
                .Build();

            string job = BackgroundJob.Enqueue(() => LastSuccess_Job());
            BackgroundJob.ContinueJobWith(job, () => LastSuccess_Job());

            _ = host.RunAsync();

            var result = await taskCompletionSource.Task;
            Assert.IsTrue(result);
        }
    }
}
