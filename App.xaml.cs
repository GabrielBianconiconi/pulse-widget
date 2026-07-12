using System.Drawing;
using System.Windows;
using Microsoft.Win32;
using PulseWidget.Models;
using PulseWidget.Services;
using PulseWidget.Views;
using Forms = System.Windows.Forms;

namespace PulseWidget;

public partial class App : System.Windows.Application
{
    private const string StartupRegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string StartupValueName = "PulseWidget";

    private readonly SettingsStore _settingsStore = new();
    private readonly AlertEvaluator _alertEvaluator = new();
    private SingleInstanceCoordinator? _singleInstance;
    private HardwareMonitorService? _monitor;
    private MainWindow? _window;
    private Forms.NotifyIcon? _trayIcon;
    private Forms.ToolStripMenuItem? _clickThroughItem;
    private Forms.ToolStripMenuItem? _startupItem;
    private Forms.ToolStripMenuItem? _compactModeItem;
    private AppSettings _settings = new();
    private SettingsWindow? _settingsWindow;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _singleInstance = new SingleInstanceCoordinator();
        if (!_singleInstance.IsPrimary)
        {
            _singleInstance.NotifyPrimaryAsync().GetAwaiter().GetResult();
            _singleInstance.Dispose();
            _singleInstance = null;
            Shutdown();
            return;
        }

        _singleInstance.ActivationRequested += (_, _) => Dispatcher.BeginInvoke(ShowWidget);
        _singleInstance.StartListening();

        _settings = _settingsStore.Load();
        _monitor = new HardwareMonitorService();
        _window = new MainWindow(_settings);
        _monitor.SnapshotAvailable += OnSnapshotAvailable;

