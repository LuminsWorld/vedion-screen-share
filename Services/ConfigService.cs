using System.IO;
using System.Text.Json;
using VedionScreenShare.Models;

namespace VedionScreenShare.Services;

public static class ConfigService
{
    private static readonly string _dir  = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        Constants.AppDataFolder);

    private static readonly string _path = Path.Combine(_dir, Constants.ConfigFileName);

    private static readonly JsonSerializerOptions _opts = new() { WriteIndented = true };

    public static AppConfig Load()
    {
        try
        {
            if (!File.Exists(_path)) return new AppConfig();
            string json = File.ReadAllText(_path);
            return JsonSerializer.Deserialize<AppConfig>(json, _opts) ?? new AppConfig();
        }
        catch { return new AppConfig(); }
    }

    public static void Save(AppConfig config)
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(_path, JsonSerializer.Serialize(config, _opts));
    }

    public static bool Exists() => File.Exists(_path);
}
