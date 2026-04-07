using System;
using System.Windows;
using System.Windows.Threading;

namespace VedionScreenShare
{
    public partial class App : Application
    {
        private TrayApplication _trayApp;

        protected override void OnStartup(StartupEventArgs e)
        {
            // Catch any unhandled exceptions and show them
            DispatcherUnhandledException += (s, ex) =>
            {
                MessageBox.Show($"Unhandled error:\n\n{ex.Exception.Message}\n\n{ex.Exception.StackTrace}",
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
                var setup = new SetupWindow();
                bool? result = setup.ShowDialog();

                if (result == true && setup.IsConfigured)
                {
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
