using Hangfire;
using MAD.Integration.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading.Tasks;

namespace MAD.Integration.TestApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var host = IntegrationHost.CreateDefaultBuilder()
                .UseStartup<Startup>()
                .UseAspNetCore()
                .Build();

            await host.RunAsync();
        }
    }
}
