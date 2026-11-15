using System.Runtime.InteropServices;
using KodiReader.Dto;
using Serilog;

namespace KodiReader.Tray;

/// <summary>Borderless popup above the tray that shows what Kodi is playing.</summary>
internal sealed class NowPlayingForm : Form
{
    private const int DropShadowClassStyle = 0x00020000; // CS_DROPSHADOW
    private const int ValueWidth = 360;
    private const int ScreenMargin = 12;

    private readonly Label _type;
    private readonly Label _title;
    private readonly Label _year;
    private readonly LinkLabel _file;
    private readonly Label _message;
    private readonly TableLayoutPanel _details;
    private readonly ToolTip _toolTip = new();

    public NowPlayingForm()
    {
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        ShowInTaskbar = false;
        TopMost = true;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        ResizeRedraw = true;
        BackColor = SystemColors.Window;
        ForeColor = SystemColors.WindowText;
        Font = new Font("Segoe UI", 10F);
        Padding = new Padding(Dip(16), Dip(12), Dip(16), Dip(14));

        _type = CreateValueLabel();
        _title = CreateValueLabel();
        _title.Font = new Font(Font, FontStyle.Bold);
        _year = CreateValueLabel();
        _file = CreateFileLabel();
        _message = CreateValueLabel();
        _details = CreateDetailsTable();

        Controls.Add(CreateLayout());
    }

    protected override CreateParams CreateParams
    {
        get
        {
            CreateParams createParams = base.CreateParams;
            createParams.ClassStyle |= DropShadowClassStyle;
            return createParams;
        }
    }

    /// <summary>Shows the popup in the bottom-right corner of the screen with the tray.</summary>
    public void ShowNearTray()
    {
        PlaceNearTray();
        Show();
        Activate();
    }

    public void ShowLoading() => ShowMessage("Loading…");

    public void ShowMessage(string message)
    {
        _message.Text = message;
        _details.Visible = false;
        _message.Visible = true;
    }

    public void ShowNowPlaying(NowPlayingDto nowPlaying)
    {
        _type.Text = nowPlaying.Type;
        _title.Text = nowPlaying.Title;
        _year.Text = nowPlaying.Year?.ToString() ?? "–";
        ShowFile(nowPlaying);
        _message.Visible = false;
        _details.Visible = true;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _toolTip.Dispose();

        base.Dispose(disposing);
    }

    // Hide when the user clicks anywhere else, like a standard tray flyout.
    protected override void OnDeactivate(EventArgs e)
    {
        base.OnDeactivate(e);
        Hide();
    }

    protected override bool ProcessDialogKey(Keys keyData)
    {
        if (keyData != Keys.Escape)
            return base.ProcessDialogKey(keyData);

        Hide();
        return true;
    }

    // Content changes resize the form (AutoSize), so keep it anchored to the corner.
    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        PlaceNearTray();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        using var border = new Pen(SystemColors.ControlDark);
        e.Graphics.DrawRectangle(border, 0, 0, Width - 1, Height - 1);
    }

    private void PlaceNearTray()
    {
        Rectangle area = Screen.FromPoint(Cursor.Position).WorkingArea;
        Location = new Point(area.Right - Width - Dip(ScreenMargin), area.Bottom - Height - Dip(ScreenMargin));
    }

    private FlowLayoutPanel CreateLayout()
    {
        var layout = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            Margin = Padding.Empty,
        };
        layout.Controls.Add(CreateHeader());
        layout.Controls.Add(_details);
        layout.Controls.Add(_message);
        return layout;
    }

    private Label CreateHeader() => new()
    {
        Text = "Now playing in Kodi",
        AutoSize = true,
        ForeColor = SystemColors.GrayText,
        Margin = new Padding(0, 0, 0, Dip(8)),
    };

    private TableLayoutPanel CreateDetailsTable()
    {
        var table = new TableLayoutPanel { ColumnCount = 2, AutoSize = true, Margin = Padding.Empty };
        AddDetailRow(table, "Type", _type);
        AddDetailRow(table, "Title", _title);
        AddDetailRow(table, "Year", _year);
        AddDetailRow(table, "File", _file);
        return table;
    }

    private void AddDetailRow(TableLayoutPanel table, string caption, Label value)
    {
        var captionLabel = new Label
        {
            Text = caption,
            AutoSize = true,
            ForeColor = SystemColors.GrayText,
            Margin = new Padding(0, Dip(3), Dip(12), Dip(3)),
        };
        table.Controls.Add(captionLabel, 0, table.RowCount);
        table.Controls.Add(value, 1, table.RowCount);
        table.RowCount++;
    }

    private Label CreateValueLabel() => new()
    {
        AutoSize = true,
        MaximumSize = new Size(Dip(ValueWidth), 0),
        Margin = new Padding(0, Dip(3), 0, Dip(3)),
    };

    // Paths and URLs rarely contain spaces and would not wrap, so they stay on one line.
    private LinkLabel CreateFileLabel()
    {
        var label = new LinkLabel
        {
            AutoSize = false,
            Size = new Size(Dip(ValueWidth), Font.Height + Dip(2)),
            Margin = new Padding(0, Dip(3), 0, Dip(3)),
            LinkBehavior = LinkBehavior.HoverUnderline,
        };
        label.LinkClicked += OnFileLinkClicked;
        return label;
    }

    // Shortened in the middle to keep the file name visible; the full path is in the tooltip.
    // Paths Windows cannot open (e.g. smb:// or plugin://) are shown as plain text.
    private void ShowFile(NowPlayingDto nowPlaying)
    {
        string file = nowPlaying.File;
        bool isLink = FileOpener.CanOpen(file);

        _file.Text = TextEllipsis.FitMiddle(file, _file.Font, _file.ClientSize.Width - Dip(4));
        _file.LinkArea = isLink ? new LinkArea(0, _file.Text.Length) : new LinkArea(0, 0);
        if (isLink)
            _file.Links[0].LinkData = nowPlaying;

        string hint = isLink ? $"{Environment.NewLine}Click to open it and copy \"{nowPlaying.SuggestedFileName}\"" : "";
        _toolTip.SetToolTip(_file, file + hint);
    }

    // Copies the suggested file name first, so the clipboard is filled even if opening fails.
    private void OnFileLinkClicked(object? sender, LinkLabelLinkClickedEventArgs e)
    {
        if (e.Link?.LinkData is not NowPlayingDto nowPlaying)
            return;

        CopyToClipboard(nowPlaying.SuggestedFileName);
        OpenFile(nowPlaying.File);
        Hide();
    }

    // Another app may hold the clipboard for a moment, hence the retries.
    private static void CopyToClipboard(string text)
    {
        try
        {
            Clipboard.SetDataObject(text, copy: true, retryTimes: 5, retryDelay: 100);
        }
        catch (ExternalException ex)
        {
            Log.Error(ex, "Copying {Text} to the clipboard failed", text);
        }
    }

    private static void OpenFile(string file)
    {
        try
        {
            FileOpener.Open(file);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Opening {File} failed", file);
        }
    }

    private int Dip(int logicalPixels) => LogicalToDeviceUnits(logicalPixels);
}
