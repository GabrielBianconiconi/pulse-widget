using System.IO;
using System.Windows;
using Microsoft.Win32;
using Clipboard = System.Windows.Clipboard;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace PulseWidget.Views;

public partial class DiagnosticsWindow : Window
{
    public DiagnosticsWindow(string report)
    {
        InitializeComponent();
        ReportTextBox.Text = report;
    }

    private void CopyButton_Click(object sender, RoutedEventArgs e)
    {
        Clipboard.SetText(ReportTextBox.Text);
    }

    private void ExportButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            FileName = $"pulse-widget-diagnostico-{DateTime.Now:yyyyMMdd-HHmmss}.txt",
            Filter = "Arquivo de texto (*.txt)|*.txt"
        };
        if (dialog.ShowDialog(this) == true)
        {
            File.WriteAllText(dialog.FileName, ReportTextBox.Text);
        }
    }
}
