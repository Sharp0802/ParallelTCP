using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using ParallelTCP.Common;

namespace ParallelTCP.Shared;

public sealed class MessageContext : IAsyncDisposable, IDisposable
{
    public MessageContext(Guid guid, TcpClient client, object lockHandle, CancellationToken token)
    {
        Guid = guid;
        LockHandle = lockHandle;
        
        Client = client;
        Stream = Client.GetStream();
        RemoteEndpoint = Client.Client.RemoteEndPoint;
        LocalEndpoint = Client.Client.LocalEndPoint;
        
        Token = token;
    }

    private object LockHandle { get; }
    private CancellationToken Token { get; }
    private bool Disposed { get; set; }

    private TcpClient Client { get; }
    private NetworkStream Stream { get; set; }
    
    public Guid Guid { get; }
    
    public EndPoint? RemoteEndpoint { get; }
    public EndPoint? LocalEndpoint { get; }
    public bool Connected
    {
        get
        {
            lock (LockHandle) return Client.Connected;
        }
    }

    private ConcurrentDictionary<Guid, MessageChannel> Channels { get; } = new();

    private event NetworkMessageEventHandler? MessageReceived;

    public event NetworkConnectionEventHandler? Disconnecting;

    private MessageChannel AllocChannel(Guid guid)
    {
        MessageChannel channel;
        lock (LockHandle)
        {
            if (Stream is null)
                throw new ObjectDisposedException(nameof(Stream), "the network stream has already been disposed.");
            channel = new MessageChannel(guid, Stream, LockHandle);
        }
        if (!Channels.TryAdd(guid, channel))
            throw new AmbiguousMatchException("guid is overlapped");
        MessageReceived += (_, args) => args.Message.Header.ChannelGuid == guid 
            ? channel.OnMessageReceived(new SharedMessage(args.Message.Header.SharedHeader, args.Message.Content))
            : Task.CompletedTask;
        return channel;
    }

    public Task<MessageChannel> GetChannelAsync()
    {
        return Task.FromResult(Channels.GetOrAdd(Guid.NewGuid(), AllocChannel));
    }

    public async Task RunAsync()
    {
        var tasks = new List<Task>();
        await Task.Factory.StartNew(() =>
        {
            while (!Token.IsCancellationRequested && !Disposed)
            {
                NetworkMessage? msg;
                try
                {
                    if (!Stream.TryReadNetworkMessage(LockHandle, out msg)) throw new IOException();
                }
                catch (IOException)
                {
                    break;
                }

                tasks.Add(MessageReceived.InvokeAsync(this, new NetworkMessageEventArgs(msg!)));
            }
        }, TaskCreationOptions.None);
        await Task.WhenAll(tasks.ToArray());
        await DisconnectAsync();
    }

    public async Task DisconnectAsync()
    {
        lock (LockHandle)
        {
            if (Disposed) return;
            Client.Close();
            Disposed = true;
        }

        await Disconnecting.InvokeAsync(this, new NetworkConnectionEventArgs(this));
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
    }

    public void Dispose()
    {
        DisconnectAsync().Wait(CancellationToken.None);
    }
}