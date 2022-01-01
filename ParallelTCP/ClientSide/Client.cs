using System.Net;
using System.Net.Sockets;
using ParallelTCP.Shared;

namespace ParallelTCP.ClientSide;

public class Client : IAsyncDisposable
{
    /// <summary>
    /// Create new <see cref="ParallelTCP.ClientSide.Client"/>
    /// </summary>
    /// <param name="endpoint">The <see cref="System.Net.EndPoint"/> to connect</param>
    public Client(IPEndPoint endpoint)
    {
        RemoteEndPoint = endpoint;
    }

    private Task Runner { get; set; } = Task.CompletedTask;
    
    public IPEndPoint RemoteEndPoint { get; }
    public MessageContext? MessageContext { get; private set; }

    public async Task OpenAsync()
    {
        var client = new TcpClient();
        await client.ConnectAsync(RemoteEndPoint.Address, RemoteEndPoint.Port);
        MessageContext = new MessageContext(Guid.Empty, client, new object(), CancellationToken.None);
        Runner = MessageContext.RunAsync();
    }

    public async Task ShutdownAsync()
    {
        if (MessageContext is not null)
            await MessageContext.DisconnectAsync();
        await Runner;
    }

    public async ValueTask DisposeAsync()
    {
        await ShutdownAsync();
    }

    public void Dispose()
    {
        ShutdownAsync().Wait();
    }
}