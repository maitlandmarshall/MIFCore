using MIFCore.Common.Settings;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;
using System.IO;

namespace MIFCore.Common
{
    public static class Globals
    {
        public static IConfiguration DefaultConfiguration { get; } = new ConfigurationBuilder().UseSettingsFile().Build();

        public static string BaseDirectory
        {
            get
            {
                var mainModule = MainModule;

                // Handle if called from dotnet ef migrations or other tool
                if (Path.GetFileName(mainModule) == "dotnet.exe")
                {
                    return Directory.GetCurrentDirectory();
                }

                // Handle if running normally or as a contained single exe service
                return Path.GetDirectoryName(mainModule);
            }
        }

        public static string MainModule
        {
            get
            {
                return Process.GetCurrentProcess().MainModule.FileName;
            }
        }
    }
}
