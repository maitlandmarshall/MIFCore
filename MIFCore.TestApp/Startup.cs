using Hangfire;
using MIFCore.Hangfire;
using Microsoft.Extensions.DependencyInjection;

namespace MIFCore.TestApp
{
    internal class Startup
    {
        public void ConfigureServices(IServiceCollection serviceDescriptors)
        {
            serviceDescriptors.AddScoped<SomeJob>();
            serviceDescriptors.AddControllers();
        }

        public void Configure()
        {

        }

        public void PostConfigure(IBackgroundJobClient backgroundJobClient)
        {
            backgroundJobClient.Enqueue<SomeJob>(y => y.DoTheJob());
            backgroundJobClient.Enqueue<SomeJob>(y => y.DoTheJobButError());
        }
    }
}