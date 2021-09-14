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

    private static MADArguments args = null;
    public static MADArguments Arguments
    {
      get {
        if (args == null)
        {
          args = new MADArguments(null);
        }
        return args;
      }
      set
      {
        args = value;
      }
    }
  }
}
