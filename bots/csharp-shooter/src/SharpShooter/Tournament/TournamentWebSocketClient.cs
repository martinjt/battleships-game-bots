using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SharpShooter.Tournament.Messages;

namespace SharpShooter.Tournament;

public class TournamentWebSocketClient : IDisposable
{
    private readonly ILogger _logger;
    private readonly Uri _webSocketUri;
    private readonly int _maxReconnectAttempts;
    private ClientWebSocket? _webSocket;
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private CancellationTokenSource? _receiveLoopCts;
    private Task? _receiveLoopTask;
    private bool _disposed;

    public event Func<string, JsonElement, Task>? MessageReceived;
    public event Func<Task>? Connected;
    public event Func<Task>? Disconnected;

    public bool IsConnected => _webSocket?.State == WebSocketState.Open;

    public TournamentWebSocketClient(string webSocketUrl, int maxReconnectAttempts, ILogger logger)
    {
        _logger = logger;
        _webSocketUri = new Uri(webSocketUrl);
        _maxReconnectAttempts = maxReconnectAttempts;
    }

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        var attempt = 0;
        var delay = TimeSpan.FromSeconds(1);

        while (attempt < _maxReconnectAttempts && !cancellationToken.IsCancellationRequested)
        {
            try
            {
                attempt++;
                _logger.LogInformation("Connecting to WebSocket (attempt {Attempt}/{Max})...", attempt, _maxReconnectAttempts);

                _webSocket?.Dispose();
                _webSocket = new ClientWebSocket();

                await _webSocket.ConnectAsync(_webSocketUri, cancellationToken).ConfigureAwait(false);

                _logger.LogInformation("WebSocket connected successfully");

                // Start receive loop
                _receiveLoopCts = new CancellationTokenSource();
                _receiveLoopTask = Task.Run(() => ReceiveLoopAsync(_receiveLoopCts.Token), cancellationToken);

                if (Connected != null)
                {
                    await Connected.Invoke().ConfigureAwait(false);
                }

                return;
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "WebSocket connection attempt {Attempt} failed", attempt);

                if (attempt < _maxReconnectAttempts)
                {
                    _logger.LogInformation("Retrying in {Delay} seconds...", delay.TotalSeconds);
                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                    delay = TimeSpan.FromSeconds(Math.Min(delay.TotalSeconds * 2, 16)); // Exponential backoff, max 16 seconds
                }
                else
                {
                    _logger.LogError("Max reconnection attempts reached");
                    throw;
                }
            }
        }

        throw new OperationCanceledException("Connection cancelled");
    }

    public async Task SendMessageAsync<T>(string messageType, T payload, CancellationToken cancellationToken = default)
    {
        if (_webSocket?.State != WebSocketState.Open)
        {
            throw new InvalidOperationException("WebSocket is not connected");
        }

        var message = new WebSocketMessage<T>
        {
            MessageType = messageType,
            Payload = payload
        };

        var json = JsonSerializer.Serialize(message, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        _logger.LogDebug("Sending message: {MessageType}", messageType);
        _logger.LogTrace("Message payload: {Json}", json);

        var buffer = Encoding.UTF8.GetBytes(json);

        await _sendLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await _webSocket.SendAsync(
                new ArraySegment<byte>(buffer),
                WebSocketMessageType.Text,
                endOfMessage: true,
                cancellationToken
            ).ConfigureAwait(false);
        }
        finally
        {
            _sendLock.Release();
        }
    }

    private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
    {
        var buffer = new byte[8192];

        try
        {
            while (!cancellationToken.IsCancellationRequested && _webSocket?.State == WebSocketState.Open)
            {
                var messageBuilder = new StringBuilder();
                WebSocketReceiveResult result;

                do
                {
                    result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken).ConfigureAwait(false);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        _logger.LogInformation("WebSocket close message received");
                        await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Close received", CancellationToken.None).ConfigureAwait(false);
                        return;
                    }

                    messageBuilder.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                }
                while (!result.EndOfMessage);

                var messageJson = messageBuilder.ToString();
                _logger.LogTrace("Received message: {Message}", messageJson);

                await ProcessMessageAsync(messageJson).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Receive loop cancelled");
        }
        catch (WebSocketException ex)
        {
            _logger.LogError(ex, "WebSocket error in receive loop");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in receive loop");
        }
        finally
        {
            if (Disconnected != null)
            {
                await Disconnected.Invoke().ConfigureAwait(false);
            }
        }
    }

    private async Task ProcessMessageAsync(string messageJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(messageJson);
            var root = doc.RootElement;

            if (!root.TryGetProperty("messageType", out var messageTypeElement))
            {
                _logger.LogWarning("Received message without messageType: {Message}", messageJson);
                return;
            }

            var messageType = messageTypeElement.GetString();
            if (string.IsNullOrEmpty(messageType))
            {
                _logger.LogWarning("Received message with empty messageType");
                return;
            }

            _logger.LogDebug("Received message type: {MessageType}", messageType);

            JsonElement payload = default;
            if (root.TryGetProperty("payload", out var payloadElement))
            {
                payload = payloadElement;
            }

            if (MessageReceived != null)
            {
                await MessageReceived.Invoke(messageType, payload).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message: {Message}", messageJson);
        }
    }

    public async Task DisconnectAsync()
    {
        if (_webSocket?.State == WebSocketState.Open)
        {
            _logger.LogInformation("Disconnecting WebSocket...");

            _receiveLoopCts?.Cancel();

            try
            {
                await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client disconnecting", CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error closing WebSocket");
            }

            if (_receiveLoopTask != null)
            {
                await _receiveLoopTask.ConfigureAwait(false);
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _receiveLoopCts?.Cancel();
        _receiveLoopCts?.Dispose();
        _sendLock.Dispose();
        _webSocket?.Dispose();
    }
}
