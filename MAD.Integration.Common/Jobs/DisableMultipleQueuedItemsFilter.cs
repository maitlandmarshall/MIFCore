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
    public class DisableMultipleQueuedItemsFilter : JobFilterAttribute, IClientFilter, IServerFilter, IApplyStateFilter
    {
        private static readonly TimeSpan LockTimeout = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan FingerprintTimeout = TimeSpan.FromHours(1);

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

                if (failedState || deletedState)
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

        public void OnStateUnapplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
        {

        }

        private static bool AddFingerprintIfNotExists(IStorageConnection connection, Job job)
        {
            using (connection.AcquireDistributedLock(GetFingerprintLockKey(job), LockTimeout))
            {
                string fingerprintKey = GetFingerprintKey(job);
                Dictionary<string, string> fingerprint = connection.GetAllEntriesFromHash(fingerprintKey);

                if (fingerprint != null)
                {
                    if (fingerprint.ContainsKey("Timestamp") &&
                        DateTimeOffset.TryParse(fingerprint["Timestamp"], null, DateTimeStyles.RoundtripKind, out DateTimeOffset timestamp) &&
                        DateTimeOffset.UtcNow <= timestamp.Add(FingerprintTimeout))
                    {
                        // Actual fingerprint found, returning.
                        return false;
                    }
                }

                // Fingerprint does not exist, it is invalid (no `Timestamp` key),
                // or it is not actual (timeout expired).
                connection.SetRangeInHash(fingerprintKey, new Dictionary<string, string>
                {
                    { "Timestamp", DateTimeOffset.UtcNow.ToString("o") }
                });

                return true;
            }
        }

        private static void RemoveFingerprint(IStorageConnection connection, Job job)
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
            return string.Format("{0}:lock", GetFingerprintKey(job));
        }

        private static string GetFingerprintKey(Job job)
        {
            return string.Format("fingerprint:{0}", GetFingerprint(job));
        }

        private static string GetFingerprint(Job job)
        {
            string parameters = string.Empty;

            if (job.Args != null)
            {
                parameters = string.Join(".", job.Args);
            }

            if (job.Type == null || job.Method == null)
            {
                return string.Empty;
            }

            string fingerprint = string.Format(
                "{0}.{1}.{2}",
                job.Type.Name,
                job.Method.Name, parameters);

            return fingerprint;
        }

        void IClientFilter.OnCreated(CreatedContext filterContext)
        {
        }

        void IServerFilter.OnPerforming(PerformingContext filterContext)
        {
        }


    }
}
