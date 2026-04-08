using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;

namespace VedionScreenShare.Services;

public record UpdateInfo(string Version, string DownloadUrl, string ReleaseNotes);

public static class UpdateService
{
    private static readonly HttpClient _http = new();

    public static async Task<UpdateInfo?> CheckForUpdateAsync()
    {
        try
        {
            var req = new HttpRequestMessage(HttpMethod.Get, Constants.ReleasesApiUrl);
            req.Headers.UserAgent.Add(new ProductInfoHeaderValue("VedionScreenShare", Constants.AppVersion));

            var resp = await _http.SendAsync(req);
            if (!resp.IsSuccessStatusCode) return null;

            var json = await resp.Content.ReadAsStringAsync();
            var doc  = JsonDocument.Parse(json);
            var root = doc.RootElement;

            string latestTag = root.GetProperty("tag_name").GetString()?.TrimStart('v') ?? "";
            if (!IsNewer(latestTag, Constants.AppVersion)) return null;

            // Find installer asset (.exe)
            string? downloadUrl = null;
            if (root.TryGetProperty("assets", out var assets))
            {
                foreach (var asset in assets.EnumerateArray())
                {
                    string? name = asset.TryGetProperty("name", out var n) ? n.GetString() : null;
                    if (name?.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        downloadUrl = asset.GetProperty("browser_download_url").GetString();
                        break;
                    }
                }
            }

            string notes = root.TryGetProperty("body", out var b) ? b.GetString() ?? "" : "";
            return new UpdateInfo($"v{latestTag}", downloadUrl ?? "", notes);
        }
        catch { return null; }
    }

    public static async Task DownloadAndInstallAsync(string downloadUrl)
    {
        string tmp = Path.Combine(Path.GetTempPath(), "VedionScreenShare-Update.exe");

        var bytes = await _http.GetByteArrayAsync(downloadUrl);
        await File.WriteAllBytesAsync(tmp, bytes);

        // Launch installer and exit current instance
        Process.Start(new ProcessStartInfo(tmp)
        {
            UseShellExecute = true,
            Arguments = "/SILENT"  // Inno Setup silent flag
        });

        Environment.Exit(0);
    }

    private static bool IsNewer(string latest, string current)
    {
        if (!Version.TryParse(latest, out var l)) return false;
        if (!Version.TryParse(current, out var c)) return false;
        return l > c;
    }
}
