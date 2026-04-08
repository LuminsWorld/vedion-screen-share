using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace VedionScreenShare.Services
{
    public class LicenseService
    {
        private static readonly HttpClient _http = new HttpClient();
        private const string VALIDATE_URL = "https://api.lemonsqueezy.com/v1/licenses/validate";

        // Cached result
        private static bool? _cachedResult = null;

        public static async Task<(bool valid, string message)> ValidateAsync(string licenseKey)
        {
            if (string.IsNullOrWhiteSpace(licenseKey))
                return (false, "No license key entered.");

            // Cache valid result for session
            if (_cachedResult == true)
                return (true, "Licensed");

            try
            {
                var body = new { license_key = licenseKey };
                string json = JsonSerializer.Serialize(body);

                var response = await _http.PostAsync(
                    VALIDATE_URL,
                    new StringContent(json, Encoding.UTF8, "application/json"));

                string responseBody = await response.Content.ReadAsStringAsync();

                using var doc = JsonDocument.Parse(responseBody);
                bool valid = doc.RootElement.TryGetProperty("valid", out var validProp)
                             && validProp.GetBoolean();

                if (valid)
                {
                    _cachedResult = true;
                    return (true, "License valid ✓");
                }

                string error = doc.RootElement.TryGetProperty("error", out var errProp)
                    ? errProp.GetString() ?? "Invalid license"
                    : "Invalid license key";

                return (false, error);
            }
            catch (Exception ex)
            {
                // If we can't reach server, allow offline use for 7 days
                // (implement with local timestamp check)
                return (false, $"Could not validate license: {ex.Message}");
            }
        }

        /// <summary>
        /// Save license key locally
        /// </summary>
        public static void SaveKey(string key)
        {
            var dir = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "VedionScreenShare");
            System.IO.Directory.CreateDirectory(dir);
            System.IO.File.WriteAllText(System.IO.Path.Combine(dir, "license.key"), key);
        }

        /// <summary>
        /// Load saved license key
        /// </summary>
        public static string LoadKey()
        {
            var path = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "VedionScreenShare", "license.key");

            return System.IO.File.Exists(path)
                ? System.IO.File.ReadAllText(path).Trim()
                : "";
        }
    }
}
