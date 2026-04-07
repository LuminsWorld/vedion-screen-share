using System;
using System.Windows;
using System.Windows.Controls;
using VedionScreenShare.Models;
using VedionScreenShare.Services;

namespace VedionScreenShare
{
    public partial class SetupWindow : Window
    {
        public AppConfig Config { get; private set; }
        public bool IsConfigured { get; private set; }

        public SetupWindow()
        {
            InitializeComponent();
            IntervalSlider.ValueChanged += (s, e) => IntervalLabel.Text = $"{(int)IntervalSlider.Value}ms";
            QualitySlider.ValueChanged  += (s, e) => QualityLabel.Text  = $"{(int)QualitySlider.Value}%";
        }

        private void AiProviderCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (VedionPanel == null) return; // Not initialized yet — ignore
            if (AiProviderCombo.SelectedItem is not ComboBoxItem item) return;

            string tag = item.Tag?.ToString() ?? "";

            VedionPanel.Visibility  = tag == "VedionDiscord" ? Visibility.Visible : Visibility.Collapsed;
            ApiKeyPanel.Visibility  = tag is "Gemini" or "Anthropic" or "OpenAi" or "Groq" ? Visibility.Visible : Visibility.Collapsed;
            OllamaPanel.Visibility  = tag == "Ollama" ? Visibility.Visible : Visibility.Collapsed;

            // Update hints per provider
            switch (tag)
            {
                case "Gemini":
                    ApiKeyLabel.Text = "Gemini API Key:";
                    ApiKeyHint.Text  = "Get free key at: aistudio.google.com → Get API key";
                    break;
                case "Anthropic":
                    ApiKeyLabel.Text = "Anthropic API Key:";
                    ApiKeyHint.Text  = "Get key at: console.anthropic.com";
                    break;
                case "OpenAi":
                    ApiKeyLabel.Text = "OpenAI API Key:";
                    ApiKeyHint.Text  = "Get key at: platform.openai.com/api-keys";
                    break;
                case "Groq":
                    ApiKeyLabel.Text = "Groq API Key:";
                    ApiKeyHint.Text  = "Get free key at: console.groq.com — Fast and free tier available";
                    break;
            }
        }

        private void GenerateKeyButton_Click(object sender, RoutedEventArgs e)
        {
            KeyInput.Text = EncryptionService.GenerateKey();
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = AiProviderCombo.SelectedItem as ComboBoxItem;
            string providerTag = selectedItem?.Tag?.ToString() ?? "VedionDiscord";

            // Validate
            if (providerTag == "VedionDiscord" && string.IsNullOrWhiteSpace(DiscordWebhookInput.Text))
            {
                MessageBox.Show("Please enter a Discord Webhook URL.", "Missing Field", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (providerTag is "Gemini" or "Anthropic" or "OpenAi" or "Groq" && string.IsNullOrWhiteSpace(ApiKeyInput.Text))
            {
                MessageBox.Show("Please enter your API key.", "Missing Field", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(KeyInput.Text))
            {
                MessageBox.Show("Please generate an encryption key.", "Missing Field", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Enum.TryParse<AiProviderType>(providerTag, out var provider);

            Config = new AppConfig
            {
                AiProvider            = provider,
                AiApiKey              = ApiKeyInput.Text.Trim(),
                AiModel               = ModelInput.Text.Trim(),
                AiEndpointOverride    = OllamaEndpointInput.Text.Trim(),
                DiscordWebhookUrl     = DiscordWebhookInput.Text.Trim(),
                SystemPrompt          = SystemPromptInput.Text.Trim(),
                EncryptionKey         = KeyInput.Text.Trim(),
                CaptureIntervalMs     = (int)IntervalSlider.Value,
                JpegQuality           = (int)QualitySlider.Value,
                AutoStart             = AutoStartCheck.IsChecked == true,
                MinimizeToTray        = MinimizeCheck.IsChecked == true,
                SendToAi              = true
            };

            IsConfigured = true;
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            IsConfigured = false;
            DialogResult = false;
            Close();
        }
    }
}
