using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using VedionScreenShare.Services;

namespace VedionScreenShare
{
    public partial class App : Application
    {
        private TrayApplication _trayApp;

        protected override async void OnStartup(StartupEventArgs e)
        {
            DispatcherUnhandledException += (s, ex) =>
            {
                MessageBox.Show($"Error:\n\n{ex.Exception.Message}\n\n{ex.Exception.StackTrace}",
                    "Vedion Error", MessageBoxButton.OK, MessageBoxImage.Error);
                ex.Handled = true;
            };

            AppDomain.CurrentDomain.UnhandledException += (s, ex) =>
            {
                MessageBox.Show($"Fatal error:\n\n{ex.ExceptionObject}",
                    "Vedion Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);
            };

            base.OnStartup(e);

            try
            {
                // Check license first
                string savedKey = LicenseService.LoadKey();
                var (licensed, _) = await LicenseService.ValidateAsync(savedKey);

                if (!licensed)
                {
                    var licenseWindow = new LicenseWindow();
                    if (licenseWindow.ShowDialog() != true)
                    {
                        Shutdown();
                        return;
                    }
                }

                // Load saved config if it exists
                var savedConfig = ConfigService.Exists() ? ConfigService.Load() : null;

                var setup = new SetupWindow(savedConfig);
                bool? result = setup.ShowDialog();

                if (result == true && setup.IsConfigured)
                {
                    // Save config for next time
                    ConfigService.Save(setup.Config);

                    _trayApp = new TrayApplication(setup.Config);
                    _trayApp.Start();
                }
                else
                {
                    Shutdown();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Startup error:\n\n{ex.Message}\n\n{ex.StackTrace}",
                    "Vedion Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _trayApp?.Dispose();
            base.OnExit(e);
        }
    }
}
