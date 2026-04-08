using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace VedionScreenShare.Services
{
    public class LicenseService
    {
        private static readonly HttpClient _http = new HttpClient();
        private const string VALIDATE_URL = "https://vedion.cloud/api/license/validate";

        private static bool? _cachedResult = null;

        /// <summary>
        /// Get a unique machine ID for license binding
        /// </summary>
        public static string GetMachineId()
        {
            try
            {
                // Use a combination of machine name + user name + processor ID
                string raw = Environment.MachineName + Environment.UserName
                    + Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER");
                using var sha = System.Security.Cryptography.SHA256.Create();
                byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));
                return Convert.ToHexString(hash)[..16]; // First 16 chars
            }
            catch
            {
                return Environment.MachineName;
            }
        }

        public static async Task<(bool valid, string message)> ValidateAsync(string licenseKey)
        {
            if (string.IsNullOrWhiteSpace(licenseKey))
                return (false, "No license key entered.");

            if (_cachedResult == true)
                return (true, "Licensed");

            try
            {
                var body = new
                {
                    licenseKey = licenseKey,
                    machineId = GetMachineId()
                };

                string json = JsonSerializer.Serialize(body);
                var response = await _http.PostAsync(
                    VALIDATE_URL,
                    new StringContent(json, Encoding.UTF8, "application/json"));

                string responseBody = await response.Content.ReadAsStringAsync();

                using var doc = JsonDocument.Parse(responseBody);
                bool valid = doc.RootElement.TryGetProperty("valid", out var validProp)
                             && validProp.GetBoolean();

                string message = doc.RootElement.TryGetProperty("message", out var msgProp)
                    ? msgProp.GetString() ?? "Unknown"
                    : "Unknown";

                if (valid) _cachedResult = true;

                return (valid, message);
            }
            catch (Exception ex)
            {
                // Offline grace: allow if key is saved and looks valid
                string saved = LoadKey();
                if (!string.IsNullOrWhiteSpace(saved) && saved == licenseKey)
                    return (true, "Offline — using cached license");

                return (false, $"Could not validate: {ex.Message}");
            }
        }

        public static void SaveKey(string key)
        {
            var dir = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "VedionScreenShare");
            System.IO.Directory.CreateDirectory(dir);
            System.IO.File.WriteAllText(System.IO.Path.Combine(dir, "license.key"), key);
        }

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
