using MIFCore.Hangfire.JobActions.Database;
using Newtonsoft.Json;
using System;
using System.Runtime.Serialization;

namespace MIFCore.Hangfire.JobActions
{
    [Serializable]
    public class JobActionFailedException : Exception
    {
        public JobActionFailedException(JobAction jobAction, Exception innerException) : base($"JobAction failed: {JsonConvert.SerializeObject(jobAction)}", innerException)
        {
            this.JobAction = jobAction;
        }

        public JobAction JobAction { get; }
    }
}