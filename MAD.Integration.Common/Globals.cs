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
#if DEBUG
                return Directory.GetCurrentDirectory();

#else
                return Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
#endif
            }
        }
    }
}
