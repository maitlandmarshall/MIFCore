using Microsoft.Extensions.Hosting;
using MIFCore.Hangfire.Analytics;
using MIFCore.Hangfire.JobActions;
using MIFCore.Http;

namespace MIFCore.TestApp
{
    class Program
    {
        public static void Main(string[] args) => CreateHostBuilder(args).Build().Run();

        public static IHostBuilder CreateHostBuilder(string[] args)
            => IntegrationHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .UseJobActions()
                .UseAspNetCore()
                .UseAppInsights();
    }
}
