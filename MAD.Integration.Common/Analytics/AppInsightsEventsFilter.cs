using Hangfire;
using Hangfire.Server;
using Hangfire.States;
using Hangfire.Storage;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;

namespace MAD.Integration.Common.Analytics
{
    public class AppInsightsEventsFilter : IServerFilter, IApplyStateFilter
    {
        static ConcurrentDictionary<string, IOperationHolder<RequestTelemetry>> operationHolderDict = new ConcurrentDictionary<string, IOperationHolder<RequestTelemetry>>();

        private readonly TelemetryClient telemetryClient;

        public AppInsightsEventsFilter(TelemetryClient telemetryClient)
        {
            this.telemetryClient = telemetryClient;
        }

        void IServerFilter.OnPerforming(PerformingContext filterContext)
        {
            var appName = Path.GetFileNameWithoutExtension(Globals.MainModule);
            var operationId = $"{appName}.{filterContext.BackgroundJob.Id}";

            var operationHolder = telemetryClient.StartOperation<RequestTelemetry>(GetJobName(filterContext.BackgroundJob), operationId);
            operationHolder.Telemetry.Properties.Add("arguments", GetJobArguments(filterContext.BackgroundJob));
            operationHolder.Telemetry.Properties.Add("appName", appName);

            operationHolderDict.TryAdd(filterContext.BackgroundJob.Id, operationHolder);

            var eventTelemetry = new EventTelemetry("Job Started");
            eventTelemetry.Context.Operation.Id = operationId;
            eventTelemetry.Context.Operation.ParentId = operationId;

            telemetryClient.TrackEvent(eventTelemetry);
        }

        void IServerFilter.OnPerformed(PerformedContext filterContext)
        {
            var operationHolder = operationHolderDict[filterContext.BackgroundJob.Id];
            var operationId = operationHolder.Telemetry.Context.Operation.Id;

            var eventTelemetry = new EventTelemetry();
            eventTelemetry.Context.Operation.Id = operationId;
            eventTelemetry.Context.Operation.ParentId = operationId;

            if (filterContext.Exception != null)
            {
                var exception = filterContext.Exception;
                if (exception is JobPerformanceException perfEx)
                {
                    exception = perfEx.InnerException;
                }

                var exceptionTelemetry = new ExceptionTelemetry()
                {
                    Exception = exception
                };

                exceptionTelemetry.Context.Operation.Id = operationId;
                exceptionTelemetry.Context.Operation.ParentId = operationId;
                telemetryClient.TrackException(exceptionTelemetry);

                operationHolder.Telemetry.Success = false;
                operationHolder.Telemetry.ResponseCode = "Attempt Failed";

                eventTelemetry.Name = "Job Attempt Failed";
            }
            else
            {
                operationHolder.Telemetry.Success = true;
                operationHolder.Telemetry.ResponseCode = "Success";

                eventTelemetry.Name = "Job Succeeded";
            }

            telemetryClient.TrackEvent(eventTelemetry);
        }

        public void OnStateApplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
        {
            operationHolderDict.TryGetValue(context.BackgroundJob.Id, out var operationHolder);

            try
            {
                if (context.NewState.Name != FailedState.StateName) return;

                operationHolder.Telemetry.Success = false;
                operationHolder.Telemetry.ResponseCode = "Failed";

                var eventTelemetry = new EventTelemetry("Job Failed");
                eventTelemetry.Context.Operation.Id = operationHolder.Telemetry.Context.Operation.Id;
                eventTelemetry.Context.Operation.ParentId = operationHolder.Telemetry.Context.Operation.Id;

                telemetryClient.TrackEvent(eventTelemetry);
            }
            finally
            {
                if (operationHolder != null)
                {
                    telemetryClient.StopOperation(operationHolder);

                    operationHolder.Dispose();
                    operationHolderDict.TryRemove(context.BackgroundJob.Id, out var _);
                }
            }
        }

        public void OnStateUnapplied(ApplyStateContext context, IWriteOnlyTransaction transaction) { }

        private string GetJobName(BackgroundJob backgroundJob)
        {
            return $"{backgroundJob.Job.Type.Name}.{backgroundJob.Job.Method.Name}";
        }

        private string GetJobArguments(BackgroundJob backgroundJob)
        {
            return JsonConvert.SerializeObject(backgroundJob.Job.Args);
        }

    }
}
