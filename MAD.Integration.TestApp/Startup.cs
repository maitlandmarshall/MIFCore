using Hangfire;
using MAD.Integration.Common.Jobs;
using Microsoft.Extensions.DependencyInjection;

namespace MAD.Integration.TestApp
{
    internal class Startup
    {
        public void ConfigureServices(IServiceCollection serviceDescriptors)
        {
            serviceDescriptors.AddScoped<SomeJob>();
        }

        public void Configure()
        {
            
        }

        public void PostConfigure(IBackgroundJobClient backgroundJobClient)
        {
            backgroundJobClient.Enqueue<SomeJob>(y => y.DoTheJob());
        }
    }
}