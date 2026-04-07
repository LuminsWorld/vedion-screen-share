using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace VedionScreenShare.Services.AI
{
    /// <summary>
    /// Vedion Discord Relay — posts frames to a Discord channel for Vedion to see and respond.
    /// Free, no AI API key needed. Just a Discord webhook URL.
    /// </summary>
    public class DiscordRelayProvider : IAiProvider
    {
        private readonly string _webhookUrl;
        private static readonly HttpClient _http = new HttpClient();

        public string Name => "Vedion (Discord)";
        public bool IsConfigured => !string.IsNullOrWhiteSpace(_webhookUrl);

        public DiscordRelayProvider(string webhookUrl)
        {
            _webhookUrl = webhookUrl;
        }

        public async Task<string> AnalyzeFrameAsync(byte[] jpegFrame, string systemPrompt)
        {
            // Post the frame as an image to Discord via webhook
            using var form = new MultipartFormDataContent();

            var imageContent = new ByteArrayContent(jpegFrame);
            imageContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
            form.Add(imageContent, "file", $"screen-{DateTime.UtcNow:HH-mm-ss}.jpg");

            var payload = new { content = "📸 Screen update" };
            var payloadJson = JsonSerializer.Serialize(payload);
            form.Add(new StringContent(payloadJson, Encoding.UTF8, "application/json"), "payload_json");

            var response = await _http.PostAsync(_webhookUrl, form);

            if (!response.IsSuccessStatusCode)
            {
                string error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Discord relay error {response.StatusCode}: {error}");
            }

            // Discord relay is one-way — responses come back in Discord
            return "Frame sent to Discord ✓";
        }
    }
}
