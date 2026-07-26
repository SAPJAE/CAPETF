using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.IO;

namespace CAPETF.Desktop;

internal interface ICapitalStreamingSocket : IDisposable
{
    WebSocketState State { get; }
    Task ConnectAsync(Uri uri, CancellationToken cancellationToken);
    Task SendAsync(ArraySegment<byte> bytes, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken);
    Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken);
    Task CloseAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken);
}

internal sealed class CapitalStreamingSocket : ICapitalStreamingSocket
{
    private readonly ClientWebSocket _socket = new();

    public WebSocketState State => _socket.State;
    public Task ConnectAsync(Uri uri, CancellationToken cancellationToken) => _socket.ConnectAsync(uri, cancellationToken);
    public Task SendAsync(ArraySegment<byte> bytes, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken) =>
        _socket.SendAsync(bytes, messageType, endOfMessage, cancellationToken);
    public Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken) =>
        _socket.ReceiveAsync(buffer, cancellationToken);
    public Task CloseAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken) =>
        _socket.CloseAsync(closeStatus, statusDescription, cancellationToken);
    public void Dispose() => _socket.Dispose();
}

public sealed class CapitalStreamingClient : IAsyncDisposable
{
    private readonly ICapitalStreamingSocket _socket;
    private readonly CancellationTokenSource _lifetime = new();
    private Task? _readerTask;
    private int _correlationId;

    public event EventHandler<QuoteUpdate>? QuoteReceived;
    public event EventHandler<string>? StatusChanged;
    public event EventHandler<string>? Disconnected;

    public CapitalStreamingClient() : this(new CapitalStreamingSocket())
    {
    }

    internal CapitalStreamingClient(ICapitalStreamingSocket socket)
    {
        _socket = socket;
    }

    public bool IsConnected =>
        _socket.State == WebSocketState.Open && (_readerTask is null || !_readerTask.IsCompleted);

    public async Task ConnectAsync(CapitalSession session, CancellationToken cancellationToken = default)
    {
        if (IsConnected) return;
        if (_socket.State != WebSocketState.None)
        {
            throw new InvalidOperationException($"Realtime socket cannot reconnect from {_socket.State}; create a new streaming client.");
        }
        await _socket.ConnectAsync(new Uri("wss://api-streaming-capital.backend-capital.com/connect"), cancellationToken);
        _readerTask = Task.Run(() => ReadLoopAsync(_lifetime.Token));
        StatusChanged?.Invoke(this, "Realtime connected");
        _ = Task.Run(() => PingLoopAsync(session, _lifetime.Token));
    }

    public Task SubscribeQuotesAsync(CapitalSession session, IEnumerable<string> epics, CancellationToken cancellationToken = default)
    {
        var selected = epics.Where(epic => !string.IsNullOrWhiteSpace(epic)).Distinct().Take(40).ToArray();
        return SendAsync(new
        {
            destination = "marketData.subscribe",
            correlationId = NextCorrelation(),
            cst = session.Cst,
            securityToken = session.SecurityToken,
            payload = new { epics = selected },
        }, cancellationToken);
    }

    public Task SubscribeOhlcAsync(CapitalSession session, IEnumerable<string> epics, string resolution, CancellationToken cancellationToken = default)
    {
        var selected = epics.Where(epic => !string.IsNullOrWhiteSpace(epic)).Distinct().Take(40).ToArray();
        return SendAsync(new
        {
            destination = "OHLCMarketData.subscribe",
            correlationId = NextCorrelation(),
            cst = session.Cst,
            securityToken = session.SecurityToken,
            payload = new { epics = selected, resolutions = new[] { resolution }, type = "classic" },
        }, cancellationToken);
    }

    private async Task SendAsync(object message, CancellationToken cancellationToken)
    {
        if (!IsConnected)
        {
            var status = $"Realtime disconnected: socket is {_socket.State}.";
            StatusChanged?.Invoke(this, status);
            throw new InvalidOperationException($"Realtime socket is not open ({_socket.State}).");
        }
        var json = JsonSerializer.Serialize(message);
        var bytes = Encoding.UTF8.GetBytes(json);
        await _socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cancellationToken);
    }

    private async Task ReadLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            var buffer = new byte[64 * 1024];
            while (!cancellationToken.IsCancellationRequested && _socket.State == WebSocketState.Open)
            {
                using var memory = new MemoryStream();
                WebSocketReceiveResult result;
                do
                {
                    result = await _socket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        var message = $"Realtime disconnected: {result.CloseStatusDescription ?? "socket closed"}.";
                        StatusChanged?.Invoke(this, message);
                        Disconnected?.Invoke(this, message);
                        return;
                    }
                    memory.Write(buffer, 0, result.Count);
                } while (!result.EndOfMessage);

                var json = Encoding.UTF8.GetString(memory.ToArray());
                HandleMessage(json);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            var message = $"Realtime faulted: {ex.Message}";
            StatusChanged?.Invoke(this, message);
            Disconnected?.Invoke(this, message);
            throw;
        }
    }

    private async Task PingLoopAsync(CapitalSession session, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMinutes(4), cancellationToken).ConfigureAwait(false);
            await SendAsync(new { destination = "ping", correlationId = NextCorrelation(), cst = session.Cst, securityToken = session.SecurityToken }, cancellationToken);
        }
    }

    private void HandleMessage(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var destination = root.TryGetProperty("destination", out var dest) ? dest.GetString() : "";
            if (destination == "quote" && root.TryGetProperty("payload", out var payload))
            {
                var epic = ReadString(payload, "epic") ?? "";
                var bid = ReadDecimal(payload, "bid");
                var offer = ReadDecimal(payload, "ofr");
                var price = bid is not null && offer is not null ? (bid.Value + offer.Value) / 2m : bid ?? offer;
                var time = ReadTimestamp(payload);
                QuoteReceived?.Invoke(this, new QuoteUpdate(epic, bid, offer, price, time));
                return;
            }

            if (!string.IsNullOrWhiteSpace(destination))
            {
                StatusChanged?.Invoke(this, destination);
            }
        }
        catch (Exception ex)
        {
            StatusChanged?.Invoke(this, $"Realtime parse error: {ex.Message}");
        }
    }

    private string NextCorrelation() => Interlocked.Increment(ref _correlationId).ToString();

    private static string? ReadString(JsonElement element, string name)
    {
        return element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    }

    private static decimal? ReadDecimal(JsonElement element, string name)
    {
        return element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var parsed) ? parsed : null;
    }

    private static DateTimeOffset ReadTimestamp(JsonElement payload)
    {
        if (payload.TryGetProperty("timestamp", out var timestamp) && timestamp.ValueKind == JsonValueKind.Number && timestamp.TryGetInt64(out var ms))
        {
            return DateTimeOffset.FromUnixTimeMilliseconds(ms);
        }
        return DateTimeOffset.Now;
    }

    public async ValueTask DisposeAsync()
    {
        _lifetime.Cancel();
        if (_socket.State == WebSocketState.Open)
        {
            await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "CAPETF closing", CancellationToken.None);
        }
        _socket.Dispose();
        _lifetime.Dispose();
        if (_readerTask is not null) await Task.WhenAny(_readerTask, Task.Delay(500));
    }
}
