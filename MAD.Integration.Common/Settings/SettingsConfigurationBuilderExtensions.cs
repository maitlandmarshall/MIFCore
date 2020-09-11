using Microsoft.Extensions.Configuration;
using System.IO;

namespace MAD.Integration.Common.Settings
{
    public static class SettingsConfigurationBuilderExtensions
    {
        public static IConfigurationBuilder UseSettingsFile(this IConfigurationBuilder builder)
        {
            BuildSettingsFile();

            builder
                .SetBasePath(Globals.BaseDirectory)
                .AddJsonFile("settings.json", optional: true);

            return builder;
        }

        private static void BuildSettingsFile()
        {
            var settingsPath = Path.Combine(Globals.BaseDirectory, "settings.json");
            var defaultSettings = new FileInfo("settings.default.json");
            var settings = new FileInfo(settingsPath);

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
        }
    }
}
