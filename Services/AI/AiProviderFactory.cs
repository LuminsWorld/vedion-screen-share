using System;
using VedionScreenShare.Models;

namespace VedionScreenShare.Services.AI
{
    public static class AiProviderFactory
    {
        public static IAiProvider Create(AppConfig config)
        {
            return config.AiProvider switch
            {
                AiProviderType.VedionDiscord => new DiscordRelayProvider(config.DiscordWebhookUrl),
                AiProviderType.Gemini        => new GeminiProvider(config.AiApiKey, string.IsNullOrWhiteSpace(config.AiModel) ? "gemini-1.5-flash" : config.AiModel),
                AiProviderType.OpenAi        => new OpenAiProvider("OpenAI", config.AiApiKey, string.IsNullOrWhiteSpace(config.AiModel) ? "gpt-4o" : config.AiModel),
                AiProviderType.Anthropic     => new AnthropicProvider(config.AiApiKey, string.IsNullOrWhiteSpace(config.AiModel) ? "claude-3-5-sonnet-20241022" : config.AiModel),
                AiProviderType.Groq          => new OpenAiProvider("Groq", config.AiApiKey, string.IsNullOrWhiteSpace(config.AiModel) ? "llama-3.2-90b-vision-preview" : config.AiModel, "https://api.groq.com/openai/v1"),
                AiProviderType.Ollama        => new OpenAiProvider("Ollama (Local)", "", string.IsNullOrWhiteSpace(config.AiModel) ? "llava" : config.AiModel, string.IsNullOrWhiteSpace(config.AiEndpointOverride) ? "http://localhost:11434/v1" : config.AiEndpointOverride),
                _                            => throw new ArgumentException($"Unknown AI provider: {config.AiProvider}")
            };
        }
    }
}
