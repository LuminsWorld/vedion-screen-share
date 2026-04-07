using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace VedionScreenShare.Services
{
    public class DiscordService
    {
        private readonly string _webhookUrl;
        private static readonly HttpClient _http = new HttpClient();

        public bool IsConfigured => !string.IsNullOrWhiteSpace(_webhookUrl)
                                    && _webhookUrl.StartsWith("https://discord.com/api/webhooks/");

        public DiscordService(string webhookUrl)
        {
            _webhookUrl = webhookUrl;
        }

        /// <summary>
        /// Post a text message to the Discord channel via webhook
        /// </summary>
        public async Task PostTextAsync(string message)
        {
            if (!IsConfigured) return;

            // Discord has a 2000 char limit per message
            if (message.Length > 1990)
                message = message[..1990] + "…";

            var payload = new { content = message };
            string json = JsonSerializer.Serialize(payload);

            var response = await _http.PostAsync(_webhookUrl,
                new StringContent(json, Encoding.UTF8, "application/json"));

            if (!response.IsSuccessStatusCode)
            {
                string error = await response.Content.ReadAsStringAsync();
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    throw new Exception("Discord webhook URL is invalid (404). Open Settings and fix the webhook URL.");
                throw new Exception($"Discord error {response.StatusCode}: {error}");
            }
        }

        /// <summary>
        /// Post a screenshot image to the Discord channel via webhook
        /// </summary>
        public async Task PostImageAsync(byte[] jpegBytes, string caption = "")
        {
            if (!IsConfigured) return;

            using var form = new MultipartFormDataContent();

            var imageContent = new ByteArrayContent(jpegBytes);
            imageContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
            form.Add(imageContent, "file", $"screen-{DateTime.UtcNow:HH-mm-ss}.jpg");

            if (!string.IsNullOrWhiteSpace(caption))
            {
                var payload = new { content = caption };
                form.Add(new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"), "payload_json");
            }

            var response = await _http.PostAsync(_webhookUrl, form);

            if (!response.IsSuccessStatusCode)
            {
                string error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Discord image post error {response.StatusCode}: {error}");
            }
        }

        /// <summary>
        /// Post AI response with optional timestamp header
        /// </summary>
        public async Task PostAiResponseAsync(string aiName, string response)
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            string message = $"**[{timestamp}] {aiName}:**\n{response}";
            await PostTextAsync(message);
        }
    }
}
