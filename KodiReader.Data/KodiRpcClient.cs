using System.Net.WebSockets;
using System.Text.Json;

namespace KodiReader.Data;

/// <summary>Sends JSON-RPC requests to Kodi over one WebSocket connection.</summary>
internal sealed class KodiRpcClient : IDisposable
{
    private const int ReceiveBufferSize = 8192;

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly ClientWebSocket _socket = new();
    private int _lastRequestId;

    public Task ConnectAsync(Uri uri, CancellationToken ct) => _socket.ConnectAsync(uri, ct);

    /// <summary>Calls a Kodi method and deserializes its "result" into <typeparamref name="T"/>.</summary>
    public async Task<T> CallAsync<T>(string method, object? parameters, CancellationToken ct)
    {
        int requestId = ++_lastRequestId;
        await SendRequestAsync(requestId, method, parameters, ct);
        JsonElement result = await ReceiveResultAsync(requestId, method, ct);

        return result.Deserialize<T>(JsonOptions)
            ?? throw new KodiRpcException(method, "empty result");
    }

    public void Dispose() => _socket.Dispose();

    private async Task SendRequestAsync(int requestId, string method, object? parameters, CancellationToken ct)
    {
        var request = new { jsonrpc = "2.0", id = requestId, method, @params = parameters ?? new { } };
        byte[] payload = JsonSerializer.SerializeToUtf8Bytes(request);
        await _socket.SendAsync(payload, WebSocketMessageType.Text, endOfMessage: true, ct);
    }

    // Kodi also pushes notifications (e.g. Player.OnPause) on the same socket, so skip
    // every message until the response with our request id arrives.
    private async Task<JsonElement> ReceiveResultAsync(int requestId, string method, CancellationToken ct)
    {
        while (true)
        {
            using JsonDocument message = JsonDocument.Parse(await ReceiveMessageAsync(ct));
            JsonElement root = message.RootElement;

            if (!IsResponseTo(root, requestId))
                continue;

            if (root.TryGetProperty("error", out JsonElement error))
                throw new KodiRpcException(method, error.GetRawText());

            return root.GetProperty("result").Clone();
        }
    }

    private static bool IsResponseTo(JsonElement message, int requestId) =>
        message.TryGetProperty("id", out JsonElement id)
        && id.ValueKind == JsonValueKind.Number
        && id.GetInt32() == requestId;

    // Reads one complete text message; a large message can arrive in several frames.
    private async Task<byte[]> ReceiveMessageAsync(CancellationToken ct)
    {
        var buffer = new byte[ReceiveBufferSize];
        using var message = new MemoryStream();
        ValueWebSocketReceiveResult frame;

        do
        {
            frame = await _socket.ReceiveAsync(buffer.AsMemory(), ct);
            if (frame.MessageType == WebSocketMessageType.Close)
                throw new KodiConnectionException("Kodi closed the connection.");

            message.Write(buffer, 0, frame.Count);
        }
        while (!frame.EndOfMessage);

        return message.ToArray();
    }
}
