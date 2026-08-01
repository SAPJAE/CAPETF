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
    private Task? _pingTask;
    private int _correlationId;
    private int _disposeStarted;

    public event EventHandler<QuoteUpdate>? QuoteReceived;
    public event EventHandler<CapitalOhlcUpdate>? OhlcReceived;
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
        _pingTask = Task.Run(() => PingLoopAsync(session, _lifetime.Token));
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
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromMinutes(4), cancellationToken).ConfigureAwait(false);
                await SendAsync(new { destination = "ping", correlationId = NextCorrelation(), cst = session.Cst, securityToken = session.SecurityToken }, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
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
                if (time is null)
                {
                    StatusChanged?.Invoke(this, "Realtime quote ignored: source timestamp missing.");
                    return;
                }
                QuoteReceived?.Invoke(this, new QuoteUpdate(epic, bid, offer, price, time.Value));
                return;
            }

            if (destination == "ohlc.event")
            {
                var update = ParseOhlcUpdate(root);
                if (update is null)
                {
                    StatusChanged?.Invoke(this, "Realtime OHLC ignored: payload is incomplete or invalid.");
                    return;
                }
                OhlcReceived?.Invoke(this, update);
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

    private static DateTimeOffset? ReadTimestamp(JsonElement payload)
    {
        if (payload.TryGetProperty("timestamp", out var timestamp) && timestamp.ValueKind == JsonValueKind.Number && timestamp.TryGetInt64(out var ms))
        {
            return DateTimeOffset.FromUnixTimeMilliseconds(ms);
        }
        return null;
    }

    internal static CapitalOhlcUpdate? ParseOhlcUpdate(string json)
    {
        using var document = JsonDocument.Parse(json);
        return ParseOhlcUpdate(document.RootElement);
    }

    private static CapitalOhlcUpdate? ParseOhlcUpdate(JsonElement root)
    {
        if (!root.TryGetProperty("destination", out var destination) ||
            !string.Equals(destination.GetString(), "ohlc.event", StringComparison.Ordinal) ||
            !root.TryGetProperty("payload", out var payload))
        {
            return null;
        }

        var epic = ReadString(payload, "epic") ?? "";
        var resolution = ReadString(payload, "resolution") ?? "";
        var open = ReadDecimal(payload, "o");
        var high = ReadDecimal(payload, "h");
        var low = ReadDecimal(payload, "l");
        var close = ReadDecimal(payload, "c");
        if (string.IsNullOrWhiteSpace(epic) || string.IsNullOrWhiteSpace(resolution) ||
            open is not > 0m || high is not > 0m || low is not > 0m || close is not > 0m ||
            high < low || high < open || high < close || low > open || low > close ||
            !payload.TryGetProperty("t", out var timestamp) ||
            timestamp.ValueKind != JsonValueKind.Number || !timestamp.TryGetInt64(out var milliseconds))
        {
            return null;
        }

        DateTimeOffset time;
        try
        {
            time = DateTimeOffset.FromUnixTimeMilliseconds(milliseconds);
        }
        catch (ArgumentOutOfRangeException)
        {
            return null;
        }
        return new CapitalOhlcUpdate(epic, resolution, time, open.Value, high.Value, low.Value, close.Value);
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposeStarted, 1) != 0) return;
        _lifetime.Cancel();
        try
        {
            if (_socket.State == WebSocketState.Open)
            {
                using var closeTimeout = new CancellationTokenSource(TimeSpan.FromMilliseconds(750));
                try
                {
                    await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "CAPETF closing", closeTimeout.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (closeTimeout.IsCancellationRequested)
                {
                }
                catch
                {
                    // Shutdown is best-effort; disposal below always releases the socket.
                }
            }

            var backgroundTasks = new[] { _readerTask, _pingTask }.Where(task => task is not null).Cast<Task>().ToArray();
            if (backgroundTasks.Length > 0)
            {
                var combined = Task.WhenAll(backgroundTasks);
                if (await Task.WhenAny(combined, Task.Delay(500)).ConfigureAwait(false) == combined)
                {
                    try { await combined.ConfigureAwait(false); }
                    catch { }
                }
            }
        }
        finally
        {
            _socket.Dispose();
            _lifetime.Dispose();
        }
    }
}
