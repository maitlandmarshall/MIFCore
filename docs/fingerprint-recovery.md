# Fingerprint ownership and recovery

`DisableIdenticalQueuedItemsAttribute` prevents another job with the same fingerprint from being created while an existing job is queued, running, scheduled for retry, or awaiting continuation. Version 2 adds finite recovery deadlines and committed job ownership. The default fingerprint calculation and existing `fpt:` hash keys are preserved.

## Configuration

| Setting | Default | Meaning |
| --- | --- | --- |
| `FingerprintTimeoutMinutes` | 1440 | Recovery deadline written after ownership is established and when active protection is renewed. |
| `ReservationTimeoutMinutes` | 5 | Deadline for a creation reservation that has not acquired a committed owning job. |
| `IncludeFailedJobs` | false | Whether an exhausted failed job continues to block another request. Scheduled retries remain protected regardless. |
| `FingerprintFactory` | Existing job fingerprint | Optional function grouping related jobs under one logical refresh. |
| `ContinueChildJobs` | false | Allows a running job to create distinct child jobs under the same reservation. |

Both timeout settings must be greater than zero. A timeout is a recovery deadline, not a maximum permitted job duration. Active committed jobs retain their protection even after the deadline passes. A scheduled poll can therefore wait for days without allowing another extraction.

## Persistence and concurrency

The hash stores `Timestamp`, `ExpiresAt`, `JobId`, `Reservation`, `MethodName` and `TypeName`. Job parameters store `FingerprintReservation` and `FingerprintKey`; `fpj:` sets track all jobs sharing a refresh. Hash and set keys remain within the 100-character limit in existing SQL storage.

A distributed creation lock stays held from reservation through job persistence. Initial-state application binds the owning job and membership in the same storage transaction that queues it. If the reservation has changed before that transaction, creation fails instead of queuing an unowned job. If a commit succeeds but its acknowledgement is lost, the persisted owner remains available for inspection. A failed unbound creation removes only its matching reservation; a cleanup failure is logged and leaves the finite creation deadline intact.

Every execution, including an older queued job, takes a separate distributed lock for the refresh. Unrelated jobs cannot borrow an active reservation. When child continuation is enabled, only the current job's matching reservation can create another distinct child. This protects a multi-stage refresh through its final page.

## Legacy jobs and terminal states

A missing marker, an expired unbound reservation, or a legacy marker without ownership triggers inspection of active queues and scheduled/processing/awaiting indexes. Every matching active job is adopted. This includes an old child job whose root fingerprint has already disappeared. Failed storage reads stop creation; they never count as evidence that the refresh is absent.

State-filter callbacks run before the caller commits the new state. The filter therefore retains protection through callbacks and inspects committed states under the creation lock on the next request. Successful/deleted jobs, exhausted failures when `IncludeFailedJobs` is false, and missing jobs allow recovery. An older callback cannot erase a replacement reservation. Cleanup is deliberately deferred rather than performed before the state commit.

`ExpiresAt` is a logical deadline. The hash does not receive an unconditional storage TTL: expiring it independently of its queued or running owner would lose protection during long jobs or an outage. Terminal markers may remain stored until the next matching request, but do not block that request.

## Diagnostics and targeted recovery

Information logs include the fingerprint, owning job when available, deadline and decision: reserved, adopted, renewed, duplicate suppressed, or recovered. Error logs include the same available context and the storage/cleanup exception.

Inspect the named owning job and every `fpj:` member before considering recovery. Scheduled retries and child jobs are active work. A processing job after worker interruption should be resumed or requeued using the same job ID through Hangfire's normal recovery. A database outage or unreadable active job is a reason to restore storage visibility, not remove protection. Confirm that all old workers are stopped before introducing the new filter; older workers do not participate in its execution locks.

No broad fingerprint deletion is required for this upgrade. An abandoned legacy marker or unbound expired reservation is recovered by the next eligible request. Any manual job-state or storage change remains a separate operational action after the exact affected jobs have been verified.

## Runtime and release compatibility

The proposed releases are `MIFCore.Common` 1.0.3, `MIFCore.Hangfire` 2.0.0 and `MIFCore` 2.0.0. The host's new major version targets .NET 10; its previous 1.x package remains the explicit choice for older applications. Common retains its .NET Standard 2.1 target and adds .NET 10; Hangfire retains .NET Standard 2.1.

The .NET 10 host uses the shared ASP.NET Core framework, serviced Windows hosting support, and Microsoft.Data.SqlClient for database bootstrap. Linux `dotnet` hosting resolves settings from the application working directory. Storage initialization and startup callbacks complete in `StartAsync`, before the host reports successful startup. This follows the [.NET 10 background-service change](https://learn.microsoft.com/en-us/dotnet/core/compatibility/extensions/10.0/backgroundservice-executeasync-task).

Hangfire 1.8.25 retains the existing data compatibility settings. Heavy SQL migrations are explicitly disabled: an existing schema 7 stays at 7; a new empty database receives the provider's current schema 9. See the [Hangfire upgrade guidance](https://docs.hangfire.io/en/latest/upgrade-guides/upgrading-to-hangfire-1.8.html). Publish and verify all three package versions before upgrading consumers. Pull-request workflows run the full test suite and cannot publish packages; master release workflows still publish each package.

## Validation

The focused tests cover stale/missing legacy markers, active queue waits and long retries, concurrent creation, lost commit acknowledgement, failed persistence/cleanup, failed locks, retry transitions and terminal-state transaction failure. The full shared-library suite also exercises asynchronous job context, nested batch parameters and last-success tracking through actual in-memory workers.

Consumer integration checks must additionally use the selected storage provider and service host. A controlled worker shutdown/restart is distinct from an abrupt process or machine failure; recovery timing after an abrupt failure depends on the storage provider and server watchdog.
