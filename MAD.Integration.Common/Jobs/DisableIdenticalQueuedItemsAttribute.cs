using Hangfire.Client;
using Hangfire.Common;
using Hangfire.Server;
using Hangfire.States;
using Hangfire.Storage;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace MAD.Integration.Common.Jobs
{
    public class DisableIdenticalQueuedItemsAttribute : JobFilterAttribute, IClientFilter, IServerFilter, IApplyStateFilter
    {
        private static readonly TimeSpan LockTimeout = TimeSpan.FromSeconds(100);

        public uint FingerprintTimeoutMinutes { get; set; } = 0;
        public bool IncludeFailedJobs { get; set; } = true;

        public void OnCreating(CreatingContext filterContext)
        {
            if (!AddFingerprintIfNotExists(filterContext.Connection, filterContext.Job))
            {
                filterContext.Canceled = true;
            }
        }

        public void OnPerformed(PerformedContext filterContext)
        {
            if (filterContext.Exception is null || filterContext.ExceptionHandled)
            {
                RemoveFingerprint(filterContext.Connection, filterContext.BackgroundJob.Job);
            }
        }

        public void OnStateApplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
        {
            try
            {
                bool failedState = context.NewState is FailedState;
                bool deletedState = context.NewState is DeletedState;

                if (deletedState || (this.IncludeFailedJobs == false && failedState))
                {
                    RemoveFingerprint(context.Connection, context.BackgroundJob.Job);
                }
            }
            catch (Exception)
            {
                // Unhandled exceptions can cause an endless loop.
                // Therefore, catch and ignore them all.
            }
        }

        private bool AddFingerprintIfNotExists(IStorageConnection connection, Job job)
        {
            using var lck = connection.AcquireDistributedLock(GetFingerprintLockKey(job), LockTimeout);
            
            string fingerprintKey = GetFingerprintKey(job);
            Dictionary<string, string> fingerprint = connection.GetAllEntriesFromHash(fingerprintKey);

            if (fingerprint != null)
            {
                if (fingerprint.ContainsKey("Timestamp")
                    && DateTimeOffset.TryParse(fingerprint["Timestamp"], null, DateTimeStyles.RoundtripKind, out DateTimeOffset timestamp))
                {
                    if (this.FingerprintTimeoutMinutes > 0)
                    {
                        var timestampWithTimeout = timestamp.Add(TimeSpan.FromMinutes(FingerprintTimeoutMinutes));

                        if (DateTimeOffset.UtcNow <= timestampWithTimeout)
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
            }

            // Fingerprint does not exist, it is invalid (no `Timestamp` key),
            // or it is not actual (timeout expired).
            connection.SetRangeInHash(fingerprintKey, new Dictionary<string, string>
            {
                { "Timestamp", DateTimeOffset.UtcNow.ToString("o") },
                { "MethodName", job.Method.Name },
                { "TypeName", job.Type.Name }
            });

            return true;
        }

        private void RemoveFingerprint(IStorageConnection connection, Job job)
        {
            using (connection.AcquireDistributedLock(GetFingerprintLockKey(job), LockTimeout))
            using (IWriteOnlyTransaction transaction = connection.CreateWriteTransaction())
            {
                string fingerprintKey = GetFingerprintKey(job);

                transaction.RemoveHash(fingerprintKey);
                transaction.Commit();
            }
        }

        private static string GetFingerprintLockKey(Job job)
        {
            return string.Format("lck:{0}", job.GetFingerprint());
        }

        private static string GetFingerprintKey(Job job)
        {
            return string.Format("fpt:{0}", job.GetFingerprint());
        }

        void IApplyStateFilter.OnStateUnapplied(ApplyStateContext context, IWriteOnlyTransaction transaction) { }
        void IClientFilter.OnCreated(CreatedContext filterContext) { }
        void IServerFilter.OnPerforming(PerformingContext filterContext) { }
    }
}
