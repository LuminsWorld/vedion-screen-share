using System;
using System.Text.Json.Serialization;

namespace VedionScreenShare.Models
{
    /// <summary>
    /// Encrypted frame packet sent over WebSocket
    /// </summary>
    public class FramePacket
    {
        /// <summary>
        /// Unique frame ID for correlation
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Timestamp of capture (UTC, ISO 8601)
        /// </summary>
        [JsonPropertyName("timestamp")]
        public string Timestamp { get; set; } = DateTime.UtcNow.ToString("O");

        /// <summary>
        /// Encrypted JPEG frame data (Base64)
        /// </summary>
        [JsonPropertyName("data")]
        public string EncryptedData { get; set; } = "";

        /// <summary>
        /// IV for AES (Base64)
        /// </summary>
        [JsonPropertyName("iv")]
        public string Iv { get; set; } = "";

        /// <summary>
        /// Frame width in pixels
        /// </summary>
        [JsonPropertyName("width")]
        public int Width { get; set; }

        /// <summary>
        /// Frame height in pixels
        /// </summary>
        [JsonPropertyName("height")]
        public int Height { get; set; }

        /// <summary>
        /// Client version identifier
        /// </summary>
        [JsonPropertyName("version")]
        public string Version { get; set; } = "1.0";
    }

    /// <summary>
    /// Server response to frame
    /// </summary>
    public class FrameAck
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("status")]
        public string Status { get; set; } = "ok";

        [JsonPropertyName("message")]
        public string Message { get; set; } = "";
    }
}
