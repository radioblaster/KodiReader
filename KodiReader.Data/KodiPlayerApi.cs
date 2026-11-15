using System.Net.WebSockets;
using KodiReader.Data.Models;

namespace KodiReader.Data;

/// <summary>Reads player information from Kodi.</summary>
public sealed class KodiPlayerApi(KodiSettings settings)
{
    private static readonly string[] ItemProperties = ["title", "showtitle", "season", "episode", "year", "file"];

    /// <summary>Returns the item Kodi is playing, or null when nothing is playing.</summary>
    /// <exception cref="KodiConnectionException">Kodi is not reachable or did not answer in time.</exception>
    /// <exception cref="KodiRpcException">Kodi answered with an error.</exception>
    public async Task<KodiItem?> GetPlayingItemAsync(CancellationToken ct = default)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(settings.Timeout);

        try
        {
            using var client = new KodiRpcClient();
            await client.ConnectAsync(settings.WebSocketUri, timeout.Token);
            return await ReadPlayingItemAsync(client, timeout.Token);
        }
        catch (WebSocketException ex)
        {
            throw new KodiConnectionException($"Cannot connect to Kodi at {settings.WebSocketUri}.", ex);
        }
        catch (OperationCanceledException ex) when (!ct.IsCancellationRequested)
        {
            throw new KodiConnectionException($"Kodi did not answer within {settings.TimeoutSeconds} s.", ex);
        }
    }

    private static async Task<KodiItem?> ReadPlayingItemAsync(KodiRpcClient client, CancellationToken ct)
    {
        var players = await client.CallAsync<List<KodiActivePlayer>>("Player.GetActivePlayers", null, ct);
        if (players.Count == 0)
            return null;

        var parameters = new { playerid = players[0].PlayerId, properties = ItemProperties };
        var response = await client.CallAsync<KodiItemResponse>("Player.GetItem", parameters, ct);
        return response.Item;
    }
}
