namespace KodiReader.Dto;

/// <summary>What Kodi is playing, ready for display.</summary>
/// <param name="Type">Kodi item type, e.g. "movie", "episode", "song" or "unknown".</param>
/// <param name="Title">Display title; for TV episodes it includes the show, season and episode.</param>
/// <param name="Year">Release year, or null when Kodi does not know it.</param>
/// <param name="File">File path or stream URL.</param>
/// <param name="SuggestedFileName">Title as a valid Windows file name with the .mkv extension.</param>
public sealed record NowPlayingDto(string Type, string Title, int? Year, string File, string SuggestedFileName);
