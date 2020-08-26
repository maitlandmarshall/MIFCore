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
            get => Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
        }

        public static string SettingsDirectory
        {
            get => Path.Combine(BaseDirectory, "settings");
        }
    }
}
