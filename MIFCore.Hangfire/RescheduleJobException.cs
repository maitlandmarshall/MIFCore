using System;

namespace MIFCore.Hangfire
{
    public class RescheduleJobException : Exception
    {
        public DateTime RescheduleDate { get; set; }

        public RescheduleJobException(DateTime rescheduleDate)
        {
            this.RescheduleDate = rescheduleDate;
        }
    }
}
