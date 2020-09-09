using Hangfire;
using Hangfire.MemoryStorage;
using MAD.Integration.Common.Jobs;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

namespace MAD.Integration.Common.Tests
{
    [TestClass]
    public class BackgroundJobContextTests
    {
        private static readonly TaskCompletionSource<bool> taskCompletionSource = new TaskCompletionSource<bool>();
        public static async Task BackgroundJobContext_Job()
        {
            try
            {
                await Task.Delay(5);
                var bg = BackgroundJobContext.Current;

                taskCompletionSource.SetResult(bg != null);
            }
            catch (Exception)
            {
                taskCompletionSource.SetResult(false);
            }
        }

        [TestMethod]
        public async Task CurrentJob_InAsyncExecutingJob_NotNull()
        {
            var host = IntegrationHost.CreateDefaultBuilder()
                .UseHangfire(cfg => cfg.UseMemoryStorage())
                .Build();

            BackgroundJob.Enqueue(() => BackgroundJobContext_Job());

            _ = host.RunAsync();

            var result = await taskCompletionSource.Task;
            Assert.IsTrue(result);
        }
    }
}
