using Hangfire;
using Hangfire.MemoryStorage;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MIFCore.Hangfire;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MIFCore.Tests
{
    [TestClass]
    public class TrackLastSuccessAttributeTests
    {
        private static readonly TaskCompletionSource<bool> taskCompletionSource = new TaskCompletionSource<bool>();
        private static readonly IDictionary<int, DateTime> paramSuccessMap = new Dictionary<int, DateTime>();

        [TrackLastSuccess("Test")]
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

        [TrackLastSuccess]
        public static async Task LastSuccess_Job(int paramId)
        {
            try
            {
                await Task.Delay(5);

                var lastSuccess = BackgroundJobContext.Current.BackgroundJob.GetLastSuccess();

                if (lastSuccess.HasValue)
                {
                    paramSuccessMap.Add(paramId, lastSuccess.Value);

                    if (paramSuccessMap.Count > 1)
                    {
                        taskCompletionSource.SetResult(true);
                    }
                }
            }
            catch (Exception)
            {
                taskCompletionSource.SetResult(false);
            }

        }

        [TestMethod]
        public async Task CanAccessFromJobParameters_Ok()
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

        [TestMethod]
        public async Task ParamterizedMethod_WithDynamicJobId_HasLastDistinctLastSuccessForEachDistinctParamCombo()
        {
            var host = IntegrationHost.CreateDefaultBuilder()
                   .UseHangfire(cfg => cfg.UseMemoryStorage())
                   .Build();

            await host.StartAsync();

            string job = BackgroundJob.Enqueue(() => LastSuccess_Job(5));
            job = BackgroundJob.ContinueJobWith(job, () => LastSuccess_Job(4));
            job = BackgroundJob.ContinueJobWith(job, () => LastSuccess_Job(5));
            job = BackgroundJob.ContinueJobWith(job, () => LastSuccess_Job(4));
            job = BackgroundJob.ContinueJobWith(job, () => LastSuccess_Job(4));
            job = BackgroundJob.ContinueJobWith(job, () => LastSuccess_Job(5));
            job = BackgroundJob.ContinueJobWith(job, () => LastSuccess_Job(4));

            var result = await taskCompletionSource.Task;

            Assert.IsTrue(result);
            Assert.IsTrue(paramSuccessMap.Values.Distinct().Count() == 2);
        }
    }
}
