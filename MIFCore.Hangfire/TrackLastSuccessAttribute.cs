using Hangfire;
using Hangfire.Common;
using Hangfire.Server;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace MIFCore.Hangfire
{
    public class TrackLastSuccessAttribute : JobFilterAttribute, IServerFilter
    {
        public const string ParameterName = "LastSuccess";

        public TrackLastSuccessAttribute(string jobIdentifier)
        {
            this.JobIdentifier = jobIdentifier;
        }

        public TrackLastSuccessAttribute() {}

        public string JobIdentifier { get; }

        void IServerFilter.OnPerformed(PerformedContext filterContext)
        {
            if (filterContext.Exception is null == false) return;

            var jobId = this.GetJobIdentifier(filterContext.BackgroundJob.Job);
            var connection = JobStorage.Current.GetConnection();
            connection.SetRangeInHash(jobId, new Dictionary<string, string>
            {
                { ParameterName, DateTime.UtcNow.ToString("O") }
            });
        }

        void IServerFilter.OnPerforming(PerformingContext filterContext)
        {
            var jobId = this.GetJobIdentifier(filterContext.BackgroundJob.Job);
            var connection = JobStorage.Current.GetConnection();
            var hashEntries = connection.GetAllEntriesFromHash(jobId);

            if (hashEntries is null) 
                return;

            if (hashEntries.TryGetValue(ParameterName, out string lastSuccessUtcString)
                && DateTime.TryParseExact(lastSuccessUtcString, "O", CultureInfo.InvariantCulture, DateTimeStyles.None, out var lastSuccess))
            {
                filterContext.SetJobParameter(ParameterName, lastSuccess);
            }
        }

        private string GetJobIdentifier(Job job)
        {
            if (!string.IsNullOrWhiteSpace(this.JobIdentifier))
                return this.JobIdentifier;

            return job.GetFingerprint();
        }
    }
}
