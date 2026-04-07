using System;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using VedionScreenShare.Models;

namespace VedionScreenShare.Services
{
    /// <summary>
    /// WebSocket client for transmitting encrypted frames
    /// </summary>
    public class NetworkService : IDisposable
    {
        private ClientWebSocket _webSocket;
        private readonly Uri _endpointUri;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private bool _connected = false;

        public event EventHandler<string> OnConnected;
        public event EventHandler<string> OnDisconnected;
        public event EventHandler<Exception> OnError;

        public bool IsConnected => _connected && _webSocket?.State == WebSocketState.Open;

        public NetworkService(string endpointUrl)
        {
            if (!Uri.TryCreate(endpointUrl, UriKind.Absolute, out var uri))
                throw new ArgumentException("Invalid endpoint URL", nameof(endpointUrl));

            _endpointUri = uri;
            _cancellationTokenSource = new CancellationTokenSource();
        }

        /// <summary>
        /// Connect to the WebSocket endpoint
        /// </summary>
        public async Task ConnectAsync()
        {
            try
            {
                _webSocket = new ClientWebSocket();
                
                // Set certificate validation (implement pinning in production)
                // For now, using default validation
                
                await _webSocket.ConnectAsync(_endpointUri, _cancellationTokenSource.Token);
                _connected = true;
                OnConnected?.Invoke(this, $"Connected to {_endpointUri.Host}");

                // Start listening for responses
                _ = ListenAsync();
            }
            catch (Exception ex)
            {
                _connected = false;
                OnError?.Invoke(this, ex);
                throw;
            }
        }

        /// <summary>
        /// Send an encrypted frame packet
        /// </summary>
        public async Task SendFrameAsync(FramePacket packet)
        {
            if (!IsConnected)
                throw new InvalidOperationException("WebSocket not connected");

            try
            {
                string json = JsonSerializer.Serialize(packet);
                byte[] data = Encoding.UTF8.GetBytes(json);

                await _webSocket.SendAsync(
                    data,
                    WebSocketMessageType.Text,
                    endOfMessage: true,
                    _cancellationTokenSource.Token
                );
            }
            catch (Exception ex)
            {
                OnError?.Invoke(this, ex);
                throw;
            }
        }

        /// <summary>
        /// Listen for incoming responses from server
        /// </summary>
        private async Task ListenAsync()
        {
            byte[] buffer = new byte[1024 * 64]; // 64KB buffer

            try
            {
                while (_webSocket.State == WebSocketState.Open)
                {
                    var result = await _webSocket.ReceiveAsync(
                        new ArraySegment<byte>(buffer),
                        _cancellationTokenSource.Token
                    );

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        string json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        try
                        {
                            var ack = JsonSerializer.Deserialize<FrameAck>(json);
                            // Handle acknowledgement if needed
                        }
                        catch { /* Ignore parse errors */ }
                    }
                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await _webSocket.CloseAsync(
                            WebSocketCloseStatus.NormalClosure,
                            "Closing",
                            CancellationToken.None
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                OnError?.Invoke(this, ex);
            }
            finally
            {
                _connected = false;
                OnDisconnected?.Invoke(this, "Disconnected");
            }
        }

        /// <summary>
        /// Disconnect from the server
        /// </summary>
        public async Task DisconnectAsync()
        {
            if (_webSocket?.State == WebSocketState.Open)
            {
                try
                {
                    await _webSocket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "Client closing",
                        CancellationToken.None
                    );
                }
                catch { /* Ignore close errors */ }
            }
            _connected = false;
        }

        public void Dispose()
        {
            _cancellationTokenSource?.Cancel();
            _webSocket?.Dispose();
            _cancellationTokenSource?.Dispose();
        }
    }
}
