#nullable enable
using System;

namespace VedionScreenShare.Models
{
    public class AppConfig
    {
        // Screen capture
        public int CaptureIntervalMs { get; set; } = 2000;
        public int JpegQuality { get; set; } = 75;
        public CaptureArea? CaptureArea { get; set; }

        // Encryption
        public string EncryptionKey { get; set; } = "";

        // AI Provider
        public AiProviderType AiProvider { get; set; } = AiProviderType.VedionDiscord;
        public string AiApiKey { get; set; } = "";
        public string AiModel { get; set; } = "";
        public string AiEndpointOverride { get; set; } = ""; // for Ollama or custom

        // Discord — webhook for posting AI responses
        public string DiscordWebhookUrl { get; set; } = "";
        public bool PostResponsesToDiscord { get; set; } = true;
        public bool PostImagesToDiscord { get; set; } = false; // optionally also post the screenshot

        // Telegram
        public string TelegramBotToken { get; set; } = "";
        public string TelegramChatId { get; set; } = "";

        // System prompt sent with each frame
        public string SystemPrompt { get; set; } = "You are a helpful assistant. The user is sharing their screen with you. Describe what you see and help them with anything you notice.";

        // Behavior
        public bool AutoStart { get; set; } = false;
        public bool MinimizeToTray { get; set; } = true;
        public bool SendToAi { get; set; } = true;
        public bool SendToDiscord { get; set; } = false;
        public bool SendToTelegram { get; set; } = false;
    }

    public enum AiProviderType
    {
        VedionDiscord,   // Special: posts to Discord for Vedion to see
        Gemini,          // Google Gemini (free tier available)
        OpenAi,          // OpenAI GPT-4o
        Anthropic,       // Anthropic Claude
        Groq,            // Groq (fast, free tier)
        Ollama           // Local Ollama (100% free)
    }
}
