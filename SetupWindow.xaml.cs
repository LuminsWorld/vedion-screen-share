using System;
using System.Windows;
using VedionScreenShare.Models;
using VedionScreenShare.Services;

namespace VedionScreenShare
{
    public partial class SetupWindow : Window
    {
        public CaptureConfig Config { get; private set; }
        public bool IsConfigured { get; private set; }

        public SetupWindow()
        {
            InitializeComponent();
            IntervalSlider.ValueChanged += (s, e) => IntervalLabel.Text = $"{(int)IntervalSlider.Value}ms";
            QualitySlider.ValueChanged += (s, e) => QualityLabel.Text = $"{(int)QualitySlider.Value}%";
        }

        private void GenerateKeyButton_Click(object sender, RoutedEventArgs e)
        {
            KeyInput.Text = EncryptionService.GenerateKey();
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(EndpointInput.Text))
            {
                MessageBox.Show("Endpoint URL is required", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(KeyInput.Text))
            {
                MessageBox.Show("Encryption key is required. Click 'Generate New' to create one.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Validate encryption key is valid Base64 and correct length
                EncryptionService testService = new EncryptionService(KeyInput.Text);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Invalid encryption key: {ex.Message}", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Create config
            Config = new CaptureConfig
            {
                EndpointUrl = EndpointInput.Text,
                EncryptionKey = KeyInput.Text,
                CaptureIntervalMs = (int)IntervalSlider.Value,
                JpegQuality = (int)QualitySlider.Value,
                AutoStart = true,
                MinimizeToTray = true
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
