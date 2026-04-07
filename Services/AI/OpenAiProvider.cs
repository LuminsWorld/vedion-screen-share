using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace VedionScreenShare.Services.AI
{
    /// <summary>
    /// OpenAI-compatible provider — works with OpenAI, Groq, and Ollama (local)
    /// </summary>
    public class OpenAiProvider : IAiProvider
    {
        private readonly string _apiKey;
        private readonly string _model;
        private readonly string _endpoint;
        private static readonly HttpClient _http = new HttpClient();

        public string Name { get; }
        public bool IsConfigured => !string.IsNullOrWhiteSpace(_apiKey) || _endpoint.Contains("localhost");

        public OpenAiProvider(string name, string apiKey, string model, string endpoint = "https://api.openai.com/v1")
        {
            Name = name;
            _apiKey = apiKey;
            _model = model;
            _endpoint = endpoint.TrimEnd('/');
        }

        public async Task<string> AnalyzeFrameAsync(byte[] jpegFrame, string systemPrompt)
        {
            string base64Image = Convert.ToBase64String(jpegFrame);

            var body = new
            {
                model = _model,
                messages = new[]
                {
                    new
                    {
                        role = "user",
                        content = new object[]
                        {
                            new { type = "text", text = systemPrompt },
                            new
                            {
                                type = "image_url",
                                image_url = new
                                {
                                    url = $"data:image/jpeg;base64,{base64Image}"
                                }
                            }
                        }
                    }
                },
                max_tokens = 1024
            };

            string url = $"{_endpoint}/chat/completions";
            string json = JsonSerializer.Serialize(body);

            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            if (!string.IsNullOrWhiteSpace(_apiKey))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

            var response = await _http.SendAsync(request);
            string responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"{Name} error {response.StatusCode}: {responseBody}");

            using var doc = JsonDocument.Parse(responseBody);
            return doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? "(no response)";
        }
    }
}
