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
        private bool _snapshotRequested = false;
        private CancellationTokenSource _cts;

        private ScreenCaptureService _captureService;
        private EncryptionService _encryptionService;
        private IAiProvider _aiProvider;
        private DiscordService _discordService;
        private HotkeyService _hotkeyService;

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
                SetupHotkeys();
                _ = CaptureLoopAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Startup error: {ex.Message}", "Vedion", MessageBoxButton.OK, MessageBoxImage.Error);
                Stop();
            }
        }

        private void SetupHotkeys()
        {
            try
            {
                _hotkeyService = new HotkeyService();
                _hotkeyService.Register(
                    _config.HotkeyPauseMod, _config.HotkeyPauseKey,
                    _config.HotkeySnapMod,  _config.HotkeySnapKey);

                _hotkeyService.OnPauseResumePressed += () =>
                {
                    _isPaused = !_isPaused;
                    if (_pauseItem != null) _pauseItem.Text = _isPaused ? "Resume" : "Pause";
                    UpdateStatus(_isPaused ? "Paused" : "Active");
                    ShowNotification(_isPaused ? "⏸ Paused" : "▶ Resumed", ToolTipIcon.Info);
                };

                _hotkeyService.OnSnapshotPressed += () =>
                {
                    if (_config.CaptureMode == CaptureMode.Snapshot || _isPaused)
                    {
                        _snapshotRequested = true;
                        ShowNotification("📸 Snapshot taken", ToolTipIcon.Info);
                    }
                };
            }
            catch { /* Hotkeys might fail if already registered — non-fatal */ }
        }

        public void Stop()
        {
            try
            {
                _isRunning = false;
                _cts?.Cancel();
                _hotkeyService?.Dispose();
                _trayIcon?.Dispose();
                _captureService?.Dispose();
            }
            catch { }
            finally { Environment.Exit(0); }
        }

        private async Task CaptureLoopAsync()
        {
            string modeStr = _config.CaptureMode == CaptureMode.Snapshot
                ? "Snapshot mode (Ctrl+Shift+S)" : "Live mode";
            ShowNotification($"Sharing started — {_aiProvider.Name} — {modeStr}", ToolTipIcon.Info);
            UpdateStatus("Active");

            while (_isRunning && !_cts.Token.IsCancellationRequested)
            {
                try
                {
                    bool shouldCapture = false;

                    if (_config.CaptureMode == CaptureMode.Continuous && !_isPaused)
                        shouldCapture = true;

                    if (_snapshotRequested)
                    {
                        shouldCapture = true;
                        _snapshotRequested = false;
                    }

                    if (shouldCapture)
                    {
                        var (jpegData, width, height) = _config.CaptureArea != null
                            ? _captureService.CaptureArea(
                                _config.CaptureArea.X, _config.CaptureArea.Y,
                                _config.CaptureArea.Width, _config.CaptureArea.Height)
                            : _captureService.CaptureScreen();

                        if (_config.PostImagesToDiscord && _discordService.IsConfigured)
                            await _discordService.PostImageAsync(jpegData);

                        if (_config.SendToAi && _aiProvider.IsConfigured)
                        {
                            string aiResponse = await _aiProvider.AnalyzeFrameAsync(jpegData, _config.SystemPrompt);
                            UpdateLastResponse(aiResponse);

                            if (_config.PostResponsesToDiscord && _discordService.IsConfigured)
                                await _discordService.PostAiResponseAsync(_aiProvider.Name, aiResponse);
                        }
                    }

                    await Task.Delay(
                        _config.CaptureMode == CaptureMode.Snapshot ? 200 : _config.CaptureIntervalMs,
                        _cts.Token);
                }
                catch (TaskCanceledException) { break; }
                catch (Exception ex)
                {
                    ShowNotification($"Error: {ex.Message}", ToolTipIcon.Warning);
                    await Task.Delay(3000);
                }
            }
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

            // Hotkey hints
            string pauseHint = _config.CaptureMode == CaptureMode.Continuous
                ? "Pause (Ctrl+Shift+P)" : "Pause (disabled in snapshot mode)";
            _pauseItem = new ToolStripMenuItem(pauseHint);
            _pauseItem.Click += (s, e) =>
            {
                _isPaused = !_isPaused;
                _pauseItem.Text = _isPaused ? "Resume (Ctrl+Shift+P)" : "Pause (Ctrl+Shift+P)";
                UpdateStatus(_isPaused ? "Paused" : "Active");
            };
            menu.Items.Add(_pauseItem);

            if (_config.CaptureMode == CaptureMode.Snapshot)
            {
                var snapItem = new ToolStripMenuItem("Take Snapshot (Ctrl+Shift+S)");
                snapItem.Click += (s, e) => _snapshotRequested = true;
                menu.Items.Add(snapItem);
            }

            // Settings
            var settingsItem = new ToolStripMenuItem("⚙️  Settings...");
            settingsItem.Click += (s, e) =>
            {
                _isPaused = true;
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    var setup = new SetupWindow(ConfigService.Load());
                    if (setup.ShowDialog() == true && setup.IsConfigured)
                    {
                        ConfigService.Save(setup.Config);
                        Stop();
                        var newApp = new TrayApplication(setup.Config);
                        newApp.Start();
                    }
                    else
                    {
                        _isPaused = false;
                        UpdateStatus("Active");
                    }
                });
            };
            menu.Items.Add(settingsItem);

            menu.Items.Add(new ToolStripSeparator());

            var modeItem = new ToolStripMenuItem(
                $"Mode: {(_config.CaptureMode == CaptureMode.Snapshot ? "Snapshot" : "Continuous")}") { Enabled = false };
            menu.Items.Add(modeItem);

            var aiItem = new ToolStripMenuItem($"AI: {_aiProvider?.Name ?? "None"}") { Enabled = false };
            menu.Items.Add(aiItem);

            menu.Items.Add(new ToolStripSeparator());

            var exitItem = new ToolStripMenuItem("Exit");
            exitItem.Click += (s, e) => Stop();
            menu.Items.Add(exitItem);

            _trayIcon.ContextMenuStrip = menu;
        }

        private void UpdateStatus(string status)
        {
            if (_statusItem != null) _statusItem.Text = $"Status: {status}";
        }

        private void UpdateLastResponse(string response)
        {
            if (_lastResponseItem == null) return;
            string t = response.Length > 60 ? response[..60] + "…" : response;
            _lastResponseItem.Text = $"AI: {t}";
        }

        private void ShowNotification(string message, ToolTipIcon icon = ToolTipIcon.Info)
        {
            try { _trayIcon?.ShowBalloonTip(4000, "Vedion", message, icon); }
            catch { }
        }

        public void Dispose() => Stop();
    }
}
