using Hangfire;
using MIFCore.Hangfire;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

namespace MIFCore.Tests
{
    [TestClass]
    [DoNotParallelize]
    public class BackgroundJobContextTests
    {
        private static readonly TaskCompletionSource<bool> taskCompletionSource = new TaskCompletionSource<bool>();
        public static async Task BackgroundJobContext_Job()
        {
            try
            {
                await Task.Delay(5);
                var bg = BackgroundJobContext.Current;

                taskCompletionSource.TrySetResult(bg != null);
            }
            catch (Exception)
            {
                taskCompletionSource.TrySetResult(false);
            }
        }

        [TestMethod]
        public async Task CurrentJob_InAsyncExecutingJob_NotNull()
        {
            using var server = new HangfireTestServer();
            BackgroundJob.Enqueue(() => BackgroundJobContext_Job());
            var result = await taskCompletionSource.Task.WaitAsync(TimeSpan.FromSeconds(15));
            Assert.IsTrue(result);
        }
    }
}
