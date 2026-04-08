using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using VedionScreenShare.Models;

namespace VedionScreenShare.Services;

public static class AiService
{
    private static readonly HttpClient _http = new();

    /// <summary>Sends an image to an AI provider and returns the text response.</summary>
    public static async Task<string> AnalyzeAsync(
        string endpoint, string apiKey, string model,
        AiFormat format, string prompt, byte[] imageBytes)
    {
        return format switch
        {
            AiFormat.Gemini     => await CallGeminiAsync(endpoint, apiKey, model, prompt, imageBytes),
            AiFormat.Anthropic  => await CallAnthropicAsync(endpoint, apiKey, model, prompt, imageBytes),
            _                   => await CallOpenAiAsync(endpoint, apiKey, model, prompt, imageBytes),
        };
    }

    // ── OpenAI format (also Groq, Ollama, custom) ─────────────────────────

    private static async Task<string> CallOpenAiAsync(
        string endpoint, string apiKey, string model, string prompt, byte[] imageBytes)
    {
        string base64 = Convert.ToBase64String(imageBytes);
        var body = new
        {
            model,
            messages = new object[]
            {
                new
                {
                    role    = "user",
                    content = new object[]
                    {
                        new { type = "image_url", image_url = new { url = $"data:image/jpeg;base64,{base64}" } },
                        new { type = "text", text = prompt }
                    }
                }
            },
            max_tokens = 1024,
        };

        var req = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var resp = await _http.SendAsync(req);
        var json = await resp.Content.ReadAsStringAsync();

        if (!resp.IsSuccessStatusCode)
            throw new Exception($"AI error ({resp.StatusCode}): {json}");

        var doc = JsonDocument.Parse(json);
        return doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? "(no response)";
    }

    // ── Gemini ────────────────────────────────────────────────────────────

    private static async Task<string> CallGeminiAsync(
        string endpointTemplate, string apiKey, string model, string prompt, byte[] imageBytes)
    {
        string base64 = Convert.ToBase64String(imageBytes);

        // Fill template: replace {model} and {key}
        string endpoint = endpointTemplate
            .Replace("{model}", model)
            .Replace("{key}", apiKey);

        var body = new
        {
            contents = new object[]
            {
                new
                {
                    parts = new object[]
                    {
                        new { inline_data = new { mime_type = "image/jpeg", data = base64 } },
                        new { text = prompt }
                    }
                }
            }
        };

        var req = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
        };

        var resp = await _http.SendAsync(req);
        var json = await resp.Content.ReadAsStringAsync();

        if (!resp.IsSuccessStatusCode)
            throw new Exception($"Gemini error ({resp.StatusCode}): {json}");

        var doc = JsonDocument.Parse(json);
        return doc.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString() ?? "(no response)";
    }

    // ── Anthropic ─────────────────────────────────────────────────────────

    private static async Task<string> CallAnthropicAsync(
        string endpoint, string apiKey, string model, string prompt, byte[] imageBytes)
    {
        string base64 = Convert.ToBase64String(imageBytes);
        var body = new
        {
            model,
            max_tokens = 1024,
            messages = new object[]
            {
                new
                {
                    role    = "user",
                    content = new object[]
                    {
                        new { type = "image", source = new { type = "base64", media_type = "image/jpeg", data = base64 } },
                        new { type = "text", text = prompt }
                    }
                }
            }
        };

        var req = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
        };
        req.Headers.Add("x-api-key", apiKey);
        req.Headers.Add("anthropic-version", "2023-06-01");

        var resp = await _http.SendAsync(req);
        var json = await resp.Content.ReadAsStringAsync();

        if (!resp.IsSuccessStatusCode)
            throw new Exception($"Anthropic error ({resp.StatusCode}): {json}");

        var doc = JsonDocument.Parse(json);
        return doc.RootElement
            .GetProperty("content")[0]
            .GetProperty("text")
            .GetString() ?? "(no response)";
    }
}