        CreateTrayIcon();
        _window.Show();
        _monitor.Start(_settings.UpdateIntervalMilliseconds);
    }

    private void OnSnapshotAvailable(object? sender, SensorSnapshot snapshot)
    {
        if (_window is null || Dispatcher.HasShutdownStarted)
        {
            return;
        }

        Dispatcher.BeginInvoke(() => ProcessSnapshot(snapshot));
    }

    private void ProcessSnapshot(SensorSnapshot snapshot)
    {
        if (_window is null)
        {
            return;
        }

        _window.UpdateSnapshot(snapshot);
        _window.SetTemperatureWarning(
            _settings.AlertsEnabled && snapshot.CpuTemperature >= _settings.CpuTemperatureThreshold,
            _settings.AlertsEnabled && snapshot.GpuTemperature >= _settings.GpuTemperatureThreshold);

        foreach (var alert in _alertEvaluator.Evaluate(snapshot, _settings))
        {
            _trayIcon?.ShowBalloonTip(
                5000,
                "Alerta do Pulse Widget",
                $"{alert.Metric}: {alert.Value:0} C (limite {alert.Threshold:0} C)",
                Forms.ToolTipIcon.Warning);
        }
    }

    private void CreateTrayIcon()
    {
        if (_window is null || _monitor is null)
        {
            return;
        }

        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("Mostrar widget", null, (_, _) => ShowWidget());
        menu.Items.Add("Configuracoes...", null, (_, _) => Dispatcher.BeginInvoke(ShowSettings));

        _clickThroughItem = new Forms.ToolStripMenuItem("Ignorar cliques")
        {
            Checked = _settings.ClickThrough,
            CheckOnClick = true
        };
        _clickThroughItem.CheckedChanged += (_, _) =>
        {
            if (_window is null)
            {
                return;
            }

            _window.SetClickThrough(_clickThroughItem.Checked);
            _settings.ClickThrough = _clickThroughItem.Checked;
            SaveSettings();
        };
        menu.Items.Add(_clickThroughItem);

        _compactModeItem = new Forms.ToolStripMenuItem("Modo compacto")
        {
            Checked = _settings.CompactMode,
            CheckOnClick = true
        };
        _compactModeItem.CheckedChanged += (_, _) => SetCompactMode(_compactModeItem.Checked);
        menu.Items.Add(_compactModeItem);

        var updateMenu = new Forms.ToolStripMenuItem("Intervalo de atualizacao");
        AddIntervalItem(updateMenu, "1 segundo", 1000);
        AddIntervalItem(updateMenu, "2 segundos", 2000);
        AddIntervalItem(updateMenu, "5 segundos", 5000);
        menu.Items.Add(updateMenu);

        _startupItem = new Forms.ToolStripMenuItem("Iniciar com o Windows")
        {
            Checked = IsStartupEnabled(),
            CheckOnClick = true
        };
        _startupItem.CheckedChanged += (_, _) => SetStartup(_startupItem.Checked);
        menu.Items.Add(_startupItem);

        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("Sair", null, async (_, _) => await ExitApplicationAsync());

        _trayIcon = new Forms.NotifyIcon
        {
            Text = "Pulse Widget",
            Icon = GetApplicationIcon(),
            ContextMenuStrip = menu,
            Visible = true
        };
        _trayIcon.DoubleClick += (_, _) => ShowWidget();

        _window.HideRequested += (_, _) =>
        {
            SaveSettings();
            _window.Hide();
        };
        _window.SettingsRequested += (_, _) => ShowSettings();
        _window.CompactModeChanged += (_, enabled) => SetCompactMode(enabled);
    }

    private void AddIntervalItem(Forms.ToolStripMenuItem parent, string text, int milliseconds)
    {
        var item = new Forms.ToolStripMenuItem(text)
        {
            Checked = _settings.UpdateIntervalMilliseconds == milliseconds
        };

        item.Click += (_, _) =>
        {
            _settings.UpdateIntervalMilliseconds = milliseconds;
            _monitor?.SetInterval(milliseconds);
            _window?.SetIntervalLabel(milliseconds);
            SaveSettings();

            foreach (var sibling in parent.DropDownItems.OfType<Forms.ToolStripMenuItem>())
            {
                sibling.Checked = ReferenceEquals(sibling, item);
            }
        };

        parent.DropDownItems.Add(item);
    }

    private void ShowWidget()
    {
        if (_window is null)
        {
            return;
        }

        _window.Show();
        _window.Activate();
    }

    private void ShowSettings()
    {
        if (_window is null)
        {
            return;
        }

        if (_settingsWindow is not null)
        {
            _settingsWindow.Activate();
            return;
        }

        _settingsWindow = new SettingsWindow(_settings, IsStartupEnabled()) { Owner = _window };
        if (_settingsWindow.ShowDialog() == true)
        {
            _settings = _settingsWindow.Result;
            _monitor?.SetInterval(_settings.UpdateIntervalMilliseconds);
            _window.ApplySettings(_settings);
            if (_compactModeItem is not null && _compactModeItem.Checked != _settings.CompactMode)
            {
                _compactModeItem.Checked = _settings.CompactMode;
            }
            if (_clickThroughItem is not null)
            {
                _clickThroughItem.Checked = _settings.ClickThrough;
            }

            if (_startupItem is not null && _startupItem.Checked != _settingsWindow.StartWithWindows)
            {
                _startupItem.Checked = _settingsWindow.StartWithWindows;
            }

            SetStartup(_settingsWindow.StartWithWindows);
            SaveSettings();
        }

        _settingsWindow = null;
    }

    private void SetCompactMode(bool enabled)
    {
        if (_window is null)
        {
            return;
        }

        _settings.CompactMode = enabled;
        _window.ApplyCompactMode(_settings);
        if (_compactModeItem is not null && _compactModeItem.Checked != enabled)
        {
            _compactModeItem.Checked = enabled;
        }

        SaveSettings();
    }

    private async Task ExitApplicationAsync()
    {
        if (_window is not null)
        {
            SaveSettings();
            _window.AllowClose();
        }

        if (_monitor is not null)
        {
            _monitor.SnapshotAvailable -= OnSnapshotAvailable;
            await _monitor.StopAsync();
            _monitor.Dispose();
        }

        if (_trayIcon is not null)
        {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
        }

        _singleInstance?.Dispose();
        _singleInstance = null;

        Shutdown();
    }

    private void SaveSettings()
    {
        if (_window is null)
        {
            return;
        }

        _settings = _window.CaptureSettings(_settings);
        _settingsStore.Save(_settings);
    }

    private static Icon GetApplicationIcon()
    {
        var executablePath = Environment.ProcessPath;
        return executablePath is null
            ? SystemIcons.Application
            : Icon.ExtractAssociatedIcon(executablePath) ?? SystemIcons.Application;
    }

    private static bool IsStartupEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(StartupRegistryPath);
        return key?.GetValue(StartupValueName) is string;
    }

    private static void SetStartup(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(StartupRegistryPath);
            if (enabled && Environment.ProcessPath is { } executablePath)
            {
                key.SetValue(StartupValueName, $"\"{executablePath}\"");
            }
            else
            {
                key.DeleteValue(StartupValueName, false);
            }
        }
        catch
        {
            // Startup is optional and can be blocked by Windows policies.
        }
    }
}
