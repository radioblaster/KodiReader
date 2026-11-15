namespace KodiReader.Data;

/// <summary>Connection settings for Kodi JSON-RPC (bound from the "Kodi" section of appsettings.json).</summary>
public sealed class KodiSettings
{
    public string Host { get; set; } = "127.0.0.1";

    public int Port { get; set; } = 9090;

    public int TimeoutSeconds { get; set; } = 3;

    public Uri WebSocketUri => new($"ws://{Host}:{Port}/jsonrpc");

    public TimeSpan Timeout => TimeSpan.FromSeconds(TimeoutSeconds);
}
