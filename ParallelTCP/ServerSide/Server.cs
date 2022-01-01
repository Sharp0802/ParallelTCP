using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using ParallelTCP.Common;
using ParallelTCP.Shared;
using ParallelTCP.Shared.Handlers;

namespace ParallelTCP.ServerSide;

public class Server : IAsyncDisposable, IDisposable
{
    /// <summary>
    /// Create new <see cref="ParallelTCP.ServerSide"/>
    /// </summary>
    /// <param name="port">the port to <see cref="ParallelTCP.ServerSide.Server"/> will be listening at</param>
    public Server(int port)
    {
        LocalEndpoint = new IPEndPoint(IPAddress.Any, port);
        TcpListener = new TcpListener(LocalEndpoint);
    }
    
    private bool Disposed { get; set; }
    private object LockHandle { get; } = new();
    
    private TcpListener TcpListener { get; }
    private ConcurrentDictionary<Guid, (MessageContext Context, Task Runner)> Containers { get; } = new();
    private CancellationTokenSource ShutdownTokenSource { get; } = new();

    /// <summary>
    /// Gets the local <see cref="System.Net.IPEndPoint"/>.
    /// </summary>
    /// <returns>The <see cref="System.Net.IPEndPoint"/> that the <see cref="ParallelTCP.ServerSide.Server"/> is using
    /// for communications.</returns>
    public IPEndPoint LocalEndpoint { get; }

    /// <summary>
    /// Raises when a client connected successfully
    /// </summary>
    public event NetworkConnectionEventHandler? ClientConnected;
    
    /// <summary>
    /// Raises when the <see cref="ParallelTCP.ServerSide.Server"/> shut down.
    /// </summary>
    public event NetworkConnectionEventHandler? Shutdown;
    
    /// <summary>
    /// Starts listening for incoming connection requests.
    /// </summary>
    /// <inheritdoc cref="System.Net.Sockets.TcpListener.Start()" path="/exception"/>
    public Task OpenAsync()
    {
        TcpListener.Start();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Starts receiving messages.
    /// </summary>
    public async Task RunAsync()
    {
        var tasks = new List<Task>();
        await Task.Factory.StartNew(() =>
        {
            while (!ShutdownTokenSource.IsCancellationRequested && !Disposed)
            {
                var guid = Guid.NewGuid();
                MessageContext context;
                try
                {
                    context = new MessageContext(guid, TcpListener.AcceptTcpClient(), new object(), ShutdownTokenSource.Token);
                    if (!Containers.TryAdd(guid, (context, context.RunAsync())))
                        throw new AmbiguousMatchException("guid is overlapped");
                }
                catch (Exception)
                {
                    break;
                }
                context.Disconnected += (_, _) => Task.FromResult(Containers.TryRemove(guid, out _));
                tasks.Add(ClientConnected.InvokeAsync(this, new NetworkConnectionEventArgs(context)));
            }
        }, TaskCreationOptions.None);
        await Task.WhenAll(tasks);
        await ShutdownAsync();
    }
    
    /// <summary>
    /// Disconnects and Disposes this <see cref="ParallelTCP.ServerSide.Server"/> instance and requests that the
    /// underlying TCP connection be closed.
    /// </summary>
    public async Task ShutdownAsync()
    {
        lock (LockHandle)
        {
            if (Disposed) return;
            ShutdownTokenSource.Cancel();
            TcpListener.Stop();
            Disposed = true;
        }
        await Task.WhenAll(Containers.Select(pair => pair.Value.Context.DisconnectAsync()).ToArray());
        await Shutdown.InvokeAsync(this, new NetworkConnectionEventArgs(null));
    }
    
    /// <inheritdoc cref="ParallelTCP.ServerSide.Server.ShutdownAsync()"/>
    public async ValueTask DisposeAsync()
    {
        await ShutdownAsync();
        ShutdownTokenSource.Dispose();
    }

    /// <inheritdoc cref="ParallelTCP.ServerSide.Server.ShutdownAsync()"/>
    public void Dispose()
    {
        ShutdownAsync().Wait();
        ShutdownTokenSource.Dispose();
    }
}