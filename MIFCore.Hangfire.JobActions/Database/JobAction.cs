namespace MIFCore.Hangfire.JobActions.Database
{
    public class JobAction
    {
        public string JobName { get; set; }
        public JobActionTiming Timing { get; set; }

        public string Action { get; set; }
        public uint Order { get; set; }

        public bool IsEnabled { get; set; }

        public string Database { get; set; }
    }
}
