using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using BattleshipsBot.Common.Skirmish.Messages;

namespace BattleshipsBot.Common.Skirmish;

public class SkirmishWebSocketClient : IDisposable
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

    public SkirmishWebSocketClient(string webSocketUrl, int maxReconnectAttempts, ILogger logger)
    {
        _logger = logger;
        _webSocketUri = new Uri(webSocketUrl);
        _maxReconnectAttempts = maxReconnectAttempts;
    }

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        var attempt = 0;
        var delay = TimeSpan.FromSeconds(1);

        // Keep trying to connect indefinitely
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                attempt++;
                _logger.LogInformation("Connecting to WebSocket (attempt {Attempt})...", attempt);

                _webSocket?.Dispose();
                _webSocket = new ClientWebSocket();

                await _webSocket.ConnectAsync(_webSocketUri, cancellationToken).ConfigureAwait(false);

                _logger.LogInformation("WebSocket connected successfully");

                // Reset delay on successful connection
                attempt = 0;
                delay = TimeSpan.FromSeconds(1);

                // Start receive loop
                _receiveLoopCts = new CancellationTokenSource();
                _receiveLoopTask = Task.Run(() => ReceiveLoopAsync(_receiveLoopCts.Token), cancellationToken);

                if (Connected != null)
                {
                    await Connected.Invoke().ConfigureAwait(false);
                }

                // Wait for receive loop to complete (disconnection)
                await _receiveLoopTask.ConfigureAwait(false);

                // If we get here, we disconnected - check if it was a rejection
                if (_webSocket.CloseStatus == WebSocketCloseStatus.PolicyViolation &&
                    _webSocket.CloseStatusDescription?.Contains("already has an active connection") == true)
                {
                    _logger.LogError("Connection rejected: Player already has an active connection. " +
                        "This indicates multiple bot instances are running with the same player ID. " +
                        "Stopping reconnection attempts to prevent connection storms.");
                    throw new InvalidOperationException(
                        "Connection rejected: Player already has an active connection. " +
                        "Only one bot instance per player ID is allowed.");
                }

                // Normal disconnection - try to reconnect
                _logger.LogInformation("Disconnected, will attempt to reconnect...");
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "WebSocket connection attempt {Attempt} failed", attempt);

                // Don't retry if it's a duplicate connection error
                if (ex is InvalidOperationException && ex.Message.Contains("already has an active connection"))
                {
                    throw;
                }

                _logger.LogInformation("Retrying in {Delay} seconds...", delay.TotalSeconds);

                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                delay = TimeSpan.FromSeconds(Math.Min(delay.TotalSeconds * 2, 30)); // Exponential backoff, max 30 seconds
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
