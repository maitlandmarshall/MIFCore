using Hangfire;
using Hangfire.Client;
using Hangfire.Common;
using Hangfire.Logging;
using Hangfire.Server;
using Hangfire.States;
using Hangfire.Storage;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace MIFCore.Hangfire
{
    public class DisableIdenticalQueuedItemsAttribute : JobFilterAttribute, IClientFilter, IServerFilter, IApplyStateFilter
    {
        private const string ReservationParameter = "FingerprintReservation";
        private const string FingerprintParameter = "FingerprintKey";
        private const string CreationLockItem = "FingerprintCreationLock";
        private const string ExecutionLockItem = "FingerprintExecutionLock";
        private static readonly TimeSpan LockTimeout = TimeSpan.FromSeconds(100);
        private readonly ILogger<DisableIdenticalQueuedItemsAttribute> logger;
        private uint fingerprintTimeoutMinutes = 1440;
        private uint reservationTimeoutMinutes = 5;

        public DisableIdenticalQueuedItemsAttribute() : this(null) { }

        public DisableIdenticalQueuedItemsAttribute(ILogger<DisableIdenticalQueuedItemsAttribute> logger)
        {
            this.logger = logger;
            Order = int.MaxValue;
        }

        public uint FingerprintTimeoutMinutes
        {
            get => fingerprintTimeoutMinutes;
            set => fingerprintTimeoutMinutes = value > 0 ? value : throw new ArgumentOutOfRangeException(nameof(value));
        }

        public uint ReservationTimeoutMinutes
        {
            get => reservationTimeoutMinutes;
            set => reservationTimeoutMinutes = value > 0 ? value : throw new ArgumentOutOfRangeException(nameof(value));
        }

        public bool IncludeFailedJobs { get; set; }
        public Func<Job, string> FingerprintFactory { get; set; } = job => job.GetFingerprint();
        public bool ContinueChildJobs { get; set; }

        public void OnCreating(CreatingContext context)
        {
            var job = context.Job;
            IDisposable creationLock = null;
            Dictionary<string, string> marker = null;
            try
            {
                creationLock = context.Connection.AcquireDistributedLock(GetLockKey(job), LockTimeout);
                marker = context.Connection.GetAllEntriesFromHash(GetKey(job));
                if (IsProtected(context.Storage, context.Connection, job, marker))
                {
                    marker = context.Connection.GetAllEntriesFromHash(GetKey(job));
                    var parent = BackgroundJobContext.Current;
                    var parentReservation = parent?.GetJobParameter<string>(ReservationParameter);
                    var parentFingerprint = parent?.GetJobParameter<string>(FingerprintParameter);
                    if (ContinueChildJobs && parentReservation == Get(marker, "Reservation") && parentFingerprint == FingerprintFactory(job)
                        && !HasIdenticalActiveJob(context.Connection, job))
                    {
                        context.SetJobParameter(ReservationParameter, parentReservation);
                        context.SetJobParameter(FingerprintParameter, parentFingerprint);
                        context.Items[CreationLockItem] = creationLock;
                        Log(job, "child job continues the refresh reservation", parent.BackgroundJob.Id, ReadExpiry(marker));
                        return;
                    }
                    context.Canceled = true;
                    Log(job, "duplicate suppressed", Get(marker, "JobId"), ReadExpiry(marker));
                    creationLock.Dispose();
                    return;
                }

                var reservation = Guid.NewGuid().ToString("N");
                context.SetJobParameter(ReservationParameter, reservation);
                context.SetJobParameter(FingerprintParameter, FingerprintFactory(job));
                using (var transaction = context.Connection.CreateWriteTransaction())
                {
                    ClearJobs(context.Connection, transaction, job);
                    transaction.SetRangeInHash(GetKey(job), Marker(job, "", reservation, ReservationTimeoutMinutes));
                    transaction.Commit();
                }

                // Keep the lock until creation finishes. The initial state binds the job ID
                // in the same transaction that queues it, including ambiguous commit failures.
                context.Items[CreationLockItem] = creationLock;
                Log(job, "reserved", "", DateTimeOffset.UtcNow.AddMinutes(ReservationTimeoutMinutes));
            }
            catch (Exception exception)
            {
                creationLock?.Dispose();
                LogFailure(job, "reservation failed; creation stopped", exception, Get(marker, "JobId"), ReadExpiry(marker));
                throw;
            }
        }

        public void OnCreated(CreatedContext context)
        {
            if (!context.Items.TryGetValue(CreationLockItem, out var creationLock)) return;

            Dictionary<string, string> marker = null;
            try
            {
                if (context.BackgroundJob != null) return;

                marker = context.Connection.GetAllEntriesFromHash(GetKey(context.Job));
                var reservation = context.Parameters.TryGetValue(ReservationParameter, out var value) ? value as string : null;
                if (Get(marker, "Reservation") == reservation && string.IsNullOrEmpty(Get(marker, "JobId")))
                {
                    RemoveMarker(context.Connection, context.Job);
                    Log(context.Job, "failed creation reservation removed", "", null);
                }
                else
                {
                    Log(context.Job, "creation failed after ownership was persisted; retained for state inspection", Get(marker, "JobId"), ReadExpiry(marker));
                }
            }
            catch (Exception exception)
            {
                // Creation already failed. Leave its finite reservation for the next request,
                // and keep the original failure visible to Hangfire's creation retry path.
                LogFailure(context.Job, "failed creation cleanup failed; reservation retained", exception, Get(marker, "JobId"), ReadExpiry(marker));
            }
            finally
            {
                ((IDisposable)creationLock).Dispose();
                context.Items.Remove(CreationLockItem);
            }
        }

        public void OnPerforming(PerformingContext context)
        {
            var job = context.BackgroundJob.Job;
            // Old queued jobs have no reservation parameter. Every execution takes this lock,
            // so recovery of an old marker cannot overlap two refreshes of the same job.
            IDisposable executionLock = null;
            try
            {
                executionLock = context.Connection.AcquireDistributedLock($"run:{FingerprintFactory(job)}", LockTimeout);
                using (context.Connection.AcquireDistributedLock(GetLockKey(job), LockTimeout))
                {
                    var marker = context.Connection.GetAllEntriesFromHash(GetKey(job));
                    var jobId = Get(marker, "JobId");
                    var reservation = context.GetJobParameter<string>(ReservationParameter);
                    var activeJob = FindActiveJob(context.Connection, job, marker);
                    var knownLegacyJob = reservation == null && (jobId == context.BackgroundJob.Id
                        || (context.Connection.GetAllItemsFromSet(GetJobsKey(job))?.Contains(context.BackgroundJob.Id) ?? false));
                    var ownsReservation = !string.IsNullOrEmpty(reservation) && reservation == Get(marker, "Reservation");
                    if (activeJob != null && activeJob != context.BackgroundJob.Id && !ownsReservation && !knownLegacyJob)
                    {
                        Log(job, "execution deferred while another job is active", jobId, ReadExpiry(marker));
                        throw new InvalidOperationException($"Fingerprint {GetKey(job)} is protected by job {jobId}.");
                    }

                    reservation = reservation ?? (knownLegacyJob ? Get(marker, "Reservation") : null) ?? Guid.NewGuid().ToString("N");
                    context.SetJobParameter(ReservationParameter, reservation);
                    context.SetJobParameter(FingerprintParameter, FingerprintFactory(job));
                    using (var transaction = context.Connection.CreateWriteTransaction())
                    {
                        transaction.SetRangeInHash(GetKey(job), Marker(job, context.BackgroundJob.Id, reservation, FingerprintTimeoutMinutes));
                        transaction.AddToSet(GetJobsKey(job), context.BackgroundJob.Id);
                        transaction.Commit();
                    }
                    Log(job, "execution protection renewed", context.BackgroundJob.Id, DateTimeOffset.UtcNow.AddMinutes(FingerprintTimeoutMinutes));
                }
                context.Items[ExecutionLockItem] = executionLock;
            }
            catch (Exception exception)
            {
                executionLock?.Dispose();
                LogFailure(job, "execution protection failed; refresh did not start", exception, context.BackgroundJob.Id);
                throw;
            }
        }

        public void OnPerformed(PerformedContext context)
        {
            if (context.Items.TryGetValue(ExecutionLockItem, out var executionLock))
            {
                ((IDisposable)executionLock).Dispose();
                context.Items.Remove(ExecutionLockItem);
            }
            // The final state has not been committed yet. Removing the marker here would
            // allow a duplicate before AutomaticRetry has scheduled the same job again.
        }

        public void OnStateApplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
        {
            var job = context.BackgroundJob.Job;
            try
            {
                if (context.OldStateName == null)
                {
                    var marker = context.Connection.GetAllEntriesFromHash(GetKey(job));
                    var reservation = context.GetJobParameter<string>(ReservationParameter);
                    if (!string.IsNullOrEmpty(reservation) && Get(marker, "Reservation") != reservation)
                    {
                        // A delayed creation must not queue work after losing its reservation.
                        // Throwing here prevents its initial-state transaction from committing.
                        throw new InvalidOperationException($"Fingerprint {GetKey(job)} no longer belongs to this creation.");
                    }
                    if (!string.IsNullOrEmpty(reservation) && Get(marker, "Reservation") == reservation)
                    {
                        transaction.SetRangeInHash(GetKey(job), Marker(job, Get(marker, "JobId") is string id && id.Length > 0 ? id : context.BackgroundJob.Id, reservation, FingerprintTimeoutMinutes));
                        transaction.AddToSet(GetJobsKey(job), context.BackgroundJob.Id);
                        Log(job, "ownership enlisted with initial state", context.BackgroundJob.Id, DateTimeOffset.UtcNow.AddMinutes(FingerprintTimeoutMinutes));
                    }
                }
                else
                {
                    // State callbacks run before the caller commits. Cleanup is done under
                    // the creation lock on the next request, using the committed job state.
                    // This also prevents an older job's callback from deleting a new marker.
                    Log(job, $"state {context.NewState.Name}; protection retained until committed state inspection", context.BackgroundJob.Id, null);
                }
            }
            catch (Exception exception)
            {
                LogFailure(job, "state protection failed", exception, context.BackgroundJob.Id);
                throw;
            }
        }

        public void OnStateUnapplied(ApplyStateContext context, IWriteOnlyTransaction transaction) { }

        private bool IsProtected(JobStorage storage, IStorageConnection connection, Job job, Dictionary<string, string> marker)
        {
            var jobId = Get(marker, "JobId");
            if (!string.IsNullOrEmpty(jobId))
            {
                var activeJob = FindActiveJob(connection, job, marker);
                if (activeJob != null)
                {
                    WriteMarker(connection, job, activeJob, Get(marker, "Reservation"), FingerprintTimeoutMinutes);
                    Log(job, "queued, processing or retrying job retained", activeJob, DateTimeOffset.UtcNow.AddMinutes(FingerprintTimeoutMinutes));
                    return true;
                }
                Log(job, "committed terminal or missing job recovered", jobId, ReadExpiry(marker));
            }

            var expiry = ReadExpiry(marker);
            if (string.IsNullOrEmpty(jobId) && expiry.HasValue && expiry.Value > DateTimeOffset.UtcNow)
            {
                Log(job, "creation reservation still valid", "", expiry);
                return true;
            }

            // Old polling/CSV jobs can outlive their root marker. Inspect active queues
            // even when that marker is absent, and adopt every matching part of the refresh.
            // Storage/read failures stop creation rather than risking duplicate extraction.
            var matchingJobs = FindLegacyJobs(storage, connection, job);
            if (matchingJobs.Count > 0)
            {
                var owner = matchingJobs.First();
                using var transaction = connection.CreateWriteTransaction();
                transaction.SetRangeInHash(GetKey(job), Marker(job, owner, Guid.NewGuid().ToString("N"), FingerprintTimeoutMinutes));
                foreach (var id in matchingJobs) transaction.AddToSet(GetJobsKey(job), id);
                transaction.Commit();
                Log(job, "legacy or expired reservation adopted by active job", owner, DateTimeOffset.UtcNow.AddMinutes(FingerprintTimeoutMinutes));
                return true;
            }

            Log(job, "legacy or abandoned reservation recovered", "", expiry);
            return false;
        }

        private HashSet<string> FindLegacyJobs(JobStorage storage, IStorageConnection connection, Job job)
        {
            var candidates = new HashSet<string>();
            var matchingJobs = new HashSet<string>();
            foreach (var key in new[] { "schedule", "processing", "awaiting" })
            {
                candidates.UnionWith(connection.GetAllItemsFromSet(key) ?? new HashSet<string>());
            }
            if (IncludeFailedJobs) candidates.UnionWith(connection.GetAllItemsFromSet("failed") ?? new HashSet<string>());

            var monitoring = storage.GetMonitoringApi();
            foreach (var queue in monitoring.Queues())
            {
                candidates.UnionWith(monitoring.EnqueuedJobs(queue.Name, 0, int.MaxValue).Select(pair => pair.Key));
                candidates.UnionWith(monitoring.FetchedJobs(queue.Name, 0, int.MaxValue).Select(pair => pair.Key));
            }
            candidates.UnionWith(connection.GetAllItemsFromSet("processing") ?? new HashSet<string>());

            foreach (var id in candidates)
            {
                var data = connection.GetJobData(id);
                if (!IsActive(data)) continue;
                data.EnsureLoaded();
                if (data.Job == null) throw new InvalidOperationException($"Cannot inspect active job {id} for legacy fingerprint recovery.");
                if (FingerprintFactory(data.Job) == FingerprintFactory(job)) matchingJobs.Add(id);
            }
            return matchingJobs;
        }

        private bool IsActive(JobData data)
        {
            if (data == null || string.IsNullOrEmpty(data.State)) return false;
            if (data.State == SucceededState.StateName || data.State == DeletedState.StateName) return false;
            if (data.State == FailedState.StateName) return IncludeFailedJobs;
            return true;
        }

        private string FindActiveJob(IStorageConnection connection, Job job, Dictionary<string, string> marker)
        {
            var ids = connection.GetAllItemsFromSet(GetJobsKey(job)) ?? new HashSet<string>();
            var jobId = Get(marker, "JobId");
            if (!string.IsNullOrEmpty(jobId)) ids.Add(jobId);
            return ids.FirstOrDefault(id => IsActive(connection.GetJobData(id)));
        }

        private bool HasIdenticalActiveJob(IStorageConnection connection, Job job)
        {
            foreach (var id in connection.GetAllItemsFromSet(GetJobsKey(job)) ?? new HashSet<string>())
            {
                var data = connection.GetJobData(id);
                if (!IsActive(data)) continue;
                data.EnsureLoaded();
                if (data.Job.GetFingerprint() == job.GetFingerprint()) return true;
            }
            return false;
        }

        private static Dictionary<string, string> Marker(Job job, string jobId, string reservation, uint lifetimeMinutes)
        {
            var now = DateTimeOffset.UtcNow;
            return new Dictionary<string, string>
            {
                ["Timestamp"] = now.ToString("O", CultureInfo.InvariantCulture),
                ["ExpiresAt"] = now.AddMinutes(lifetimeMinutes).ToString("O", CultureInfo.InvariantCulture),
                ["JobId"] = jobId,
                ["Reservation"] = reservation,
                ["MethodName"] = job.Method.Name,
                ["TypeName"] = job.Type.Name
            };
        }

        private void WriteMarker(IStorageConnection connection, Job job, string jobId, string reservation, uint lifetimeMinutes)
        {
            using var transaction = connection.CreateWriteTransaction();
            transaction.SetRangeInHash(GetKey(job), Marker(job, jobId, reservation, lifetimeMinutes));
            transaction.Commit();
        }

        private void RemoveMarker(IStorageConnection connection, Job job)
        {
            using var transaction = connection.CreateWriteTransaction();
            transaction.RemoveHash(GetKey(job));
            ClearJobs(connection, transaction, job);
            transaction.Commit();
        }

        private void ClearJobs(IStorageConnection connection, IWriteOnlyTransaction transaction, Job job)
        {
            foreach (var id in connection.GetAllItemsFromSet(GetJobsKey(job)) ?? new HashSet<string>())
            {
                transaction.RemoveFromSet(GetJobsKey(job), id);
            }
        }

        private static string Get(Dictionary<string, string> marker, string field) =>
            marker != null && marker.TryGetValue(field, out var value) ? value : null;

        private static DateTimeOffset? ReadExpiry(Dictionary<string, string> marker) =>
            DateTimeOffset.TryParse(Get(marker, "ExpiresAt"), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var expiry) ? expiry : (DateTimeOffset?)null;

        private string GetKey(Job job) => $"fpt:{FingerprintFactory(job)}";
        private string GetLockKey(Job job) => $"lck:{FingerprintFactory(job)}";
        // Existing Hangfire SQL schemas limit hash and set keys to 100 characters.
        private string GetJobsKey(Job job) => $"fpj:{FingerprintFactory(job)}";

        private void Log(Job job, string decision, string jobId, DateTimeOffset? expiry)
        {
            if (logger != null) logger.LogInformation("Fingerprint {Fingerprint}, job {JobId}, expiry {Expiry}: {Decision}", GetKey(job), jobId, expiry, decision);
            else LogProvider.GetLogger(GetType()).Info($"Fingerprint {GetKey(job)}, job {jobId}, expiry {expiry:O}: {decision}");
        }

        private void LogFailure(Job job, string decision, Exception exception, string jobId = null, DateTimeOffset? expiry = null)
        {
            if (logger != null) logger.LogError(exception, "Fingerprint {Fingerprint}, job {JobId}, expiry {Expiry}: {Decision}", GetKey(job), jobId, expiry, decision);
            else LogProvider.GetLogger(GetType()).ErrorException($"Fingerprint {GetKey(job)}, job {jobId}, expiry {expiry:O}: {decision}", exception);
        }
    }
}
