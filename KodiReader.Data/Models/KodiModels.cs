namespace KodiReader.Data.Models;

/// <summary>One entry of the Player.GetActivePlayers result.</summary>
public sealed record KodiActivePlayer(int PlayerId, string Type);

/// <summary>Result of Player.GetItem.</summary>
public sealed record KodiItemResponse(KodiItem Item);

/// <summary>Item that a Kodi player is playing. Season and Episode are -1 when not applicable.</summary>
public sealed record KodiItem(
    string? Type,
    string? Label,
    string? Title,
    string? ShowTitle,
    int Season,
    int Episode,
    int Year,
    string? File);
