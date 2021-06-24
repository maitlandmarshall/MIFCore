using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace MAD.Integration.Common
{
    public static class Globals
    {
        public static string BaseDirectory
        {
            get 
            {
                var mainModule = Process.GetCurrentProcess().MainModule.FileName;

                // Handle if called from dotnet ef migrations or other tool
                if (Path.GetFileName(mainModule) == "dotnet.exe")
                {
                    return Directory.GetCurrentDirectory();
                }

                // Handle if running normally or as a contained single exe service
                return Path.GetDirectoryName(mainModule);
            }
        }
    }
}
