namespace KodiReader.Tray;

/// <summary>Shortens text to a pixel width.</summary>
internal static class TextEllipsis
{
    private const string Ellipsis = "…";
    private const TextFormatFlags MeasureFlags = TextFormatFlags.SingleLine | TextFormatFlags.NoPadding;

    /// <summary>
    /// Cuts the middle of the text so it fits into <paramref name="maxWidth"/> pixels,
    /// keeping the start (drive, host) and the end (file name) readable.
    /// </summary>
    public static string FitMiddle(string text, Font font, int maxWidth)
    {
        if (Fits(text, font, maxWidth))
            return text;

        for (int keep = text.Length - 1; keep > 0; keep--)
        {
            int headLength = (keep + 1) / 2;
            int tailLength = keep / 2;
            string candidate = text[..headLength] + Ellipsis + text[^tailLength..];

            if (Fits(candidate, font, maxWidth))
                return candidate;
        }

        return Ellipsis;
    }

    private static bool Fits(string text, Font font, int maxWidth) =>
        TextRenderer.MeasureText(text, font, Size.Empty, MeasureFlags).Width <= maxWidth;
}
