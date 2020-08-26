using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
            string settingsPath = Path.Combine(Globals.BaseDirectory, "settings.json");

            FileInfo defaultSettings = new FileInfo("settings.default.json");
            FileInfo settings = new FileInfo(settingsPath);

            if (!settings.Exists)
            {
                if (defaultSettings.Exists)
                {
                    defaultSettings.CopyTo(settingsPath);
                }
                else
                {
                    settings.Create().Dispose();
                }
            }

            IConfigurationRoot config = new ConfigurationBuilder()
                .SetBasePath(Globals.BaseDirectory)
                .AddJsonFile("settings.json", optional: true, reloadOnChange: true)
                .Build();

            serviceDescriptors.Configure<TSettings>(y =>
            {
                config.Bind(y);
            });

            return serviceDescriptors;
        }
    }
}
