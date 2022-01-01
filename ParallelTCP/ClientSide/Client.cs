using System.Net;
using System.Net.Sockets;
using ParallelTCP.Shared;

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
    }

    private MessageContext? Context { get; set; }
    private CancellationTokenSource ShutdownTokenSource { get; } = new();
    
    /// <summary>
    /// Gets the remote <see cref="System.Net.IPEndPoint"/>
    /// </summary>
    public IPEndPoint EndPoint { get; }

    /// <summary>
    /// Gets the message context
    /// </summary>
    /// <exception cref="NullReferenceException"><see cref="ParallelTCP.ClientSide.Client.Context"/> isn't initialized.</exception>
    public MessageContext MessageContext =>
        Context ?? throw new NullReferenceException("the message context isn't initialized.");

    /// <summary>
    /// Start receiving messages.
    /// </summary>
    public async Task RunAsync()
    {
        var client = new TcpClient();
        await client.ConnectAsync(EndPoint.Address, EndPoint.Port);
        Context = new MessageContext(Guid.NewGuid(), client, new object(), ShutdownTokenSource.Token);
        await Context.RunAsync();
    }

    /// <summary>
    /// Disconnects and Disposes this <see cref="ParallelTCP.ClientSide.Client"/> instance and requests that the
    /// underlying TCP connection be closed.
    /// </summary>
    public async Task ShutdownAsync()
    {
        ShutdownTokenSource.Cancel();
        ShutdownTokenSource.Dispose();
        if (Context is not null)
            await Context.DisconnectAsync();
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