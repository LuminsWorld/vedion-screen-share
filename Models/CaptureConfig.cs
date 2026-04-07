using System;

namespace VedionScreenShare.Models
{
    /// <summary>
    /// Configuration for screen capture and transmission
    /// </summary>
    public class CaptureConfig
    {
        /// <summary>
        /// Capture interval in milliseconds
        /// </summary>
        public int CaptureIntervalMs { get; set; } = 1000;

        /// <summary>
        /// JPEG quality (1-100)
        /// </summary>
        public int JpegQuality { get; set; } = 75;

        /// <summary>
        /// WebSocket endpoint URL (e.g., wss://vedion.example.com/screen)
        /// </summary>
        public string EndpointUrl { get; set; } = "";

        /// <summary>
        /// AES-256 encryption key (Base64 encoded, 32 bytes)
        /// </summary>
        public string EncryptionKey { get; set; } = "";

        /// <summary>
        /// Whether to capture on startup
        /// </summary>
        public bool AutoStart { get; set; } = false;

        /// <summary>
        /// Screen area to capture (null = full screen)
        /// </summary>
        public CaptureArea? CaptureArea { get; set; }

        /// <summary>
        /// Tray window position (x, y)
        /// </summary>
        public (int X, int Y)? TrayPosition { get; set; }

        /// <summary>
        /// Whether to minimize to tray on startup
        /// </summary>
        public bool MinimizeToTray { get; set; } = true;
    }

    /// <summary>
    /// Define a custom capture area (screen region)
    /// </summary>
    public class CaptureArea
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }
}
