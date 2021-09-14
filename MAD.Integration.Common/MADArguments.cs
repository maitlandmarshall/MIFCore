using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MAD.Integration.Common
{
  public class MADArguments
  {
    private const int SETTINGS_REQURIED_OPTIONS = 1;

    private string settingsJsonPath = Path.Combine(Globals.BaseDirectory, "settings.json");
    /// <summary>
    /// Settings Json file absolute path
    /// </summary>
    public string SettingsJsonPath
    {
      get => settingsJsonPath;
    }

    /// <summary>
    /// Build settings based on the launch argument array
    /// </summary>
    /// <param name="args">launch args</param>
    public MADArguments(string[] args)
    {
      if (args != null)
      {
        for (int i = 0; i < args.Length; i++)
        {
          int options = OptionCount(args, i);

          switch (args[i])
          {
            case "-s":
            case "--settings":
              ProcessSettings(args, i, options);
              break;
          };

          // skip processed options
          i += options;
        }
      }
    }

    /// <summary>
    /// Set the settings parameter if value exists
    /// </summary>
    /// <param name="args"></param>
    /// <param name="ParamIndex"></param>
    /// <param name="optionCount"></param>
    private void ProcessSettings(string[] args, int ParamIndex, int optionCount)
    {
      if (optionCount == SETTINGS_REQURIED_OPTIONS)
      {
        // ensure file exists and get set absolute path
        FileInfo fi = new FileInfo( args[ParamIndex + 1] );
        if (fi.Exists )
        {
          this.settingsJsonPath = fi.FullName;
        } else
        {
          Console.WriteLine("Invalid file path provided for settings, using default value");
        }
      } else
      {
        Console.WriteLine($"Invalid number of options provided for settings. Expected {SETTINGS_REQURIED_OPTIONS}, got {optionCount}. Using default path for settings.");
      }

      /* 
       * TODO: decide what to do here for invalid cases 
       * Throwing an error here would be breaking.
       * But that could also be benificial in immediately detecting if the setttings are wrong so you don't run it expecting a different settings file to be loaded.
       * This is being called before services are configured as the arguments are required to direct service configuration so logging provided by hangfire is not available
       * for the moment this is reverting to the default settings path instead of breaking.
      */
    }

    /// <summary>
    /// Returns number of options following an argument parameter
    /// </summary>
    /// <param name="args">argument array</param>
    /// <param name="paramIndex">index of the provided</param>
    /// <returns></returns>
    private int OptionCount(string[] args, int paramIndex = 0)
    {
      int options = 0;

      for (var nextIndex = paramIndex + 1; nextIndex < args.Length; nextIndex++)
      {
        if (args[nextIndex].StartsWith("-"))
        {
          break;
        }
        else
        {
          options++;
        }
      }

      return options;
    }

  }

}
