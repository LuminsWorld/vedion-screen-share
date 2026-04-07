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

        private CaptureArea _selectedRegion = null;

        public SetupWindow(AppConfig existing = null)
        {
            InitializeComponent();

            IntervalSlider.ValueChanged += (s, e) =>
            {
                double ms = IntervalSlider.Value;
                IntervalLabel.Text = ms >= 1000 ? $"{ms / 1000:0.#}s" : $"{(int)ms}ms";
            };
            QualitySlider.ValueChanged += (s, e) => QualityLabel.Text = $"{(int)QualitySlider.Value}%";

            // Pre-fill from saved config if provided
            if (existing != null)
                Loaded += (s, e) => PopulateFrom(existing);
        }

        private void PopulateFrom(AppConfig c)
        {
            // AI provider
            foreach (ComboBoxItem item in AiProviderCombo.Items)
                if (item.Tag?.ToString() == c.AiProvider.ToString())
                { AiProviderCombo.SelectedItem = item; break; }

            ApiKeyInput.Text           = c.AiApiKey;
            ModelInput.Text            = c.AiModel;
            OllamaEndpointInput.Text   = c.AiEndpointOverride;
            DiscordWebhookInput.Text   = c.DiscordWebhookUrl;
            PostImagesCheck.IsChecked  = c.PostImagesToDiscord;
            SystemPromptInput.Text     = c.SystemPrompt;

            // Capture
            KeyInput.Text              = c.EncryptionKey;
            IntervalSlider.Value       = c.CaptureIntervalMs;
            QualitySlider.Value        = c.JpegQuality;
            AutoStartCheck.IsChecked   = c.AutoStart;
            MinimizeCheck.IsChecked    = c.MinimizeToTray;

            // Mode
            ContinuousModeRadio.IsChecked = c.CaptureMode == CaptureMode.Continuous;
            SnapshotModeRadio.IsChecked   = c.CaptureMode == CaptureMode.Snapshot;

            // Region
            if (c.CaptureArea != null)
            {
                _selectedRegion = c.CaptureArea;
                CustomRegionRadio.IsChecked = true;
                RegionLabel.Text = $"Selected: {c.CaptureArea.Width} × {c.CaptureArea.Height} at ({c.CaptureArea.X}, {c.CaptureArea.Y})";
                RegionLabel.Foreground = System.Windows.Media.Brushes.Green;
            }
        }

        private void AiProviderCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (VedionPanel == null) return; // Not initialized yet
            if (AiProviderCombo.SelectedItem is not ComboBoxItem item) return;

            string tag = item.Tag?.ToString() ?? "";

            VedionPanel.Visibility = tag == "VedionDiscord" ? Visibility.Visible : Visibility.Collapsed;
            ApiKeyPanel.Visibility = tag is "Gemini" or "Anthropic" or "OpenAi" or "Groq" ? Visibility.Visible : Visibility.Collapsed;
            OllamaPanel.Visibility = tag == "Ollama" ? Visibility.Visible : Visibility.Collapsed;

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
                    ApiKeyHint.Text  = "Get free key at: console.groq.com";
                    break;
            }
        }

        private void GenerateKeyButton_Click(object sender, RoutedEventArgs e)
        {
            KeyInput.Text = EncryptionService.GenerateKey();
        }

        private void FullScreenRadio_Checked(object sender, RoutedEventArgs e)
        {
            if (RegionPanel != null)
                RegionPanel.Visibility = Visibility.Collapsed;
            _selectedRegion = null;
        }

        private void CustomRegionRadio_Checked(object sender, RoutedEventArgs e)
        {
            if (RegionPanel != null)
                RegionPanel.Visibility = Visibility.Visible;
        }

        private void PickRegionButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;

            var selector = new RegionSelectorWindow();
            selector.Loaded += (_, __) => selector.Focus();
            selector.ShowDialog();

            this.WindowState = WindowState.Normal;
            this.Activate();

            if (selector.WasConfirmed && selector.SelectedRegion != null)
            {
                _selectedRegion = selector.SelectedRegion;
                RegionLabel.Text = $"Selected: {_selectedRegion.Width} × {_selectedRegion.Height} at ({_selectedRegion.X}, {_selectedRegion.Y})";
                RegionLabel.Foreground = System.Windows.Media.Brushes.Green;
            }
            else
            {
                RegionLabel.Text = "Cancelled — no region selected";
                RegionLabel.Foreground = System.Windows.Media.Brushes.Gray;
            }
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = AiProviderCombo.SelectedItem as ComboBoxItem;
            string providerTag = selectedItem?.Tag?.ToString() ?? "VedionDiscord";

            if (string.IsNullOrWhiteSpace(DiscordWebhookInput.Text) ||
                DiscordWebhookInput.Text == "https://discord.com/api/webhooks/...")
            {
                MessageBox.Show("Please enter a Discord Webhook URL for AI responses.", "Missing Field", MessageBoxButton.OK, MessageBoxImage.Warning);
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

            if (CustomRegionRadio.IsChecked == true && _selectedRegion == null)
            {
                MessageBox.Show("Please draw a capture region first.", "Missing Field", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Enum.TryParse<AiProviderType>(providerTag, out var provider);

            Config = new AppConfig
            {
                AiProvider              = provider,
                AiApiKey                = ApiKeyInput.Text.Trim(),
                AiModel                 = ModelInput.Text.Trim(),
                AiEndpointOverride      = OllamaEndpointInput.Text.Trim(),
                DiscordWebhookUrl       = DiscordWebhookInput.Text.Trim(),
                PostResponsesToDiscord  = true,
                PostImagesToDiscord     = PostImagesCheck.IsChecked == true,
                SystemPrompt            = SystemPromptInput.Text.Trim(),
                EncryptionKey           = KeyInput.Text.Trim(),
                CaptureIntervalMs       = (int)IntervalSlider.Value,
                JpegQuality             = (int)QualitySlider.Value,
                CaptureArea             = _selectedRegion,
                CaptureMode             = SnapshotModeRadio.IsChecked == true ? CaptureMode.Snapshot : CaptureMode.Continuous,
                AutoStart               = AutoStartCheck.IsChecked == true,
                MinimizeToTray          = MinimizeCheck.IsChecked == true,
                SendToAi                = true
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
