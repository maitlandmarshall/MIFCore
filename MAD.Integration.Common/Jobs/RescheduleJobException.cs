using System;

namespace MAD.Integration.Common.Jobs
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
