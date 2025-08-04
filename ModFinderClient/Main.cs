using Microsoft.Win32;
using ModFinder.Mod;
using ModFinder.Util;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;

namespace ModFinder
{
  public static class Main
  {
    /// <summary>
    /// Modfinder version
    /// </summary>
    public static string ProductVersion = FileVersionInfo.GetVersionInfo(Assembly.GetEntryAssembly().Location).ProductVersion;
    /// <summary>
    /// Modfinder settings
    /// </summary>
    public static Settings Settings => m_Settings ??= Settings.Load();

    private static Settings m_Settings;

    /// <summary>
    /// Owlcat mods, use to toggle enabled status
    /// </summary>
    public static readonly OwlcatModificationSettingsManager OwlcatMods = new();

    /// <summary>
    /// Folder where we put modfinder stuff
    /// </summary>
    public static string AppFolder
    {
      get
      {
        if (!Directory.Exists(_appFolder))
        {
          _ = Directory.CreateDirectory(_appFolder);
        }
        return _appFolder;
      }
    }

    private static readonly string _appFolder =
      Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ModfinderRT");

    /// <summary>
    /// Get path to file in modfinder app folder
    /// </summary>
    /// <param name="file">file to get path to</param>
    public static string AppPath(string file) => Path.Combine(AppFolder, file);

    /// <summary>
    /// Try to read a file in the modfinder app folder
    /// </summary>
    /// <param name="file">file name</param>
    /// <param name="contents">contents of the file will be put here</param>
    /// <returns>true if the file exists and was read successfully</returns>
    public static bool TryReadFile(string file, out string contents)
    {
      var path = AppPath(file);
      if (File.Exists(path))
      {
        contents = File.ReadAllText(path);
        return true;
      }
      else
      {
        contents = null;
        return false;
      }
    }

    /// <summary>
    /// %AppData%/.. 
    /// </summary>
    public static string AppDataRoot => Directory.GetParent(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)).FullName;

    /// <summary>
    /// RogueTrader data directory, i.e. "%AppData%/../LocalLow/Owlcat Games/Warhammer 40000 Rogue Trader"
    /// </summary>
    public static string RTDataDir => Path.Combine(AppDataRoot, "LocalLow", "Owlcat Games", "Warhammer 40000 Rogue Trader");

    /// <summary>
    /// Prompts user to manually input path to RT
    /// </summary>
    public static void GetRTPathManual()
    {
      Logger.Log.Info($"RTPath not found, prompting user.");
      var dialog = new OpenFileDialog
      {
        FileName = "WH40KRT",
        DefaultExt = ".exe",
        Filter = "Executable (.exe)|*.exe",
        Title = "Select WH40KRT.exe (in the installation directory)"
      };

      var result = dialog.ShowDialog();
      if (result is not null && result.Value)
      {
        _RTPath = new(Path.GetDirectoryName(dialog.FileName));
      }
      else
      {
        Logger.Log.Error("Unable to find RT installation path.");
      }
    }
    private static Regex extractGameVersion = new(".*?Found game version string: '(.*)'.*");
    public static ModVersion GameVersion;

    private static ModVersion GameVersionRaw
    {
      get
      {
        if (RTPath == null) { return new(); }

        var log = Path.Combine(RTDataDir, "Player.log");
        if (!File.Exists(log))
        {
          return new();
        }

        try
        {
          using var sr = new StreamReader(File.OpenRead(log));

          for (int i = 0; i < 200; i++)
          {
            var line = sr.ReadLine();
            if (line is null) return new();

            var match = extractGameVersion.Match(line);
            if (match.Success)
            {
              Logger.Log.Info($"Found game version: {match.Groups[1].Value}");
              return ModVersion.Parse(match.Groups[1].Value);
            }

          }
        }
        catch (Exception e)
        {
          Logger.Log.Error("Unable to find RT game version", e);
        }

        return new();
      }
    }

    /// <summary>
    /// Path to the installed RT folder
    /// </summary>
    public static DirectoryInfo RTPath
    {
      get
      {
        if (_RTPath != null) return _RTPath;

        if (Directory.Exists(Settings.RTPath) && File.Exists(Path.Combine(Settings.RTPath, "WH40KRT.exe")))
        {
          Logger.Log.Info($"Using RTPath from settings: {Settings.RTPath}");
          _RTPath = new(Settings.RTPath);
        }
        else if (Directory.Exists(Settings.AutoRTPath) && File.Exists(Path.Combine(Settings.AutoRTPath, "WH40KRT.exe")))
        {
          Logger.Log.Info($"Using auto RTPath from settings: {Settings.AutoRTPath}");
          _RTPath = new(Settings.AutoRTPath);
        }
        else
        {
          var log = Path.Combine(RTDataDir, "Player.log");
          if (!File.Exists(log))
          {
            GetRTPathManual();
          }
          else
          {
            try
            {
              Logger.Log.Info($"Getting RTPath from UMM log.");

              using var sr = new StreamReader(File.OpenRead(log));
              var firstline = sr.ReadLine();

              var extractPath = new Regex(".*?'(.*)'");
              _RTPath = new(extractPath.Match(firstline).Groups[1].Value);
              _RTPath = _RTPath.Parent.Parent;
            }
            catch (Exception e)
            {
              Logger.Log.Error("Unable to find RT installation path, Prompting manual input.", e);
              GetRTPathManual();
            }
          }

          Settings.AutoRTPath = RTPath.FullName;
          Settings.Save();
        }

        GameVersion = GameVersionRaw;
        return _RTPath;
      }
    }

    public static readonly string UMMParamsPath = Path.Combine(RTDataDir, @"UnityModManager\Params.xml");

    public static readonly string UMMInstallPath = Path.Combine(RTDataDir, "UnityModManager");

    private static DirectoryInfo _RTPath; 
  }
}
