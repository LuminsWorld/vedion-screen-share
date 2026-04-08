using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;

namespace VedionScreenShare.Services;

/// <summary>Thin Firestore REST client — reads/writes documents.</summary>
public class FirestoreService
{
    private static readonly HttpClient _http = new();

    // ── Read ──────────────────────────────────────────────────────────────

    public static async Task<JsonElement?> GetDocumentAsync(string idToken, string path)
    {
        var req = new HttpRequestMessage(HttpMethod.Get, $"{Constants.FirestoreBase}/{path}");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", idToken);

        var resp = await _http.SendAsync(req);
        if (resp.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        resp.EnsureSuccessStatusCode();

        var json = await resp.Content.ReadAsStringAsync();
        return JsonDocument.Parse(json).RootElement;
    }

    // ── Write (patch/merge) ───────────────────────────────────────────────

    public static async Task PatchDocumentAsync(string idToken, string path, object data, string[]? updateMask = null)
    {
        string json    = ToFirestoreJson(data);
        string url     = $"{Constants.FirestoreBase}/{path}";
        if (updateMask is { Length: > 0 })
            url += "?" + string.Join("&", updateMask.Select(f => $"updateMask.fieldPaths={Uri.EscapeDataString(f)}"));

        var req = new HttpRequestMessage(new HttpMethod("PATCH"), url)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", idToken);
        (await _http.SendAsync(req)).EnsureSuccessStatusCode();
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    /// <summary>Reads a string field from a Firestore document element.</summary>
    public static string? GetString(JsonElement doc, string field)
    {
        if (!doc.TryGetProperty("fields", out var fields)) return null;
        if (!fields.TryGetProperty(field, out var f)) return null;
        return f.TryGetProperty("stringValue", out var v) ? v.GetString() : null;
    }

    public static bool? GetBool(JsonElement doc, string field)
    {
        if (!doc.TryGetProperty("fields", out var fields)) return null;
        if (!fields.TryGetProperty(field, out var f)) return null;
        return f.TryGetProperty("booleanValue", out var v) ? v.GetBoolean() : null;
    }

    public static int? GetInt(JsonElement doc, string field)
    {
        if (!doc.TryGetProperty("fields", out var fields)) return null;
        if (!fields.TryGetProperty(field, out var f)) return null;
        if (f.TryGetProperty("integerValue", out var v)) return int.TryParse(v.GetString(), out var i) ? i : null;
        return null;
    }

    public static JsonElement[]? GetArray(JsonElement doc, string field)
    {
        if (!doc.TryGetProperty("fields", out var fields)) return null;
        if (!fields.TryGetProperty(field, out var f)) return null;
        if (!f.TryGetProperty("arrayValue", out var arr)) return null;
        if (!arr.TryGetProperty("values", out var vals)) return Array.Empty<JsonElement>();
        return vals.EnumerateArray().ToArray();
    }

    /// <summary>Converts a plain C# anonymous object into Firestore JSON format.</summary>
    private static string ToFirestoreJson(object data)
    {
        var fields = new Dictionary<string, object>();
        foreach (var prop in data.GetType().GetProperties())
        {
            var val = prop.GetValue(data);
            fields[prop.Name] = val switch
            {
                string s  => new { stringValue  = s },
                bool   b  => new { booleanValue = b },
                int    i  => new { integerValue = i.ToString() },
                long   l  => new { integerValue = l.ToString() },
                null      => new { nullValue     = "NULL_VALUE" },
                _         => new { stringValue  = val.ToString() ?? "" },
            };
        }
        return JsonSerializer.Serialize(new { fields });
    }
}
