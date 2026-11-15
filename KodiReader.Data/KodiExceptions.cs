namespace KodiReader.Data;

/// <summary>Kodi is not running, refused the connection or did not answer in time.</summary>
public sealed class KodiConnectionException(string message, Exception? innerException = null)
    : Exception(message, innerException);

/// <summary>Kodi answered a JSON-RPC call with an error.</summary>
public sealed class KodiRpcException(string method, string error)
    : Exception($"Kodi returned an error for {method}: {error}");
