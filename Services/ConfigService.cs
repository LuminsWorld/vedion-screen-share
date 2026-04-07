using System;
using System.IO;
using System.Text.Json;
using VedionScreenShare.Models;

namespace VedionScreenShare.Services
{
    public static class ConfigService
    {
        private static readonly string ConfigDir  = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "VedionScreenShare");

        private static readonly string ConfigPath = Path.Combine(ConfigDir, "config.json");

        public static bool Exists() => File.Exists(ConfigPath);

        public static AppConfig Load()
        {
            try
            {
                string json = File.ReadAllText(ConfigPath);
                return JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
            }
            catch
            {
                return new AppConfig();
            }
        }

        public static void Save(AppConfig config)
        {
            try
            {
                Directory.CreateDirectory(ConfigDir);
                string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(ConfigPath, json);
            }
            catch { /* Silently fail if can't save */ }
        }

        public static void Delete()
        {
            try { File.Delete(ConfigPath); } catch { }
        }
    }
}
