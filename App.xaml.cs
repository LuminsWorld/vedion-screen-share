using System.Windows;

namespace VedionScreenShare
{
    public partial class App : Application
    {
        private TrayApplication _trayApp;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var setup = new SetupWindow();
            if (setup.ShowDialog() == true)
            {
                _trayApp = new TrayApplication(setup.Config);
                _trayApp.Start();
            }
            else
            {
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
