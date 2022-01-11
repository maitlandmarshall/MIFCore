using System;

namespace MIFCore.Hangfire
{
    public class RescheduleJobException : Exception
    {
        public DateTime RescheduleDate;

        public RescheduleJobException(DateTime rescheduleDate)
        {
            this.RescheduleDate = rescheduleDate;
        }
    }
}
