using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace VedionScreenShare.Services.AI
{
    /// <summary>
    /// Anthropic Claude API
    /// </summary>
    public class AnthropicProvider : IAiProvider
    {
        private readonly string _apiKey;
        private readonly string _model;
        private static readonly HttpClient _http = new HttpClient();

        public string Name => "Anthropic Claude";
        public bool IsConfigured => !string.IsNullOrWhiteSpace(_apiKey);

        public AnthropicProvider(string apiKey, string model = "claude-3-5-sonnet-20241022")
        {
            _apiKey = apiKey;
            _model = model;
        }

        public async Task<string> AnalyzeFrameAsync(byte[] jpegFrame, string systemPrompt)
        {
            string base64Image = Convert.ToBase64String(jpegFrame);

            var body = new
            {
                model = _model,
                max_tokens = 1024,
                system = systemPrompt,
                messages = new[]
                {
                    new
                    {
                        role = "user",
                        content = new object[]
                        {
                            new
                            {
                                type = "image",
                                source = new
                                {
                                    type = "base64",
                                    media_type = "image/jpeg",
                                    data = base64Image
                                }
                            },
                            new { type = "text", text = "What do you see on my screen? Help me with anything you notice." }
                        }
                    }
                }
            };

            string json = JsonSerializer.Serialize(body);

            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages");
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            request.Headers.Add("x-api-key", _apiKey);
            request.Headers.Add("anthropic-version", "2023-06-01");

            var response = await _http.SendAsync(request);
            string responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Anthropic error {response.StatusCode}: {responseBody}");

            using var doc = JsonDocument.Parse(responseBody);
            return doc.RootElement
                .GetProperty("content")[0]
                .GetProperty("text")
                .GetString() ?? "(no response)";
        }
    }
}
