using Hangfire;
using Hangfire.Common;
using Hangfire.Server;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace MAD.Integration.Common.Jobs
{
    public class TrackLastSuccessAttribute : JobFilterAttribute, IServerFilter
    {
        public const string ParameterName = "LastSuccess";

        public TrackLastSuccessAttribute(string jobIdentifier)
        {
            this.JobIdentifier = jobIdentifier;
        }

        public string JobIdentifier { get; }

        void IServerFilter.OnPerformed(PerformedContext filterContext)
        {
            if (filterContext.Exception is null == false) return;

            var connection = JobStorage.Current.GetConnection();
            connection.SetRangeInHash(this.JobIdentifier, new Dictionary<string, string>
            {
                { ParameterName, DateTime.UtcNow.ToString("O") }
            });
        }

        void IServerFilter.OnPerforming(PerformingContext filterContext)
        {
            var connection = JobStorage.Current.GetConnection();
            var hashEntries = connection.GetAllEntriesFromHash(this.JobIdentifier);
            if (hashEntries is null) return;

            if (hashEntries.TryGetValue(ParameterName, out string lastSuccessUtcString)
                && DateTime.TryParseExact(lastSuccessUtcString, "O", CultureInfo.InvariantCulture, DateTimeStyles.None, out var lastSuccess))
            {
                filterContext.SetJobParameter(ParameterName, lastSuccess);
            }
        }
    }
}
