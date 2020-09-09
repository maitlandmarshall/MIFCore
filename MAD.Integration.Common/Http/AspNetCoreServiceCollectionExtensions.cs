using Hangfire;
using MAD.Integration.Common.Http;
using MAD.Integration.Common.Jobs;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Text;

namespace MAD.Integration.Common.Hangfire
{
    internal static class AspNetCoreServiceCollectionExtensions
    {
        public static IServiceCollection AddAspNetCore(this IServiceCollection serviceCollection, AspNetCoreConfig aspNetCoreConfig = null)
        {
            aspNetCoreConfig ??= new AspNetCoreConfig();

            serviceCollection.AddSingleton<AspNetCoreConfig>(aspNetCoreConfig);

            return serviceCollection;
        }
    }
}
