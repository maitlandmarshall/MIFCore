using Hangfire;
using MAD.Integration.Common.Jobs;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace MAD.Integration.Common.Hangfire
{
    internal static class HangfireServiceCollectionExtensions
    {
        public static IServiceCollection AddHangfire(this IServiceCollection serviceDescriptors)
        {
            serviceDescriptors.AddHostedService<HangfireBackgroundService>();
            serviceDescriptors.AddSingleton<IGlobalConfiguration>(sp => GlobalConfiguration.Configuration);

            return serviceDescriptors;
        }
    }
}
