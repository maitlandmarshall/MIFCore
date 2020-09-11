using Microsoft.Extensions.Configuration;

namespace MAD.Integration.Common.Settings
{
    public static class SettingsConfigurationBuilderExtensions
    {
        public static IConfigurationBuilder UseSettingsFile(this IConfigurationBuilder builder)
        {
            builder
                .SetBasePath(Globals.BaseDirectory)
                .AddJsonFile("settings.json", optional: true);

            return builder;
        }
    }
}
