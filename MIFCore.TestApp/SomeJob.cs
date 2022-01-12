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
        public void DoTheJob()
        {
            var current = BackgroundJobContext.Current;

            Console.WriteLine("yees");
            this.backgroundJobClient.Enqueue<SomeJob>(y => y.DoTheNextJob());
        }

        public void TriggeredJobAction()
        {
            Console.WriteLine("triggered job action");
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

            throw new RescheduleJobException(DateTime.Now.AddMinutes(2));
        }
    }
}
