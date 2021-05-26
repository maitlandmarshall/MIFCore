using MAD.Integration.Common;
using Microsoft.Extensions.Hosting;
using System;

namespace MAD.Integration.TestApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var host = IntegrationHost.CreateDefaultBuilder()
                .UseStartup<Startup>()
                .Build();

            host.Run();
        }
    }
}
