using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace MAD.Integration.Common.Settings
{
    public static class SettingsServiceCollectionExtensions
    {
        public static IServiceCollection AddIntegrationSettings<TSettings>(this IServiceCollection serviceDescriptors)
            where TSettings : class, new()
        {
            serviceDescriptors.Configure<TSettings>(IntegrationHost.DefaultConfiguration);
            serviceDescriptors.AddSingleton<TSettings>(y => y.GetRequiredService<IOptions<TSettings>>().Value);

            return serviceDescriptors;
        }
    }
}
