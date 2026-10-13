using Hangfire;
using Hangfire.Common;
using Hangfire.InMemory;
using MIFCore.Hangfire;
using System;
using System.Linq;

namespace MIFCore.Tests
{
    internal sealed class HangfireTestServer : IDisposable
    {
        private readonly BackgroundJobServer server;
        private readonly JobStorage previousStorage;
        private readonly JobFilter[] previousFilters;

        public HangfireTestServer(object extraFilter = null)
        {
            try { previousStorage = JobStorage.Current; }
            catch (InvalidOperationException) { previousStorage = new InMemoryStorage(); }
            previousFilters = GlobalJobFilters.Filters.ToArray();

            GlobalJobFilters.Filters.Clear();
            GlobalJobFilters.Filters.Add(new BackgroundJobContext());
            GlobalJobFilters.Filters.Add(new AutomaticRetryAttribute { Attempts = 0 });
            GlobalJobFilters.Filters.Add(new ContinuationsSupportAttribute());
            if (extraFilter != null) GlobalJobFilters.Filters.Add(extraFilter);

            var storage = new InMemoryStorage();
            GlobalConfiguration.Configuration.UseRecommendedSerializerSettings().UseStorage(storage);
            server = new BackgroundJobServer(new BackgroundJobServerOptions
            {
                WorkerCount = 1,
                SchedulePollingInterval = TimeSpan.FromMilliseconds(50)
            }, storage);
        }

        public void Dispose()
        {
            server.Dispose();
            GlobalJobFilters.Filters.Clear();
            foreach (var filter in previousFilters) GlobalJobFilters.Filters.Add(filter.Instance, filter.Order);
            JobStorage.Current = previousStorage;
        }
    }
}
