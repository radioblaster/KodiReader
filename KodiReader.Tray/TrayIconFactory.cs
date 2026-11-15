using System.Drawing.Drawing2D;

namespace KodiReader.Tray;

/// <summary>Draws the tray icon at runtime, so the app needs no .ico file.</summary>
internal static class TrayIconFactory
{
    private static readonly Color Background = Color.FromArgb(23, 178, 232);

    /// <summary>Creates a blue circle with a white "play" triangle in the system small-icon size.</summary>
    public static Icon Create()
    {
        Size size = SystemInformation.SmallIconSize;
        using var bitmap = new Bitmap(size.Width, size.Height);

        using (Graphics graphics = Graphics.FromImage(bitmap))
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var brush = new SolidBrush(Background);
            graphics.FillEllipse(brush, 0, 0, size.Width - 1, size.Height - 1);
            graphics.FillPolygon(Brushes.White, CreatePlayTriangle(size));
        }

        // The icon handle lives for the whole app lifetime, so it is not destroyed explicitly.
        return Icon.FromHandle(bitmap.GetHicon());
    }

    private static PointF[] CreatePlayTriangle(Size size) =>
    [
        new(size.Width * 0.38f, size.Height * 0.27f),
        new(size.Width * 0.38f, size.Height * 0.73f),
        new(size.Width * 0.76f, size.Height * 0.50f),
    ];
}
