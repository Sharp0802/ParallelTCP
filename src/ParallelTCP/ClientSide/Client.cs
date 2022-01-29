using System.Net;
using System.Net.Sockets;
using ParallelTCP.Shared;

namespace ParallelTCP.ClientSide;

public class Client : IAsyncDisposable, IDisposable
{
    /// <summary>
    /// Create new <see cref="Client"/>
    /// </summary>
    /// <param name="endpoint">The <see cref="EndPoint"/> to connect</param>
    public Client(IPEndPoint endpoint)
    {
        RemoteEndPoint = endpoint;
    }

    private Task Runner { get; set; } = Task.CompletedTask;
    
    /// <summary>
    /// Gets the remote endpoint
    /// </summary>
    /// <returns>The <see cref="EndPoint"/> with which the <see cref="Client"/> communicating.</returns>
    public IPEndPoint RemoteEndPoint { get; }
    
    /// <summary>
    /// Gets the message context
    /// </summary>
    /// <returns>The <see cref="Shared.MessageContext"/> that the
    /// <see cref="ServerSide.Server"/> is using for communications.</returns>
    public MessageContext? MessageContext { get; private set; }

    /// <inheritdoc cref="MessageContext.RunAsync()"/>
    public async Task OpenAsync()
    {
        var client = new TcpClient();
        await client.ConnectAsync(RemoteEndPoint.Address, RemoteEndPoint.Port);
        MessageContext = new MessageContext(Guid.Empty, client, new object(), CancellationToken.None);
        Runner = MessageContext.RunAsync();
    }

    /// <summary>
    /// Disconnects and Disposes this <see cref="Client"/> instance and requests that the
    /// underlying TCP connection be closed.
    /// </summary>
    public async Task ShutdownAsync()
    {
        if (MessageContext is not null)
            await MessageContext.DisconnectAsync();
        await Runner;
    }

    /// <inheritdoc cref="ShutdownAsync()"/>
    public async ValueTask DisposeAsync()
    {
        await ShutdownAsync();
    }

    /// <inheritdoc cref="ShutdownAsync()"/>
    public void Dispose()
    {
        ShutdownAsync().Wait();
    }
}