using KodiReader.Business;
using KodiReader.Data;
using Serilog;

namespace KodiReader.Tray;

/// <summary>Owns the tray icon, its menu and the popup window.</summary>
internal sealed class TrayApplicationContext : ApplicationContext
{
    private const string AppTitle = "Kodi Reader";

    private readonly NowPlayingService _nowPlayingService;
    private readonly WindowsStartup _windowsStartup;
    private readonly NowPlayingForm _popup = new();
    private readonly ToolStripMenuItem _startWithWindowsItem;
    private readonly NotifyIcon _trayIcon;

    public TrayApplicationContext(NowPlayingService nowPlayingService, WindowsStartup windowsStartup)
    {
        _nowPlayingService = nowPlayingService;
        _windowsStartup = windowsStartup;
        _startWithWindowsItem = new ToolStripMenuItem("Start with Windows", null, OnStartWithWindowsClick);

        _trayIcon = new NotifyIcon
        {
            Icon = TrayIconFactory.Create(),
            Text = AppTitle,
            ContextMenuStrip = CreateMenu(),
            Visible = true,
        };
        _trayIcon.MouseClick += OnTrayIconMouseClick;
    }

    protected override void ExitThreadCore()
    {
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        _popup.Dispose();
        base.ExitThreadCore();
    }

    private ContextMenuStrip CreateMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Show now playing", null, async (_, _) => await ShowNowPlayingAsync());
        menu.Items.Add(_startWithWindowsItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => ExitThread());

        // Re-read on every open: the user can also turn startup off in Windows Settings.
        menu.Opening += (_, _) => _startWithWindowsItem.Checked = _windowsStartup.IsEnabled();
        return menu;
    }

    private async void OnTrayIconMouseClick(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
            await ShowNowPlayingAsync();
    }

    private async Task ShowNowPlayingAsync()
    {
        _popup.ShowLoading();
        _popup.ShowNearTray();

        try
        {
            var nowPlaying = await _nowPlayingService.GetNowPlayingAsync();
            if (nowPlaying is null)
                _popup.ShowMessage("Nothing is playing.");
            else
                _popup.ShowNowPlaying(nowPlaying);

            Log.Debug("Now playing: {@NowPlaying}", nowPlaying);
        }
        catch (KodiConnectionException ex)
        {
            Log.Warning(ex, "Kodi is not reachable");
            _popup.ShowMessage("Kodi is not reachable. Is it running?");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Reading now playing info from Kodi failed");
            _popup.ShowMessage("Could not read data from Kodi. Details are in the log.");
        }
    }

    private void OnStartWithWindowsClick(object? sender, EventArgs e)
    {
        try
        {
            if (_windowsStartup.IsEnabled())
                _windowsStartup.Disable();
            else
                _windowsStartup.Enable();

            _startWithWindowsItem.Checked = _windowsStartup.IsEnabled();
            Log.Information("Start with Windows set to {Enabled}", _startWithWindowsItem.Checked);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Changing the Windows startup setting failed");
            MessageBox.Show("Could not change the Windows startup setting. Details are in the log.",
                AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
