using Hangfire;
using MAD.Integration.Common.Jobs;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MAD.Integration.TestApp
{    
    public class SomeJob
    {
        private readonly IBackgroundJobClient backgroundJobClient;

        public SomeJob(IBackgroundJobClient backgroundJobClient)
        {
            this.backgroundJobClient = backgroundJobClient;
        }

        public void DoTheJob()
        {
            var current = BackgroundJobContext.Current;

            Console.WriteLine("yees");
            this.backgroundJobClient.Enqueue<SomeJob>(y => y.DoTheNextJob());
        }

        public async Task DoTheNextJob()
        {
            var current = BackgroundJobContext.Current;

            await Task.Delay(100);
            Console.WriteLine("yees2");
        }

        [RescheduleJobByDateOnException]
        public async Task DoTheJobButError()
        {           
            await Task.Delay(100);

            throw new RescheduleJobException(DateTime.Now.AddMinutes(2));
        }        
    }
}
