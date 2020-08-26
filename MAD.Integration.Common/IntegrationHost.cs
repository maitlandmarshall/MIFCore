using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MAD.Integration.Common
{
    public class IntegrationHost
    {
        public static IIntegrationHostBuilder CreateDefaultBuilder()
        {
            return new IntegrationHostBuilder();
        }
    }
}
