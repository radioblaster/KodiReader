using System.Diagnostics;

namespace KodiReader.Tray;

/// <summary>Opens a file reported by Kodi: web URLs in the browser, local files selected in Explorer.</summary>
internal static class FileOpener
{
    /// <summary>
    /// True for http(s) URLs and existing local files. Kodi-only paths (smb://, plugin://, ...) cannot be opened.
    /// </summary>
    public static bool CanOpen(string file) => IsWebUrl(file) || File.Exists(file);

    public static void Open(string file)
    {
        if (IsWebUrl(file))
            Process.Start(new ProcessStartInfo(file) { UseShellExecute = true });
        else
            Process.Start("explorer.exe", $"/select,\"{file}\"");
    }

    // Only http(s) is shell-executed, so a path from Kodi can never start a program.
    private static bool IsWebUrl(string file) =>
        Uri.TryCreate(file, UriKind.Absolute, out Uri? uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}
