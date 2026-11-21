using Microsoft.Win32;

namespace HuntKdaCalculator;

/// <summary>Автозапуск через HKCU\Software\Microsoft\Windows\CurrentVersion\Run.</summary>
static class Autostart
{
    const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    const string ValueName = "HuntKdaCalculator";

    // Environment.ProcessPath корректно работает и в single-file (Assembly.Location там пустой)
    static string ExePath =>
        Environment.ProcessPath ?? Path.Combine(AppContext.BaseDirectory, "HuntKdaCalculator.exe");

    public static bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, false);
        return key?.GetValue(ValueName) is string v &&
               v.Trim('"').Equals(ExePath, StringComparison.OrdinalIgnoreCase);
    }

    public static void Set(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKey, true);
        if (enabled) key.SetValue(ValueName, $"\"{ExePath}\"");
        else key.DeleteValue(ValueName, false);
    }
}
