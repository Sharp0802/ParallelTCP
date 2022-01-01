using System.Net;
using System.Net.Sockets;
using ParallelTCP.Common;
using ParallelTCP.Shared;
using ParallelTCP.Shared.Handlers;

namespace ParallelTCP.ClientSide;

public class Client : IAsyncDisposable, IDisposable
{
    /// <summary>
    /// Create new <see cref="ParallelTCP.ClientSide.Client"/>
    /// </summary>
    /// <param name="endpoint">The <see cref="System.Net.EndPoint"/> to connect</param>
    public Client(IPEndPoint endpoint)
    {
        EndPoint = endpoint;
        TcpClient = new TcpClient();
        MessageContext = new MessageContext(Guid.NewGuid(), TcpClient, new object(), ShutdownTokenSource.Token);
    }

    private CancellationTokenSource ShutdownTokenSource { get; } = new();
    private TcpClient TcpClient { get; }
    
    /// <summary>
    /// Gets the remote <see cref="System.Net.IPEndPoint"/>
    /// </summary>
    public IPEndPoint EndPoint { get; }

    /// <summary>
    /// Gets the message context
    /// </summary>
    /// <exception cref="NullReferenceException"><see cref="ParallelTCP.ClientSide.Client.Context"/> isn't initialized.</exception>
    public MessageContext MessageContext { get; }

    public event NetworkConnectionEventHandler? Connected;

    /// <summary>
    /// Start receiving messages.
    /// </summary>
    public async Task RunAsync()
    {
        await TcpClient.ConnectAsync(EndPoint.Address, EndPoint.Port);
        await Connected.InvokeAsync(this, new NetworkConnectionEventArgs(MessageContext));
        await MessageContext.RunAsync();
    }

    /// <summary>
    /// Disconnects and Disposes this <see cref="ParallelTCP.ClientSide.Client"/> instance and requests that the
    /// underlying TCP connection be closed.
    /// </summary>
    public async Task ShutdownAsync()
    {
        ShutdownTokenSource.Cancel();
        ShutdownTokenSource.Dispose();
        await MessageContext.DisconnectAsync();
    }
    
    /// <inheritdoc cref="ParallelTCP.ClientSide.Client.ShutdownAsync()"/>
    public async ValueTask DisposeAsync()
    {
        await ShutdownAsync();
    }

    /// <inheritdoc cref="ParallelTCP.ClientSide.Client.ShutdownAsync()"/>
    public void Dispose()
    {
        ShutdownAsync().Wait();
    }
}