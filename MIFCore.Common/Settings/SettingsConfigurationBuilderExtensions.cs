using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.IO;

namespace MIFCore.Common.Settings
{
    public static class SettingsConfigurationBuilderExtensions
    {
        public static IConfigurationBuilder UseSettingsFile(this IConfigurationBuilder builder)
        {
            BuildSettingsFile();

            builder
                .SetBasePath(Globals.BaseDirectory)
                .AddJsonFile("settings.json", optional: false)
                .AddJsonFile("settings.default.json", optional: true);

            return builder;
        }

        private static void BuildSettingsFile()
        {
            var settingsPath = Path.Combine(Globals.BaseDirectory, "settings.json");
            var settings = new FileInfo(settingsPath);

            if (!settings.Exists)
            {
                File.WriteAllText(settings.FullName, JsonConvert.SerializeObject(new
                {
                    ConnectionString = "",
                    BindingPort = 1337,
                    InstrumentationKey = ""
                }));
            }
        }
    }
}
