using System.Windows;
using VedionScreenShare.Services;

namespace VedionScreenShare
{
    public partial class LicenseWindow : Window
    {
        public bool IsActivated { get; private set; }

        public LicenseWindow()
        {
            InitializeComponent();

            // Pre-fill saved key
            string saved = LicenseService.LoadKey();
            if (!string.IsNullOrWhiteSpace(saved))
                LicenseKeyInput.Text = saved;
        }

        private async void ActivateButton_Click(object sender, RoutedEventArgs e)
        {
            ActivateButton.IsEnabled = false;
            StatusLabel.Foreground = System.Windows.Media.Brushes.Gray;
            StatusLabel.Text = "Validating...";

            var (valid, message) = await LicenseService.ValidateAsync(LicenseKeyInput.Text.Trim());

            if (valid)
            {
                LicenseService.SaveKey(LicenseKeyInput.Text.Trim());
                StatusLabel.Text = "✓ Activated successfully!";
                StatusLabel.Foreground = System.Windows.Media.Brushes.Green;
                IsActivated = true;

                await System.Threading.Tasks.Task.Delay(800);
                DialogResult = true;
                Close();
            }
            else
            {
                StatusLabel.Text = $"✗ {message}";
                StatusLabel.Foreground = System.Windows.Media.Brushes.Red;
                ActivateButton.IsEnabled = true;
            }
        }

        private void BuyButton_Click(object sender, RoutedEventArgs e)
        {
            // Open your store page
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "https://your-store.lemonsqueezy.com",
                UseShellExecute = true
            });
        }
    }
}
