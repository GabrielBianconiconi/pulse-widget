using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using PulseWidget.Models;

namespace PulseWidget.Services;

public static class SensorHostRunner
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static bool IsHostMode(IReadOnlyList<string> arguments) => arguments.Contains("--sensor-host");

    public static async Task RunAsync(IReadOnlyList<string> arguments)
    {
        var pipeName = GetArgument(arguments, "--pipe");
        var token = GetArgument(arguments, "--token");
        var parentId = int.Parse(GetArgument(arguments, "--parent"));
        using var parent = Process.GetProcessById(parentId);
        using var cancellation = new CancellationTokenSource();
        await using var pipe = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
        await pipe.ConnectAsync(15_000, cancellation.Token);
        using var reader = new StreamReader(pipe, Encoding.UTF8, false, 4096, true);
        await using var writer = new StreamWriter(pipe, new UTF8Encoding(false), 4096, true) { AutoFlush = true };
        var writerLock = new object();
        Write(writer, writerLock, new SensorPipeMessage { Type = "hello", Token = token });

        using var monitor = new HardwareMonitorService();
        var diagnosticsCounter = 0;
        monitor.SnapshotAvailable += (_, snapshot) =>
        {
            Write(writer, writerLock, new SensorPipeMessage { Type = "snapshot", Snapshot = snapshot });
            if (++diagnosticsCounter % 5 == 0)
            {
                Write(writer, writerLock, new SensorPipeMessage
                {
                    Type = "diagnostics",
                    Diagnostics = monitor.GetDiagnosticsReport()
                });
            }
        };
        monitor.Start(1000);

        try
        {
            while (pipe.IsConnected && !parent.HasExited)
            {
                var line = await reader.ReadLineAsync(cancellation.Token);
                if (line is null || line.Length > 64 * 1024)
                {
                    break;
                }

                var message = JsonSerializer.Deserialize<SensorPipeMessage>(line, JsonOptions);
                if (message?.Type == "shutdown")
                {
                    break;
                }

                if (message?.Type == "interval" && message.IntervalMilliseconds.HasValue)
                {
                    monitor.SetInterval(message.IntervalMilliseconds.Value);
                }
                else if (message?.Type == "gpu")
                {
                    monitor.SetSelectedGpu(message.GpuIdentifier);
                }
            }
        }
        catch (IOException)
        {
        }
        finally
        {
            await monitor.StopAsync();
        }
    }

    private static void Write(StreamWriter writer, object writerLock, SensorPipeMessage message)
    {
        lock (writerLock)
        {
            try
            {
                writer.WriteLine(JsonSerializer.Serialize(message, JsonOptions));
            }
            catch (IOException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
        }
    }

    private static string GetArgument(IReadOnlyList<string> arguments, string name)
    {
        var index = arguments.IndexOf(name);
        if (index < 0 || index + 1 >= arguments.Count)
        {
            throw new ArgumentException($"Argumento obrigatorio ausente: {name}");
        }

        return arguments[index + 1];
    }

    private static int IndexOf(this IReadOnlyList<string> arguments, string value)
    {
        for (var index = 0; index < arguments.Count; index++)
        {
            if (arguments[index] == value)
            {
                return index;
            }
        }

        return -1;
    }
}
