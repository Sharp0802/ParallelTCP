using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using ParallelTCP.Common;
using ParallelTCP.Shared;

namespace ParallelTCP.ServerSide;

public class Server : IAsyncDisposable, IDisposable
{
    public Server(int port)
    {
        LocalEndpoint = new IPEndPoint(IPAddress.Any, port);
    }
    private bool Disposed { get; set; }
    private object LockHandle { get; } = new();
    
    private TcpListener? TcpListener { get; set; }
    private ConcurrentDictionary<Guid, MessageContext> Contexts { get; } = new();
    private CancellationTokenSource ShutdownTokenSource { get; } = new();

    public IPEndPoint LocalEndpoint { get; }

    public event NetworkConnectionEventHandler? ClientConnected;
    public event NetworkConnectionEventHandler? Shutdown;
    
    public Task OpenAsync()
    {
        TcpListener = new TcpListener(LocalEndpoint);
        return Task.CompletedTask;
    }

    public async Task RunAsync()
    {
        if (TcpListener is null) throw new NullReferenceException("tcp listener is null");
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
                    if (!Contexts.TryAdd(guid, context))
                        throw new AmbiguousMatchException("guid is overlapped");
                }
                catch (Exception)
                {
                    break;
                }
                context.Disconnected += (_, args) => Task.FromResult(Contexts.TryRemove(args.Context.Guid, out var _));
                tasks.Add(ClientConnected.InvokeAsync(this, new NetworkConnectionEventArgs(context)));
            }
        }, TaskCreationOptions.None);
        await Task.WhenAll(tasks);
        await ShutdownAsync();
    }

    public async Task ShutdownAsync()
    {
        lock (LockHandle)
        {
            if (Disposed) return;
            ShutdownTokenSource.Cancel();
            TcpListener?.Stop();
            Disposed = true;
        }
        await Task.WhenAll(Contexts.Select(pair => pair.Value.DisconnectAsync()).ToArray());
        await Shutdown.InvokeAsync(this, new NetworkConnectionEventArgs(null));
    }
    
    public async ValueTask DisposeAsync()
    {
        await ShutdownAsync();
        ShutdownTokenSource.Dispose();
    }

    public void Dispose()
    {
        ShutdownAsync().Wait();
        ShutdownTokenSource.Dispose();
    }
}