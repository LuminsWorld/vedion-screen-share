using System.Windows;

namespace VedionScreenShare
{
    public partial class App : Application
    {
        private TrayApplication _trayApp;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Show setup wizard
            var setupWindow = new SetupWindow();
            if (setupWindow.ShowDialog() == true)
            {
                // Start tray application with config
                _trayApp = new TrayApplication(setupWindow.Config);
                _trayApp.Start();
            }
            else
            {
                // User cancelled setup
                Shutdown();
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _trayApp?.Stop();
            base.OnExit(e);
        }
    }
}
