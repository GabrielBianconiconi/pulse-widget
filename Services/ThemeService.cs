using System.Windows;
using System.Windows.Media;
using Application = System.Windows.Application;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;

namespace PulseWidget.Services;

public static class ThemeService
{
    private static readonly IReadOnlyDictionary<string, ThemePalette> Palettes =
        new Dictionary<string, ThemePalette>(StringComparer.OrdinalIgnoreCase)
        {
            ["Dark"] = new("#F312161D", "#FF1A2029", "#FF83909F", "#FFF5F8FA", "#FF66E3A4", "#FF62A7FF"),
            ["Graphite"] = new("#F31B1B1F", "#FF25252B", "#FF9B9BA5", "#FFF4F4F5", "#FFFFB86C", "#FF8BE9FD"),
            ["Light"] = new("#FFF2F4F7", "#FFFFFFFF", "#FF647184", "#FF17202B", "#FF168A5B", "#FF1769AA")
        };

    public static void Apply(string? theme)
    {
        var palette = Palettes.TryGetValue(theme ?? string.Empty, out var selected)
            ? selected
            : Palettes["Dark"];
        SetBrush("WindowBackground", palette.WindowBackground);
        SetBrush("CardBackground", palette.CardBackground);
        SetBrush("MutedText", palette.MutedText);
        SetBrush("PrimaryText", palette.PrimaryText);
        SetBrush("CpuAccent", palette.CpuAccent);
        SetBrush("GpuAccent", palette.GpuAccent);
    }

    private static void SetBrush(string key, string color)
    {
        var parsedColor = (Color)ColorConverter.ConvertFromString(color);
        if (Application.Current.Resources[key] is SolidColorBrush brush && !brush.IsFrozen)
        {
            brush.Color = parsedColor;
        }
        else
        {
            Application.Current.Resources[key] = new SolidColorBrush(parsedColor);
        }
    }

    private sealed record ThemePalette(
        string WindowBackground,
        string CardBackground,
        string MutedText,
        string PrimaryText,
        string CpuAccent,
        string GpuAccent);
}
