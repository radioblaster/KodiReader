using Microsoft.Win32;

namespace KodiReader.Tray;

/// <summary>Registers the app to start with Windows for the current user (HKCU Run key).</summary>
internal sealed class WindowsStartup(string appName)
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

    private static string Command => $"\"{Environment.ProcessPath}\"";

    /// <summary>True when this exe, from its current location, is registered to start with Windows.</summary>
    public bool IsEnabled()
    {
        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKeyPath);
        return key?.GetValue(appName) is string value
            && string.Equals(value, Command, StringComparison.OrdinalIgnoreCase);
    }

    public void Enable()
    {
        using RegistryKey key = Registry.CurrentUser.CreateSubKey(RunKeyPath);
        key.SetValue(appName, Command);
    }

    public void Disable()
    {
        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
        key?.DeleteValue(appName, throwOnMissingValue: false);
    }
}
