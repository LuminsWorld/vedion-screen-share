using System.Net.Http;
using System.Net.Http.Headers;

namespace VedionScreenShare.Services;

public static class DiscordService
{
    private static readonly HttpClient _http = new();

    /// <summary>Posts a message (+ optional image) to a Discord webhook.</summary>
    public static async Task PostAsync(string webhookUrl, string message, byte[]? imageBytes = null, string filename = "screenshot.jpg")
    {
        if (string.IsNullOrWhiteSpace(webhookUrl)) return;

        if (imageBytes is null)
        {
            // Text only
            var body = System.Text.Json.JsonSerializer.Serialize(new { content = message });
            await _http.PostAsync(webhookUrl,
                new StringContent(body, System.Text.Encoding.UTF8, "application/json"));
        }
        else
        {
            // Multipart: image + text
            using var form = new MultipartFormDataContent();

            var imgContent = new ByteArrayContent(imageBytes);
            imgContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
            form.Add(imgContent, "file", filename);

            if (!string.IsNullOrWhiteSpace(message))
            {
                var payload = System.Text.Json.JsonSerializer.Serialize(new { content = message });
                form.Add(new StringContent(payload, System.Text.Encoding.UTF8, "application/json"), "payload_json");
            }

            await _http.PostAsync(webhookUrl, form);
        }
    }

    /// <summary>Validates a webhook URL by sending a GET request.</summary>
    public static async Task<bool> ValidateWebhookAsync(string webhookUrl)
    {
        try
        {
            var resp = await _http.GetAsync(webhookUrl);
            return resp.IsSuccessStatusCode;
        }
        catch { return false; }
    }
}
