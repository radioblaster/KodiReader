using KodiReader.Data.Models;
using KodiReader.Dto;

namespace KodiReader.Business;

/// <summary>Maps a Kodi player item to <see cref="NowPlayingDto"/>.</summary>
internal static class NowPlayingMapper
{
    private const string SuggestedFileExtension = ".mkv";

    public static NowPlayingDto ToDto(KodiItem item)
    {
        string title = BuildTitle(item);

        return new NowPlayingDto(
            Type: string.IsNullOrWhiteSpace(item.Type) ? "unknown" : item.Type,
            Title: title,
            Year: item.Year > 0 ? item.Year : null,
            File: item.File ?? string.Empty,
            SuggestedFileName: BuildFileName(title));
    }

    // Items outside the Kodi library have no title, only a label (usually the file name).
    private static string BuildTitle(KodiItem item)
    {
        string title = string.IsNullOrWhiteSpace(item.Title) ? item.Label ?? string.Empty : item.Title;

        if (string.IsNullOrWhiteSpace(item.ShowTitle))
            return title;

        return item.Season >= 0 && item.Episode > 0
            ? $"{item.ShowTitle} – S{item.Season:D2}E{item.Episode:D2} – {title}"
            : $"{item.ShowTitle} – {title}";
    }

    // Characters Windows does not allow in file names (e.g. ':' in "Mission: Impossible") are removed.
    private static string BuildFileName(string title)
    {
        char[] invalidChars = Path.GetInvalidFileNameChars();
        string validName = new(title.Where(c => !invalidChars.Contains(c)).ToArray());
        return validName.Trim() + SuggestedFileExtension;
    }
}
