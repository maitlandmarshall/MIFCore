using Hangfire;
using Hangfire.MemoryStorage;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MIFCore.Hangfire;
using System;
using System.Threading.Tasks;

namespace MIFCore.Tests
{
    [TestClass]
    public class BatchContextAttributeTests
    {
        private static readonly TaskCompletionSource<bool> taskCompletionSource = new TaskCompletionSource<bool>();
        public static async Task RootJob()
        {
            try
            {
                BackgroundJobContext.Current.SetBatchParameter("Test", "this is a test");
                await Task.Delay(10);
                BackgroundJobContext.Current.SetBatchParameter("Test2", "a separate test");

                BackgroundJob.Enqueue(() => NestedJob());
            }
            catch (Exception ex)
            {
                taskCompletionSource.SetException(ex);
            }
        }

        public static void NestedJob()
        {
            try
            {
                var batchId = BackgroundJobContext.Current.GetBatchParameter<Guid>("Id");
                var started = BackgroundJobContext.Current.GetBatchParameter<DateTime>("Started");
                var test1 = BackgroundJobContext.Current.GetBatchParameter<string>("Test");
                var test2 = BackgroundJobContext.Current.GetBatchParameter<string>("Test2");

                Assert.IsNotNull(batchId);
                Assert.IsNotNull(started);
                Assert.IsTrue(!string.IsNullOrEmpty(test1));
                Assert.IsTrue(!string.IsNullOrEmpty(test2));

                BackgroundJob.Enqueue(() => NestedJob2());
            }
            catch (Exception ex)
            {
                taskCompletionSource.SetException(ex);
            }
        }

        public static void NestedJob2()
        {
            try
            {
                var batchId = BackgroundJobContext.Current.GetBatchParameter<Guid>("Id");
                var started = BackgroundJobContext.Current.GetBatchParameter<DateTime>("Started");
                var test1 = BackgroundJobContext.Current.GetBatchParameter<string>("Test");
                var test2 = BackgroundJobContext.Current.GetBatchParameter<string>("Test2");

                Assert.IsNotNull(batchId);
                Assert.IsNotNull(started);
                Assert.IsTrue(!string.IsNullOrEmpty(test1));
                Assert.IsTrue(!string.IsNullOrEmpty(test2));

                taskCompletionSource.SetResult(true);
            }
            catch (Exception ex)
            {
                taskCompletionSource.SetException(ex);
            }
        }

        [TestMethod]
        public async Task SetBatchParameter_NestedJob_ValuesIntact()
        {
            var host = IntegrationHost.CreateDefaultBuilder()
                .UseHangfire(cfg => cfg.UseMemoryStorage().UseFilter(new BatchContextFilter()))
                .Build();

            BackgroundJob.Enqueue(() => RootJob());

            await host.StartAsync();

            var result = await taskCompletionSource.Task;
            Assert.IsTrue(result);
        }
    }
}
