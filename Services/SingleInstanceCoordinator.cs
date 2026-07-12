using System.IO;
using System.IO.Pipes;
using System.Security.Principal;
using System.Text;

namespace PulseWidget.Services;

public sealed class SingleInstanceCoordinator : IDisposable
{
    private readonly string _pipeName;
    private readonly Mutex _mutex;
    private readonly CancellationTokenSource _cancellation = new();
    private Task? _listenerTask;
    private bool _ownsMutex;

    public SingleInstanceCoordinator()
    {
        var sid = WindowsIdentity.GetCurrent().User?.Value ?? Environment.UserName;
        var instanceId = sid.Replace('-', '_');
        _pipeName = $"PulseWidget_{instanceId}";
        _mutex = new Mutex(true, $"Local\\{_pipeName}", out var createdNew);
        _ownsMutex = createdNew;
    }

    public bool IsPrimary => _ownsMutex;

    public event EventHandler? ActivationRequested;

    public void StartListening()
    {
        if (!IsPrimary || _listenerTask is not null)
        {
            return;
        }

        _listenerTask = Task.Run(() => ListenAsync(_cancellation.Token));
    }

    public async Task NotifyPrimaryAsync()
    {
        try
        {
            using var client = new NamedPipeClientStream(
                ".",
                _pipeName,
                PipeDirection.Out,
                PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
            await client.ConnectAsync(1500);
            await using var writer = new StreamWriter(client, new UTF8Encoding(false)) { AutoFlush = true };
            await writer.WriteLineAsync("activate");
        }
        catch
        {
            // The primary process may still be starting; the second instance exits safely.
        }
    }

    private async Task ListenAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await using var server = new NamedPipeServerStream(
                    _pipeName,
                    PipeDirection.In,
                    1,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
                await server.WaitForConnectionAsync(cancellationToken);
                using var reader = new StreamReader(server, Encoding.UTF8, false, 256, true);
                if (await reader.ReadLineAsync(cancellationToken) == "activate")
                {
                    ActivationRequested?.Invoke(this, EventArgs.Empty);
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch
            {
                await Task.Delay(250, cancellationToken);
            }
        }
    }

    public void Dispose()
    {
        _cancellation.Cancel();
        try
        {
            _listenerTask?.Wait(TimeSpan.FromSeconds(1));
        }
        catch
        {
        }

        _cancellation.Dispose();
        if (_ownsMutex)
        {
            _mutex.ReleaseMutex();
            _ownsMutex = false;
        }

        _mutex.Dispose();
    }
}
