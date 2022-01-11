using Hangfire;
using MAD.Integration.Common.Settings;
using Microsoft.Extensions.DependencyInjection;

namespace MAD.Integration.TestApp
{
    internal class Startup
    {
        public void ConfigureServices(IServiceCollection serviceDescriptors)
        {
            serviceDescriptors.AddIntegrationSettings<AppConfig>();
            serviceDescriptors.AddScoped<SomeJob>();
            serviceDescriptors.AddControllers();
        }

        public void Configure()
        {
            
        }

        public void PostConfigure(IBackgroundJobClient backgroundJobClient)
        {
            //backgroundJobClient.Enqueue<SomeJob>(y => y.DoTheJob());
            backgroundJobClient.Enqueue<SomeJob>(y => y.DoTheJobButError());
        }
    }
}