using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using VedionScreenShare.Services;

namespace VedionScreenShare.Services;

public enum LicenseStatus { Valid, NoLicense, MachineLimitReached, InvalidKey, Error }

public record LicenseResult(LicenseStatus Status, string Message);

public class LicenseService
{
    // ── Machine fingerprint ───────────────────────────────────────────────

    public static string GetMachineId()
    {
        try
        {
            string raw = Environment.MachineName
                + Environment.UserName
                + (Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER") ?? "");
            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
            return Convert.ToHexString(hash)[..16];
        }
        catch { return Environment.MachineName; }
    }

    // ── Validate license for logged-in user ───────────────────────────────

    public static async Task<LicenseResult> ValidateAsync(string idToken, string uid)
    {
        try
        {
            // 1. Get user doc to find their license key
            var userDoc = await FirestoreService.GetDocumentAsync(idToken, $"users/{uid}");
            if (userDoc is null)
                return new(LicenseStatus.NoLicense, "No account found. Please sign up on vedion.cloud first.");

            string? licenseKey = FirestoreService.GetString(userDoc.Value, "screenShareLicense");
            if (string.IsNullOrWhiteSpace(licenseKey))
                return new(LicenseStatus.NoLicense, "No license found. Purchase at vedion.cloud/shop.");

            // 2. Get license doc
            var licDoc = await FirestoreService.GetDocumentAsync(idToken, $"licenses/{licenseKey}");
            if (licDoc is null)
                return new(LicenseStatus.InvalidKey, "License key not found. Contact support.");

            // 3. Check machine binding
            string machineId    = GetMachineId();
            var machines        = FirestoreService.GetArray(licDoc.Value, "machines");
            int monthlyChanges  = FirestoreService.GetInt(licDoc.Value, "machineChangesThisMonth") ?? 2;
            string? resetDate   = FirestoreService.GetString(licDoc.Value, "monthResetDate");

            // Reset monthly counter if needed
            if (ShouldResetCounter(resetDate))
            {
                await ResetMonthlyCounterAsync(idToken, licenseKey);
                monthlyChanges = 2;
            }

            // Check if this machine is already registered
            bool machineKnown = machines?.Any(m =>
                FirestoreService.GetString(m, "id") == machineId) ?? false;

            if (machineKnown)
                return new(LicenseStatus.Valid, "Licensed ✓");

            // New machine — check if slots remain
            int machineCount = machines?.Length ?? 0;
            if (machineCount >= 2 && monthlyChanges <= 0)
                return new(LicenseStatus.MachineLimitReached,
                    "Machine limit reached (2 active, 0 monthly changes left). Contact support.");

            // Register this machine
            await RegisterMachineAsync(idToken, licenseKey, machineId, machines, monthlyChanges);
            return new(LicenseStatus.Valid, "Licensed ✓ (new machine registered)");
        }
        catch (Exception ex)
        {
            return new(LicenseStatus.Error, $"License check failed: {ex.Message}");
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static bool ShouldResetCounter(string? resetDateStr)
    {
        if (!DateTime.TryParse(resetDateStr, out var resetDate)) return true;
        return DateTime.UtcNow >= resetDate;
    }

    private static async Task ResetMonthlyCounterAsync(string idToken, string licenseKey)
    {
        await FirestoreService.PatchDocumentAsync(idToken, $"licenses/{licenseKey}",
            new
            {
                machineChangesThisMonth = 2,
                monthResetDate = FirstOfNextMonth().ToString("yyyy-MM-dd"),
            },
            new[] { "machineChangesThisMonth", "monthResetDate" });
    }

    private static async Task RegisterMachineAsync(
        string idToken, string licenseKey, string machineId,
        System.Text.Json.JsonElement[]? existingMachines, int changesLeft)
    {
        // Build new machines array with this machine added (keep max 2)
        var newMachines = new List<object>();
        if (existingMachines is not null)
        {
            foreach (var m in existingMachines)
            {
                string? mid = FirestoreService.GetString(m, "id");
                if (!string.IsNullOrEmpty(mid))
                    newMachines.Add(new { id = mid, addedAt = FirestoreService.GetString(m, "addedAt") ?? "" });
            }
        }
        // Remove oldest if already at 2
        if (newMachines.Count >= 2) newMachines.RemoveAt(0);
        newMachines.Add(new { id = machineId, addedAt = DateTime.UtcNow.ToString("o") });

        // We'll write machines as individual fields since array update via REST is complex
        // Simpler: store as machineId1, machineId2 fields
        await FirestoreService.PatchDocumentAsync(idToken, $"licenses/{licenseKey}",
            new
            {
                machineChangesThisMonth = Math.Max(0, changesLeft - 1),
                lastMachineId = machineId,
                lastMachineRegistered = DateTime.UtcNow.ToString("o"),
            },
            new[] { "machineChangesThisMonth", "lastMachineId", "lastMachineRegistered" });
    }

    private static DateTime FirstOfNextMonth()
    {
        var now = DateTime.UtcNow;
        return new DateTime(now.Year, now.Month, 1).AddMonths(1);
    }
}
