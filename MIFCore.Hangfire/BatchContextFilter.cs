using Hangfire;
using Hangfire.Client;
using Hangfire.Common;
using Hangfire.Server;
using MIFCore.Hangfire.Utils;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace MIFCore.Hangfire
{
    public class BatchContextFilter : IClientFilter, IServerFilter
    {
        public void OnPerforming(PerformingContext filterContext)
        {
            var ctxIdJobParam = filterContext.GetBatchParameter<string>("Id");

            if (string.IsNullOrEmpty(ctxIdJobParam))
            {
                filterContext
                    .SetBatchParameter($"Id", Guid.NewGuid())
                    .SetBatchParameter($"Started", DateTime.UtcNow);
            }
            else
            {
                var jobDetails = JobStorage.Current.GetMonitoringApi().JobDetails(filterContext.BackgroundJob.Id);
                
                foreach (var kp in jobDetails.Properties)
                {
                    if (!kp.Key.StartsWith("ctx")) continue;

                    var jObj = JToken.Parse(kp.Value);
                    filterContext.Items[kp.Key] = jObj;
                }
            }
        }
        public void OnPerformed(PerformedContext filterContext) { }

        public void OnCreated(CreatedContext filterContext) { }
        public void OnCreating(CreatingContext filterContext)
        {
            var currentJob = BackgroundJobContext.Current;
            if (currentJob is null) return;

            var ctxIdJobParam = currentJob.GetBatchParameter<Guid?>("Id");
            if (!ctxIdJobParam.HasValue) return;

            foreach (var itm in currentJob.Items)
            {
                if (!itm.Key.StartsWith("ctx:")) continue;
                var key = itm.Key.Substring("ctx:".Length);

                filterContext.SetBatchParameter(key, itm.Value);
            }
        }

       
    }
}
