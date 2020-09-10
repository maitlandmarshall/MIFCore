using Hangfire.Common;
using Hangfire.Server;
using Hangfire.States;
using Hangfire.Storage;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MAD.Integration.Common.Analytics
{
    public class AppInsightsEventsFilter : JobFilterAttribute, IServerFilter
    {
        [ThreadStatic]
        private static IOperationHolder<RequestTelemetry> operationHolder;

        private readonly TelemetryClient telemetryClient;

        public AppInsightsEventsFilter(TelemetryClient telemetryClient)
        {
            this.telemetryClient = telemetryClient;
        }

        void IServerFilter.OnPerforming(PerformingContext filterContext)
        {
            operationHolder = this.telemetryClient.StartOperation<RequestTelemetry>($"{filterContext.BackgroundJob.Job.Type.Name}.{filterContext.BackgroundJob.Job.Method.Name}");
            operationHolder.Telemetry.Properties.Add("arguments", JsonConvert.SerializeObject(filterContext.BackgroundJob.Job.Args));

            var eventTelemetry = new EventTelemetry("Job Started");
            eventTelemetry.Properties.Add("jobName", $"{filterContext.BackgroundJob.Job.Type.Name}.{filterContext.BackgroundJob.Job.Method.Name}");
            eventTelemetry.Properties.Add("arguments", JsonConvert.SerializeObject(filterContext.BackgroundJob.Job.Args));
            eventTelemetry.Context.Operation.Id = operationHolder.Telemetry.Context.Operation.Id;

            this.telemetryClient.TrackEvent(eventTelemetry);   
        }

        void IServerFilter.OnPerformed(PerformedContext filterContext)
        {
            var eventTelemetry = new EventTelemetry();
            eventTelemetry.Properties.Add("jobName", $"{filterContext.BackgroundJob.Job.Type.Name}.{filterContext.BackgroundJob.Job.Method.Name}");
            eventTelemetry.Properties.Add("arguments", JsonConvert.SerializeObject(filterContext.BackgroundJob.Job.Args));
            eventTelemetry.Context.Operation.Id = operationHolder.Telemetry.Context.Operation.Id;

            if (filterContext.Exception != null)
            {
                var exception = filterContext.Exception;
                if (exception is JobPerformanceException perfEx)
                {
                    exception ??= perfEx.InnerException;
                }

                var exceptionTelemetry = new ExceptionTelemetry
                {
                    Exception = exception
                };
                exceptionTelemetry.Properties.Add("jobName", $"{filterContext.BackgroundJob.Job.Type.Name}.{filterContext.BackgroundJob.Job.Method.Name}");
                exceptionTelemetry.Properties.Add("arguments", JsonConvert.SerializeObject(filterContext.BackgroundJob.Job.Args));
                exceptionTelemetry.Context.Operation.Id = operationHolder.Telemetry.Context.Operation.Id;
                this.telemetryClient.TrackException(exceptionTelemetry);

                operationHolder.Telemetry.Success = false;
                operationHolder.Telemetry.ResponseCode = "500";
                eventTelemetry.Name = "Job Failed";
            }
            else
            {
                eventTelemetry.Name = "Job Succeeded";
            }

            this.telemetryClient.TrackEvent(eventTelemetry);
            this.telemetryClient.StopOperation(operationHolder);

            operationHolder.Dispose();
            operationHolder = null;
        }
    }
}
