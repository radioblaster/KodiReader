using KodiReader.Data;
using KodiReader.Dto;

namespace KodiReader.Business;

/// <summary>Provides information about what Kodi is playing.</summary>
public sealed class NowPlayingService(KodiPlayerApi kodiPlayerApi)
{
    /// <summary>Returns the playing item, or null when nothing is playing.</summary>
    /// <exception cref="KodiConnectionException">Kodi is not reachable or did not answer in time.</exception>
    /// <exception cref="KodiRpcException">Kodi answered with an error.</exception>
    public async Task<NowPlayingDto?> GetNowPlayingAsync(CancellationToken ct = default)
    {
        var item = await kodiPlayerApi.GetPlayingItemAsync(ct);
        return item is null ? null : NowPlayingMapper.ToDto(item);
    }
}
