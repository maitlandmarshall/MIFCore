using Microsoft.Extensions.Hosting;
using MIFCore;
using MIFCore.Hangfire.Analytics;
using MIFCore.Http;
using System.Threading.Tasks;

namespace MIFCore.TestApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var host = IntegrationHost.CreateDefaultBuilder()
                .UseStartup<Startup>()
                .UseAspNetCore()
                .UseAppInsights()
                .Build();

            await host.RunAsync();
        }
    }
}
