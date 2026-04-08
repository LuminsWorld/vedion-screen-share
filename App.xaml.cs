using System.Windows;
using VedionScreenShare.Services;
using VedionScreenShare.Services.Auth;
using VedionScreenShare.Windows;

namespace VedionScreenShare;

public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += (_, ex) =>
        {
            System.Windows.MessageBox.Show(ex.Exception.Message, "Unexpected Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
            ex.Handled = true;
        };

        // ── Check for updates ──────────────────────────────────────────────
        _ = Task.Run(async () =>
        {
            var update = await UpdateService.CheckForUpdateAsync();
            if (update is not null)
            {
                Dispatcher.Invoke(() =>
                {
                    var result = System.Windows.MessageBox.Show(
                        $"Update available: {update.Version}\n\nDownload and install now?",
                        "Vedion Screen Share — Update",
                        MessageBoxButton.YesNo, MessageBoxImage.Information);

                    if (result == MessageBoxResult.Yes && !string.IsNullOrWhiteSpace(update.DownloadUrl))
                        _ = UpdateService.DownloadAndInstallAsync(update.DownloadUrl);
                });
            }
        });

        // ── Load config ───────────────────────────────────────────────────
        var config = ConfigService.Load();

        // ── Try auto-login via saved refresh token ─────────────────────────
        FirebaseUser? user = null;
        if (!string.IsNullOrWhiteSpace(config.RefreshToken))
        {
            try
            {
                user = await FirebaseAuthService.RefreshTokenAsync(config.RefreshToken);
                config.IdToken      = user.IdToken;
                config.RefreshToken = user.RefreshToken;
                config.Uid          = user.Uid;
                config.Email        = user.Email;
                config.DisplayName  = user.DisplayName;
                ConfigService.Save(config);
            }
            catch { /* Token expired — fall through to login */ }
        }

        // ── Show login if no valid session ────────────────────────────────
        if (user is null)
        {
            var login = new LoginWindow();
            if (login.ShowDialog() != true || login.LoggedInUser is null)
            {
                Shutdown();
                return;
            }
            user = login.LoggedInUser;

            // Save tokens
            config.IdToken      = user.IdToken;
            config.RefreshToken = user.RefreshToken;
            config.Uid          = user.Uid;
            config.Email        = user.Email;
            config.DisplayName  = user.DisplayName;
            ConfigService.Save(config);
        }

        // ── Validate license ───────────────────────────────────────────────
        var licenseResult = await LicenseService.ValidateAsync(user.IdToken, user.Uid);
        if (licenseResult.Status != LicenseStatus.Valid)
        {
            var msg = licenseResult.Status == LicenseStatus.NoLicense
                ? $"{licenseResult.Message}\n\nOpen vedion.cloud/shop to purchase?"
                : licenseResult.Message;

            var btn = licenseResult.Status == LicenseStatus.NoLicense
                ? MessageBoxButton.YesNo : MessageBoxButton.OK;

            var result = System.Windows.MessageBox.Show(msg, "License Required", btn, MessageBoxImage.Warning);

            if (licenseResult.Status == LicenseStatus.NoLicense && result == MessageBoxResult.Yes)
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(
                    "https://vedion.cloud/shop") { UseShellExecute = true });

            Shutdown();
            return;
        }

        // ── Launch main window ────────────────────────────────────────────
        var main = new MainWindow(user, config);
        main.Show();
    }
}
