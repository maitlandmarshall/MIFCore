using Hangfire;
using Hangfire.Client;
using Hangfire.Common;
using Hangfire.InMemory;
using Hangfire.Server;
using Hangfire.States;
using Hangfire.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MIFCore.Hangfire;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace MIFCore.Tests
{
    [TestClass]
    public class DisableIdenticalQueuedItemsAttributeTests
    {
        private FaultingStorage storage;
        private DisableIdenticalQueuedItemsAttribute filter;
        private BackgroundJobClient client;
        private RecordingLogger logger;
        private Job job;
        private string key;

        public static void SomeJob() { }

        [TestInitialize]
        public void Initialize()
        {
            storage = new FaultingStorage();
            logger = new RecordingLogger();
            filter = new DisableIdenticalQueuedItemsAttribute(logger);
            var filters = new JobFilterCollection();
            filters.Add(new AutomaticRetryAttribute { Attempts = 2, DelaysInSeconds = new[] { 60 } });
            filters.Add(filter);
            client = new BackgroundJobClient(storage, filters);
            job = Job.FromExpression(() => SomeJob());
            key = $"fpt:{job.GetFingerprint()}";
        }

        [TestMethod]
        public void ConcurrentCreation_QueuesOnlyOneJobAndPersistsOwnership()
        {
            var ids = new ConcurrentBag<string>();
            Parallel.For(0, 24, _ => ids.Add(client.Create(job, new EnqueuedState())));
            var id = ids.Single(value => value != null);
            using var connection = storage.GetConnection();
            var marker = connection.GetAllEntriesFromHash(key);
            Assert.AreEqual(id, marker["JobId"]);
            Assert.IsTrue(DateTimeOffset.Parse(marker["ExpiresAt"]) > DateTimeOffset.UtcNow);
            Assert.AreEqual(1, storage.Inner.GetMonitoringApi().EnqueuedCount("default"));
        }

        [TestMethod]
        public void StaleLegacyMarkerWithoutExpiry_DoesNotBlockNextRefresh()
        {
            SetMarker(new Dictionary<string, string> { ["Timestamp"] = DateTimeOffset.UtcNow.AddDays(-30).ToString("O") });
            Assert.IsNotNull(client.Create(job, new EnqueuedState()));
            Assert.IsTrue(logger.Messages.Any(message => message.Contains("abandoned reservation recovered")));
        }

        [TestMethod]
        [DataRow("Enqueued")]
        [DataRow("Scheduled")]
        [DataRow("Processing")]
        public void LegacyActiveJob_IsAdoptedInsteadOfDuplicated(string state)
        {
            var unfiltered = new BackgroundJobClient(storage, new JobFilterCollection());
            IState initialState = state == "Scheduled" ? new ScheduledState(TimeSpan.FromHours(1)) : new EnqueuedState();
            var id = unfiltered.Create(job, initialState);
            if (state == "Processing") unfiltered.ChangeState(id, Processing("interrupted-worker"));
            SetMarker(new Dictionary<string, string> { ["Timestamp"] = "invalid old timestamp" });
            Assert.IsNull(client.Create(job, new EnqueuedState()));
            using var connection = storage.GetConnection();
            Assert.AreEqual(id, connection.GetAllEntriesFromHash(key)["JobId"]);
        }

        [TestMethod]
        [DataRow("Enqueued")]
        [DataRow("Scheduled")]
        [DataRow("Processing")]
        public void MissingMarker_StillAdoptsAnExistingActiveJob(string state)
        {
            var unfiltered = new BackgroundJobClient(storage, new JobFilterCollection());
            IState initial = state == "Scheduled" ? new ScheduledState(TimeSpan.FromDays(2)) : new EnqueuedState();
            var id = unfiltered.Create(job, initial);
            if (state == "Processing") unfiltered.ChangeState(id, Processing("old-worker"));
            Assert.IsNull(client.Create(job, new EnqueuedState()));
            using var connection = storage.GetConnection();
            Assert.AreEqual(id, connection.GetAllEntriesFromHash(key)["JobId"]);
        }

        [TestMethod]
        [DataRow("Enqueued")]
        [DataRow("Scheduled")]
        [DataRow("Processing")]
        public void ExpiredMarker_StillProtectsQueueWaitRetriesAndLongRunningJob(string state)
        {
            var id = client.Create(job, new EnqueuedState());
            if (state == "Scheduled") client.ChangeState(id, new ScheduledState(TimeSpan.FromDays(3)));
            if (state == "Processing") client.ChangeState(id, Processing("server"));
            SetMarker(new Dictionary<string, string> { ["ExpiresAt"] = DateTimeOffset.UtcNow.AddDays(-2).ToString("O") });
            Assert.IsNull(client.Create(job, new EnqueuedState()));
            using var connection = storage.GetConnection();
            Assert.AreEqual(id, connection.GetAllEntriesFromHash(key)["JobId"]);
            Assert.IsTrue(DateTimeOffset.Parse(connection.GetAllEntriesFromHash(key)["ExpiresAt"]) > DateTimeOffset.UtcNow);
        }

        [TestMethod]
        public void ExpiredAbandonedReservation_IsRecoveredAfterWorkerDisconnection()
        {
            SetMarker(new Dictionary<string, string>
            {
                ["Reservation"] = "interrupted-client",
                ["JobId"] = "",
                ["ExpiresAt"] = DateTimeOffset.UtcNow.AddMinutes(-1).ToString("O")
            });
            Assert.IsNotNull(client.Create(job, new EnqueuedState()));
        }

        [TestMethod]
        public void MissingOwningJob_IsRecovered()
        {
            SetMarker(new Dictionary<string, string> { ["JobId"] = "missing-job", ["ExpiresAt"] = DateTimeOffset.UtcNow.AddDays(1).ToString("O") });
            Assert.IsNotNull(client.Create(job, new EnqueuedState()));
        }

        [TestMethod]
        public void CreationPersistenceFailure_RemovesOnlyItsReservation()
        {
            storage.Before = method => { if (method.Name == "CreateExpiredJob") throw new InvalidOperationException("injected persistence failure"); };
            Assert.Throws<BackgroundJobClientException>(() => client.Create(job, new EnqueuedState()));
            storage.Before = null;
            using var connection = storage.GetConnection();
            Assert.IsNull(connection.GetAllEntriesFromHash(key));
            Assert.IsNotNull(client.Create(job, new EnqueuedState()));
            Assert.IsTrue(logger.Messages.Any(message => message.Contains("failed creation reservation removed")));
        }

        [TestMethod]
        public void CleanupFailure_IsLoggedAndFiniteReservationSurvives()
        {
            storage.Before = method =>
            {
                if (method.Name == "CreateExpiredJob" || method.Name == "RemoveHash") throw new InvalidOperationException("injected failure");
            };
            Assert.Throws<BackgroundJobClientException>(() => client.Create(job, new EnqueuedState()));
            storage.Before = null;
            using var connection = storage.GetConnection();
            Assert.IsNotNull(connection.GetAllEntriesFromHash(key)["ExpiresAt"]);
            Assert.IsTrue(logger.Messages.Any(message => message.Contains("cleanup failed")));
            Assert.IsNull(client.Create(job, new EnqueuedState()));
            SetMarker(new Dictionary<string, string> { ["ExpiresAt"] = DateTimeOffset.UtcNow.AddMinutes(-1).ToString("O") });
            Assert.IsNotNull(client.Create(job, new EnqueuedState()));
        }

        [TestMethod]
        public void ReservationCommitFailure_StopsCreationAndRecoversAfterExpiry()
        {
            storage.Before = method => { if (method.Name == "Commit") throw new InvalidOperationException("injected reservation failure"); };
            Assert.Throws<BackgroundJobClientException>(() => client.Create(job, new EnqueuedState()));
            storage.Before = null;
            Assert.AreEqual(0, storage.Inner.GetMonitoringApi().EnqueuedCount("default"));
            Assert.IsNotNull(client.Create(job, new EnqueuedState()));
        }

        [TestMethod]
        public void CommitAcknowledgementFailure_RetainsPersistedOwningJob()
        {
            var commits = 0;
            storage.After = method =>
            {
                if (method.Name == "Commit" && Interlocked.Increment(ref commits) == 2) throw new InvalidOperationException("commit succeeded but acknowledgement lost");
            };
            Assert.Throws<BackgroundJobClientException>(() => client.Create(job, new EnqueuedState()));
            using var persisted = storage.GetConnection();
            var id = persisted.GetAllEntriesFromHash(key)["JobId"];
            Assert.IsNotNull(id);
            Assert.IsNull(client.Create(job, new EnqueuedState()));
            using var connection = storage.GetConnection();
            Assert.AreEqual(id, connection.GetAllEntriesFromHash(key)["JobId"]);
        }

        [TestMethod]
        public void AutomaticRetry_RetainsSameJobProtection()
        {
            var id = client.Create(job, new EnqueuedState());
            client.ChangeState(id, new FailedState(new InvalidOperationException("temporary bulk failure")));
            using var connection = storage.GetConnection();
            Assert.AreEqual(ScheduledState.StateName, connection.GetJobData(id).State);
            Assert.IsNull(client.Create(job, new EnqueuedState()));
            Assert.AreEqual(id, connection.GetAllEntriesFromHash(key)["JobId"]);
        }

        [TestMethod]
        public void TerminalCleanup_UsesCommittedStateAndOlderCallbacksCannotClearReplacement()
        {
            var id = client.Create(job, new EnqueuedState());
            client.ChangeState(id, new DeletedState());
            var replacement = client.Create(job, new EnqueuedState());
            Assert.IsNotNull(replacement);
            using var connection = storage.GetConnection();
            using var transaction = connection.CreateWriteTransaction();
            filter.OnStateApplied(new ApplyStateContext(storage, connection, transaction, new BackgroundJob(id, job, DateTime.UtcNow), new DeletedState(), ProcessingState.StateName), transaction);
            transaction.Commit();
            Assert.AreEqual(replacement, connection.GetAllEntriesFromHash(key)["JobId"]);
        }

        [TestMethod]
        public void TerminalStateTransactionFailure_DoesNotReleaseProtection()
        {
            var id = client.Create(job, new EnqueuedState());
            storage.Before = method => { if (method.Name == "Commit") throw new InvalidOperationException("state transaction failed"); };
            Assert.Throws<BackgroundJobClientException>(() => client.ChangeState(id, new DeletedState()));
            storage.Before = null;
            Assert.IsNull(client.Create(job, new EnqueuedState()));
        }

        [TestMethod]
        public void PerformedBeforeRetryState_DoesNotRemoveMarker()
        {
            var id = client.Create(job, new EnqueuedState());
            using var connection = storage.GetConnection();
            var context = new PerformContext(storage, connection, new BackgroundJob(id, job, DateTime.UtcNow), new NullCancellationToken());
            filter.OnPerforming(new PerformingContext(context));
            filter.OnPerformed(new PerformedContext(context, null, false, new InvalidOperationException("retry")));
            Assert.AreEqual(id, connection.GetAllEntriesFromHash(key)["JobId"]);
            Assert.IsNull(client.Create(job, new EnqueuedState()));
        }

        [TestMethod]
        public void LegacyInspectionFailure_StopsCreationAndKeepsMarker()
        {
            SetMarker(new Dictionary<string, string> { ["Timestamp"] = "old" });
            storage.Before = method => { if (method.Name == "GetAllItemsFromSet") throw new InvalidOperationException("inspection unavailable"); };
            Assert.Throws<BackgroundJobClientException>(() => client.Create(job, new EnqueuedState()));
            storage.Before = null;
            using var connection = storage.GetConnection();
            Assert.AreEqual("old", connection.GetAllEntriesFromHash(key)["Timestamp"]);
        }

        [TestMethod]
        public void InfiniteTimeoutConfiguration_IsRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => filter.FingerprintTimeoutMinutes = 0);
            Assert.Throws<ArgumentOutOfRangeException>(() => filter.ReservationTimeoutMinutes = 0);
        }

        [TestMethod]
        public void DistributedLockFailure_StopsCreationAndLogsTheFingerprint()
        {
            storage.Before = method => { if (method.Name == "AcquireDistributedLock") throw new InvalidOperationException("lock unavailable"); };
            Assert.Throws<BackgroundJobClientException>(() => client.Create(job, new EnqueuedState()));
            Assert.IsTrue(logger.Messages.Any(message => message.Contains(key) && message.Contains("creation stopped")));
            Assert.AreEqual(0, storage.Inner.GetMonitoringApi().EnqueuedCount("default"));
        }

        [TestMethod]
        public void ExecutionLockFailure_StopsTheRefreshAndLogsTheOwningJob()
        {
            var id = client.Create(job, new EnqueuedState());
            using var connection = storage.GetConnection();
            var context = new PerformContext(storage, connection, new BackgroundJob(id, job, DateTime.UtcNow), new NullCancellationToken());
            storage.Before = method => { if (method.Name == "AcquireDistributedLock") throw new InvalidOperationException("lock unavailable"); };
            Assert.Throws<InvalidOperationException>(() => filter.OnPerforming(new PerformingContext(context)));
            Assert.IsTrue(logger.Messages.Any(message => message.Contains(id) && message.Contains("refresh did not start")));
        }

        [TestMethod]
        public void InitialOwnershipChanged_StopsQueuePersistenceAndPreservesReplacement()
        {
            var reads = 0;
            storage.Before = method =>
            {
                if (method.Name != "GetAllEntriesFromHash" || Interlocked.Increment(ref reads) != 2) return;
                using var connection = storage.Inner.GetConnection();
                connection.SetRangeInHash(key, new Dictionary<string, string>
                {
                    ["JobId"] = "replacement-owner",
                    ["Reservation"] = "replacement-token",
                    ["ExpiresAt"] = DateTimeOffset.UtcNow.AddDays(1).ToString("O")
                });
            };
            Assert.Throws<BackgroundJobClientException>(() => client.Create(job, new EnqueuedState()));
            storage.Before = null;
            using var persisted = storage.GetConnection();
            Assert.AreEqual("replacement-owner", persisted.GetAllEntriesFromHash(key)["JobId"]);
            Assert.AreEqual(0, storage.Inner.GetMonitoringApi().EnqueuedCount("default"));
        }

        private static ProcessingState Processing(string serverId) => (ProcessingState)Activator.CreateInstance(typeof(ProcessingState), BindingFlags.Instance | BindingFlags.NonPublic, null, new object[] { serverId, "worker" }, null);

        private void SetMarker(Dictionary<string, string> entries)
        {
            using var connection = storage.GetConnection();
            connection.SetRangeInHash(key, entries);
        }

        private sealed class NullCancellationToken : IJobCancellationToken
        {
            public CancellationToken ShutdownToken => CancellationToken.None;
            public void ThrowIfCancellationRequested() { }
        }

        private sealed class RecordingLogger : ILogger<DisableIdenticalQueuedItemsAttribute>
        {
            public ConcurrentBag<string> Messages { get; } = new ConcurrentBag<string>();
            public IDisposable BeginScope<TState>(TState state) => null;
            public bool IsEnabled(LogLevel level) => true;
            public void Log<TState>(LogLevel level, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter) => Messages.Add(formatter(state, exception));
        }

        public sealed class FaultingStorage : JobStorage
        {
            public InMemoryStorage Inner { get; } = new InMemoryStorage();
            public Action<MethodInfo> Before { get; set; }
            public Action<MethodInfo> After { get; set; }
            public override IMonitoringApi GetMonitoringApi() => Inner.GetMonitoringApi();
            public override IStorageConnection GetConnection() => ForwardingProxy.Wrap(Inner.GetConnection(), this);

            public object Invoke(object target, MethodInfo method, object[] arguments)
            {
                Before?.Invoke(method);
                object result;
                try { result = method.Invoke(target, arguments); }
                catch (TargetInvocationException exception) { ExceptionDispatchInfo.Capture(exception.InnerException).Throw(); throw; }
                if (result is IWriteOnlyTransaction transaction) result = ForwardingProxy.Wrap(transaction, this);
                After?.Invoke(method);
                return result;
            }
        }

        public class ForwardingProxy : DispatchProxy
        {
            private object target;
            private FaultingStorage storage;
            public static T Wrap<T>(T target, FaultingStorage storage) where T : class
            {
                var proxy = Create<T, ForwardingProxy>();
                ((ForwardingProxy)(object)proxy).target = target;
                ((ForwardingProxy)(object)proxy).storage = storage;
                return proxy;
            }
            protected override object Invoke(MethodInfo method, object[] arguments) => storage.Invoke(target, method, arguments);
        }
    }
}
