using Hangfire;
using MIFCore.Hangfire;
using System;
using System.Threading.Tasks;

namespace MIFCore.TestApp
{
    public class SomeJob
    {
        private readonly IBackgroundJobClient backgroundJobClient;

        public SomeJob(IBackgroundJobClient backgroundJobClient)
        {
            this.backgroundJobClient = backgroundJobClient;
        }

        [JobDisplayName("dogJob")]
        [DisableConcurrentExecution(1000)]
        public Task DoTheJob()
        {
            var current = BackgroundJobContext.Current;

            Console.WriteLine("yees");
            this.backgroundJobClient.Enqueue<SomeJob>(y => y.DoTheNextJob());

            return Task.CompletedTask;
        }

        public Task TriggeredJobAction()
        {
            Console.WriteLine("triggered job action");

            return Task.CompletedTask;
        }

        public async Task DoTheNextJob()
        {
            var current = BackgroundJobContext.Current;

            await Task.Delay(100);
            Console.WriteLine("yees2");
        }

        public async Task DoTheJobButError()
        {
            await Task.Delay(100);

            throw new RescheduleJobException(DateTimeOffset.UtcNow.AddMinutes(2).DateTime);
        }
    }
}
