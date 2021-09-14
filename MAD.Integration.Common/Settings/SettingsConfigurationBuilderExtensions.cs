using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Diagnostics;
using System.IO;

namespace MAD.Integration.Common.Settings
{
  public static class SettingsConfigurationBuilderExtensions
  {
    public static IConfigurationBuilder UseSettingsFile(this IConfigurationBuilder builder)
    {

      BuildSettingsFile();

      var settingsRelative = Path.GetRelativePath(Globals.BaseDirectory, Globals.Arguments.SettingsJsonPath);

      builder
          .SetBasePath(Globals.BaseDirectory)
          .AddJsonFile(settingsRelative, optional: false)
          .AddJsonFile("settings.default.json", optional: true);

      return builder;
    }

    private static void BuildSettingsFile()
    {
      var settings = new FileInfo(Globals.Arguments.SettingsJsonPath);

      if (!settings.Exists)
      {
        File.WriteAllText(settings.FullName, JsonConvert.SerializeObject(new
        {
          ConnectionString = "",
          BindingPort = 666,
          InstrumentationKey = ""
        }));
      }
    }
  }
}
