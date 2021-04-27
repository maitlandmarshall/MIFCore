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
            get => Path.GetDirectoryName(AppContext.BaseDirectory);
        }
    }
}
