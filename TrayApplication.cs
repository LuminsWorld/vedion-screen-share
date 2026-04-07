using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Security.Cryptography;
using VedionScreenShare.Models;
using VedionScreenShare.Services;

namespace VedionScreenShare
{
    /// <summary>
    /// Main application that runs in system tray and manages screen capture/transmission
    /// </summary>
    public class TrayApplication : IDisposable
    {
        private readonly CaptureConfig _config;
        private NotifyIcon _trayIcon;
        private bool _isRunning = false;
        private bool _isPaused = false;
        private CancellationTokenSource _cancellationTokenSource;

        private ScreenCaptureService _captureService;
        private EncryptionService _encryptionService;
        private NetworkService _networkService;

        public TrayApplication(CaptureConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <summary>
        /// Start the tray application
        /// </summary>
        public async void Start()
        {
            try
            {
                _isRunning = true;
                _cancellationTokenSource = new CancellationTokenSource();

                // Initialize services
                _captureService = new ScreenCaptureService(_config.JpegQuality);
                _encryptionService = new EncryptionService(_config.EncryptionKey);
                _networkService = new NetworkService(_config.EndpointUrl);

                // Setup tray icon
                SetupTrayIcon();

                // Connect to server
                await _networkService.ConnectAsync();

                // Start capture loop
                _ = CaptureLoopAsync();
            }
            catch (Exception ex)
            {
                ShowNotification($"Error: {ex.Message}", ToolTipIcon.Error);
                Stop();
            }
        }

        /// <summary>
        /// Stop the application
        /// </summary>
        public async void Stop()
        {
            try
            {
                _isRunning = false;
                _cancellationTokenSource?.Cancel();

                if (_networkService?.IsConnected == true)
                    await _networkService.DisconnectAsync();

                _trayIcon?.Dispose();
                _captureService?.Dispose();
                _networkService?.Dispose();

                Environment.Exit(0);
            }
            catch { /* Ignore cleanup errors */ }
        }

        /// <summary>
        /// Main capture loop
        /// </summary>
        private async Task CaptureLoopAsync()
        {
            ShowNotification("Screen sharing started", ToolTipIcon.Info);

            while (_isRunning && !_cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    if (!_isPaused && _networkService.IsConnected)
                    {
                        // Capture screen
                        var (jpegData, width, height) = _config.CaptureArea != null
                            ? _captureService.CaptureArea(
                                _config.CaptureArea.X,
                                _config.CaptureArea.Y,
                                _config.CaptureArea.Width,
                                _config.CaptureArea.Height)
                            : _captureService.CaptureScreen();

                        // Encrypt frame
                        var (ciphertext, iv) = _encryptionService.Encrypt(jpegData);

                        // Create packet
                        var packet = new FramePacket
                        {
                            EncryptedData = ciphertext,
                            Iv = iv,
                            Width = width,
                            Height = height
                        };

                        // Send to server
                        await _networkService.SendFrameAsync(packet);
                    }

                    // Wait for next capture interval
                    await Task.Delay(_config.CaptureIntervalMs, _cancellationTokenSource.Token);
                }
                catch (TaskCanceledException)
                {
                    // Expected when cancellation token is cancelled
                    break;
                }
                catch (Exception ex)
                {
                    ShowNotification($"Capture error: {ex.Message}", ToolTipIcon.Warning);
                    await Task.Delay(1000); // Back off briefly
                }
            }

            ShowNotification("Screen sharing stopped", ToolTipIcon.Info);
        }

        /// <summary>
        /// Setup system tray icon and context menu
        /// </summary>
        private void SetupTrayIcon()
        {
            _trayIcon = new NotifyIcon();
            _trayIcon.Icon = SystemIcons.Application;
            _trayIcon.Text = "Vedion Screen Share";
            _trayIcon.Visible = true;

            // Context menu
            var contextMenu = new ContextMenuStrip();

            var statusItem = new ToolStripMenuItem("Status: Active");
            contextMenu.Items.Add(statusItem);

            contextMenu.Items.Add(new ToolStripSeparator());

            var pauseItem = new ToolStripMenuItem("Pause");
            pauseItem.Click += (s, e) =>
            {
                _isPaused = !_isPaused;
                pauseItem.Text = _isPaused ? "Resume" : "Pause";
                statusItem.Text = _isPaused ? "Status: Paused" : "Status: Active";
            };
            contextMenu.Items.Add(pauseItem);

            contextMenu.Items.Add(new ToolStripSeparator());

            var exitItem = new ToolStripMenuItem("Exit");
            exitItem.Click += (s, e) => Stop();
            contextMenu.Items.Add(exitItem);

            _trayIcon.ContextMenuStrip = contextMenu;
        }

        /// <summary>
        /// Show a balloon notification in system tray
        /// </summary>
        private void ShowNotification(string message, ToolTipIcon icon = ToolTipIcon.Info)
        {
            try
            {
                _trayIcon?.ShowBalloonTip(5000, "Vedion", message, icon);
            }
            catch { /* Ignore notification errors */ }
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
