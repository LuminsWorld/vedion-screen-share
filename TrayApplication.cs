using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
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
        private DiscordService _discordService;

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
                _discordService    = new DiscordService(_config.DiscordWebhookUrl);

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

                        // Optionally post screenshot to Discord
                        if (_config.PostImagesToDiscord && _discordService.IsConfigured)
                            await _discordService.PostImageAsync(jpegData);

                        // Send to AI and post response to Discord
                        if (_config.SendToAi && _aiProvider.IsConfigured)
                        {
                            string aiResponse = await _aiProvider.AnalyzeFrameAsync(jpegData, _config.SystemPrompt);
                            UpdateLastResponse(aiResponse);

                            if (_config.PostResponsesToDiscord && _discordService.IsConfigured)
                                await _discordService.PostAiResponseAsync(_aiProvider.Name, aiResponse);
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

            _lastResponseItem = new ToolStripMenuItem("AI: (none yet)") { Enabled = false };
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

            // Settings — reopen setup window
            var settingsItem = new ToolStripMenuItem("⚙️  Settings...");
            settingsItem.Click += (s, e) =>
            {
                _isPaused = true;
                _pauseItem.Text = "Resume";
                UpdateStatus("Paused (configuring)");

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    var setup = new SetupWindow();
                    if (setup.ShowDialog() == true && setup.IsConfigured)
                    {
                        // Restart with new config
                        Stop();
                        var newApp = new TrayApplication(setup.Config);
                        newApp.Start();
                    }
                    else
                    {
                        _isPaused = false;
                        _pauseItem.Text = "Pause";
                        UpdateStatus("Active");
                    }
                });
            };
            menu.Items.Add(settingsItem);

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
