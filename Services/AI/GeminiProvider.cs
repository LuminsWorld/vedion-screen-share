using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace VedionScreenShare.Services.AI
{
    /// <summary>
    /// Google Gemini API — free tier available at aistudio.google.com
    /// </summary>
    public class GeminiProvider : IAiProvider
    {
        private readonly string _apiKey;
        private readonly string _model;
        private static readonly HttpClient _http = new HttpClient();

        public string Name => "Google Gemini";
        public bool IsConfigured => !string.IsNullOrWhiteSpace(_apiKey);

        public GeminiProvider(string apiKey, string model = "gemini-1.5-flash")
        {
            _apiKey = apiKey;
            _model = model;
        }

        public async Task<string> AnalyzeFrameAsync(byte[] jpegFrame, string systemPrompt)
        {
            string base64Image = Convert.ToBase64String(jpegFrame);

            var body = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new object[]
                        {
                            new { text = systemPrompt },
                            new
                            {
                                inline_data = new
                                {
                                    mime_type = "image/jpeg",
                                    data = base64Image
                                }
                            }
                        }
                    }
                }
            };

            string url = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent?key={_apiKey}";
            string json = JsonSerializer.Serialize(body);

            var response = await _http.PostAsync(url,
                new StringContent(json, Encoding.UTF8, "application/json"));

            string responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Gemini error {response.StatusCode}: {responseBody}");

            using var doc = JsonDocument.Parse(responseBody);
            return doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString() ?? "(no response)";
        }
    }
}
