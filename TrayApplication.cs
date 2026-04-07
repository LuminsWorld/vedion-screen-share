using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using VedionScreenShare.Models;
using VedionScreenShare.Services;
using VedionScreenShare.Services.AI;

namespace VedionScreenShare
{
    public class TrayApplication : IDisposable
    {
        private readonly AppConfig _config;
        private NotifyIcon _trayIcon;
        private bool _isRunning = false;
        private bool _isPaused = false;
        private CancellationTokenSource _cts;

        private ScreenCaptureService _captureService;
        private EncryptionService _encryptionService;
        private IAiProvider _aiProvider;

        private ToolStripMenuItem _statusItem;
        private ToolStripMenuItem _pauseItem;
        private ToolStripMenuItem _lastResponseItem;

        public TrayApplication(AppConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public async void Start()
        {
            try
            {
                _isRunning = true;
                _cts = new CancellationTokenSource();

                _captureService    = new ScreenCaptureService(_config.JpegQuality);
                _encryptionService = new EncryptionService(_config.EncryptionKey);
                _aiProvider        = AiProviderFactory.Create(_config);

                SetupTrayIcon();
                _ = CaptureLoopAsync();
            }
            catch (Exception ex)
            {
                ShowNotification($"Startup error: {ex.Message}", ToolTipIcon.Error);
                Stop();
            }
        }

        public async void Stop()
        {
            try
            {
                _isRunning = false;
                _cts?.Cancel();
                _trayIcon?.Dispose();
                _captureService?.Dispose();
            }
            catch { }
            finally
            {
                Environment.Exit(0);
            }
        }

        private async Task CaptureLoopAsync()
        {
            ShowNotification($"Sharing started — AI: {_aiProvider.Name}", ToolTipIcon.Info);
            UpdateStatus("Active");

            while (_isRunning && !_cts.Token.IsCancellationRequested)
            {
                try
                {
                    if (!_isPaused)
                    {
                        // Capture
                        var (jpegData, width, height) = _config.CaptureArea != null
                            ? _captureService.CaptureArea(
                                _config.CaptureArea.X, _config.CaptureArea.Y,
                                _config.CaptureArea.Width, _config.CaptureArea.Height)
                            : _captureService.CaptureScreen();

                        // Send to AI
                        if (_config.SendToAi && _aiProvider.IsConfigured)
                        {
                            string response = await _aiProvider.AnalyzeFrameAsync(jpegData, _config.SystemPrompt);
                            UpdateLastResponse(response);
                        }
                    }

                    await Task.Delay(_config.CaptureIntervalMs, _cts.Token);
                }
                catch (TaskCanceledException) { break; }
                catch (Exception ex)
                {
                    ShowNotification($"Error: {ex.Message}", ToolTipIcon.Warning);
                    await Task.Delay(3000);
                }
            }

            ShowNotification("Screen sharing stopped.", ToolTipIcon.Info);
        }

        private void SetupTrayIcon()
        {
            _trayIcon = new NotifyIcon
            {
                Icon    = SystemIcons.Application,
                Text    = "Vedion Screen Share",
                Visible = true
            };

            var menu = new ContextMenuStrip();

            _statusItem = new ToolStripMenuItem("Status: Starting...") { Enabled = false };
            menu.Items.Add(_statusItem);

            _lastResponseItem = new ToolStripMenuItem("Last response: (none)") { Enabled = false };
            menu.Items.Add(_lastResponseItem);

            menu.Items.Add(new ToolStripSeparator());

            _pauseItem = new ToolStripMenuItem("Pause");
            _pauseItem.Click += (s, e) =>
            {
                _isPaused = !_isPaused;
                _pauseItem.Text = _isPaused ? "Resume" : "Pause";
                UpdateStatus(_isPaused ? "Paused" : "Active");
            };
            menu.Items.Add(_pauseItem);

            menu.Items.Add(new ToolStripSeparator());

            var providerItem = new ToolStripMenuItem($"AI: {_aiProvider?.Name ?? "None"}") { Enabled = false };
            menu.Items.Add(providerItem);

            menu.Items.Add(new ToolStripSeparator());

            var exitItem = new ToolStripMenuItem("Exit");
            exitItem.Click += (s, e) => Stop();
            menu.Items.Add(exitItem);

            _trayIcon.ContextMenuStrip = menu;
        }

        private void UpdateStatus(string status)
        {
            if (_statusItem != null)
                _statusItem.Text = $"Status: {status}";
        }

        private void UpdateLastResponse(string response)
        {
            if (_lastResponseItem == null) return;
            string truncated = response.Length > 60 ? response[..60] + "..." : response;
            _lastResponseItem.Text = $"AI: {truncated}";
        }

        private void ShowNotification(string message, ToolTipIcon icon = ToolTipIcon.Info)
        {
            try { _trayIcon?.ShowBalloonTip(5000, "Vedion", message, icon); }
            catch { }
        }

        public void Dispose() => Stop();
    }
}
