using System.Security.Cryptography;
using System.Text;

namespace ModFinder.Util
{
  public class Settings
  {
    public string AutoRTPath { get; set; }
    public string RTPath { get; set; }
    public byte[] NexusApiKeyBytes { get; set; }
    public string MaybeGetNexusKey()
    {
      if (NexusApiKeyBytes == null)
      {
        return null;
      }
      var plain = ProtectedData.Unprotect(NexusApiKeyBytes, null, DataProtectionScope.CurrentUser);
      return Encoding.UTF8.GetString(plain);
    }

    private static Settings _Instance;
    public static Settings Load()
    {
      if (_Instance == null)
      {
        if (Main.TryReadFile("Settings.json", out var settingsRaw))
          _Instance = IOTool.FromString<Settings>(settingsRaw);
        else
        {
          _Instance = new();
          _Instance.Save();
        }
      }

      return _Instance;
    }

    public void Save()
    {
      IOTool.SafeRun(() =>
      {
        IOTool.Write(this, Main.AppPath("Settings.json"));
      });
    }
  }
}
