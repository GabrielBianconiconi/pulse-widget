using Microsoft.Win32;

namespace PulseWidget.Services;

public sealed class StartupRegistrationService
{
    private const string RegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "PulseWidget";

    public bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RegistryPath);
        return key?.GetValue(ValueName) is string value && value == BuildCommand();
    }

    public bool SetEnabled(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(RegistryPath);
            if (enabled)
            {
                key.SetValue(ValueName, BuildCommand());
            }
            else
            {
                key.DeleteValue(ValueName, false);
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string BuildCommand()
    {
        var executablePath = Environment.ProcessPath ?? throw new InvalidOperationException("Executavel atual nao encontrado.");
        return $"\"{executablePath}\" --autostart";
    }
}
