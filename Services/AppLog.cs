using System.IO;

namespace PulseWidget.Services;

public static class AppLog
{
    private const long MaximumBytes = 1024 * 1024;
    private static readonly object Sync = new();
    private static readonly string DirectoryPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "PulseWidget");
    private static readonly string LogPath = Path.Combine(DirectoryPath, "pulse-widget.log");
    private static readonly string PreviousLogPath = Path.Combine(DirectoryPath, "pulse-widget.previous.log");

    public static string FilePath => LogPath;

    public static void Info(string message) => Write("INFO", message);

    public static void Error(string message, Exception? exception = null)
    {
        Write("ERROR", exception is null ? message : $"{message} | {exception.GetType().Name}: {exception.Message}");
    }

    public static string GetRecentLines(int maximumLines = 80)
    {
        lock (Sync)
        {
            try
            {
                return File.Exists(LogPath)
                    ? string.Join(Environment.NewLine, File.ReadLines(LogPath).TakeLast(maximumLines))
                    : "Nenhum log registrado.";
            }
            catch
            {
                return "Nao foi possivel ler o log.";
            }
        }
    }

    private static void Write(string level, string message)
    {
        lock (Sync)
        {
            try
            {
                Directory.CreateDirectory(DirectoryPath);
                if (File.Exists(LogPath) && new FileInfo(LogPath).Length >= MaximumBytes)
                {
                    File.Move(LogPath, PreviousLogPath, true);
                }

                File.AppendAllText(LogPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}{Environment.NewLine}");
            }
            catch
            {
            }
        }
    }
}
