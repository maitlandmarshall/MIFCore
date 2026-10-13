using Hangfire;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MIFCore.Hangfire;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MIFCore.Tests
{
    [TestClass]
    [DoNotParallelize]
    public class TrackLastSuccessAttributeTests
    {
        private static TaskCompletionSource<bool> taskCompletionSource;
        private static readonly IDictionary<int, DateTime> paramSuccessMap = new Dictionary<int, DateTime>();

        [TestInitialize]
        public void Initialize()
        {
            taskCompletionSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            paramSuccessMap.Clear();
        }

        [TrackLastSuccess("Test")]
        public static async Task LastSuccess_Job()
        {
            try
            {
                await Task.Delay(5);

                var lastSuccess = BackgroundJobContext.Current.BackgroundJob.GetLastSuccess();

                if (lastSuccess.HasValue) taskCompletionSource.TrySetResult(true);
            }
            catch (Exception)
            {
                taskCompletionSource.TrySetResult(false);
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
                    paramSuccessMap[paramId] = lastSuccess.Value;

                    if (paramSuccessMap.Count > 1)
                    {
                        taskCompletionSource.TrySetResult(true);
                    }
                }
            }
            catch (Exception)
            {
                taskCompletionSource.TrySetResult(false);
            }

        }

        [TestMethod]
        public async Task CanAccessFromJobParameters_Ok()
        {
            using var server = new HangfireTestServer();
            string job = BackgroundJob.Enqueue(() => LastSuccess_Job());
            BackgroundJob.ContinueJobWith(job, () => LastSuccess_Job());

            var result = await taskCompletionSource.Task.WaitAsync(TimeSpan.FromSeconds(15));
            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task ParamterizedMethod_WithDynamicJobId_HasLastDistinctLastSuccessForEachDistinctParamCombo()
        {
            using var server = new HangfireTestServer();
            string job = BackgroundJob.Enqueue(() => LastSuccess_Job(5));
            job = BackgroundJob.ContinueJobWith(job, () => LastSuccess_Job(4));
            job = BackgroundJob.ContinueJobWith(job, () => LastSuccess_Job(5));
            job = BackgroundJob.ContinueJobWith(job, () => LastSuccess_Job(4));
            job = BackgroundJob.ContinueJobWith(job, () => LastSuccess_Job(4));
            job = BackgroundJob.ContinueJobWith(job, () => LastSuccess_Job(5));
            job = BackgroundJob.ContinueJobWith(job, () => LastSuccess_Job(4));

            var result = await taskCompletionSource.Task.WaitAsync(TimeSpan.FromSeconds(15));

            Assert.IsTrue(result);
            Assert.IsTrue(paramSuccessMap.Values.Distinct().Count() == 2);
        }
    }
}
