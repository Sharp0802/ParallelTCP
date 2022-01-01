using System.Net;
using System.Net.Sockets;
using ParallelTCP.Shared;

namespace ParallelTCP.ClientSide;

public class Client : IAsyncDisposable, IDisposable
{
    public Client(IPEndPoint endpoint)
    {
        EndPoint = endpoint;
    }

    private MessageContext? Context { get; set; }
    private CancellationTokenSource ShutdownTokenSource { get; } = new();
    
    public IPEndPoint EndPoint { get; }

    public MessageContext MessageContext =>
        Context ?? throw new NullReferenceException("the message context isn't initialized.");

    public async Task RunAsync()
    {
        var client = new TcpClient();
        await client.ConnectAsync(EndPoint.Address, EndPoint.Port);
        Context = new MessageContext(Guid.NewGuid(), client, new object(), ShutdownTokenSource.Token);
        await Context.RunAsync();
    }

    public async Task ShutdownAsync()
    {
        ShutdownTokenSource.Cancel();
        ShutdownTokenSource.Dispose();
        if (Context is not null)
            await Context.DisconnectAsync();
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