using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.IO;

namespace CAPETF.Desktop;

public sealed class CapitalStreamingClient : IAsyncDisposable
{
    private readonly ClientWebSocket _socket = new();
    private readonly CancellationTokenSource _lifetime = new();
    private Task? _readerTask;
    private int _correlationId;

    public event EventHandler<QuoteUpdate>? QuoteReceived;
    public event EventHandler<string>? StatusChanged;

    public async Task ConnectAsync(CapitalSession session, CancellationToken cancellationToken = default)
    {
        if (_socket.State == WebSocketState.Open) return;
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
        if (_socket.State != WebSocketState.Open) return;
        var json = JsonSerializer.Serialize(message);
        var bytes = Encoding.UTF8.GetBytes(json);
        await _socket.SendAsync(bytes, WebSocketMessageType.Text, true, cancellationToken);
    }

    private async Task ReadLoopAsync(CancellationToken cancellationToken)
    {
        var buffer = new byte[64 * 1024];
        while (!cancellationToken.IsCancellationRequested && _socket.State == WebSocketState.Open)
        {
            using var memory = new MemoryStream();
            WebSocketReceiveResult result;
            do
            {
                result = await _socket.ReceiveAsync(buffer, cancellationToken);
                if (result.MessageType == WebSocketMessageType.Close) return;
                memory.Write(buffer, 0, result.Count);
            } while (!result.EndOfMessage);

            var json = Encoding.UTF8.GetString(memory.ToArray());
            HandleMessage(json);
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
                var price = bid ?? offer;
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
