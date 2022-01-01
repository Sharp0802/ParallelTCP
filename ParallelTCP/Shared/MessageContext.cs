using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using ParallelTCP.Common;

namespace ParallelTCP.Shared;

public sealed class MessageContext : IAsyncDisposable, IDisposable
{
    /// <summary>
    /// Allocate new <see cref="ParallelTCP.Shared.MessageContext"/>
    /// </summary>
    /// <param name="guid"><see cref="System.Guid"/> identifier of MessageContext</param>
    /// <param name="client"><see cref="System.Net.Sockets.TcpClient"/> for this MessageContext</param>
    /// <param name="lockHandle">A lock handle for <see cref="System.Net.Sockets.NetworkStream"/> of client</param>
    /// <param name="token"><see cref="System.Threading.CancellationToken"/> for shutting down</param>
    /// <exception cref="System.InvalidOperationException"><paramref name="client"/> is not connected to a remote host.</exception>
    /// <exception cref="System.ObjectDisposedException"><paramref name="client"/> has been closed.</exception>
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

    /// <summary>
    /// Gets the identifier
    /// </summary>
    public Guid Guid { get; }

    /// <summary>
    /// Gets the remote endpoint
    /// </summary>
    /// <returns>The <see cref="System.Net.EndPoint"/> with which the <see cref="ParallelTCP.Shared.MessageContext"/>
    /// communicating.</returns>
    public EndPoint? RemoteEndpoint { get; }

    /// <summary>
    /// Gets the local endpoint
    /// </summary>
    /// <returns>The <see cref="System.Net.EndPoint"/> that the <see cref="ParallelTCP.Shared.MessageContext"/> is using
    /// for communications.</returns>
    public EndPoint? LocalEndpoint { get; }

    /// <summary>
    /// Gets a value indicating whether the underlying <see cref="System.Net.Sockets.TcpClient"/> for a
    /// <see cref="ParallelTCP.Shared.MessageContext"/> is connected to a remote host.
    /// </summary>
    /// <returns>true if the <see cref="ParallelTCP.Shared.MessageContext.Client"/> tcp client was connected to a remote
    /// resource as of the most recent operation; otherwise, false</returns>
    public bool Connected
    {
        get
        {
            lock (LockHandle) return Client.Connected;
        }
    }

    private ConcurrentDictionary<Guid, MessageChannel> Channels { get; } = new();

    private event NetworkMessageEventHandler? MessageReceived;

    /// <summary>
    /// Raised when the <see cref="ParallelTCP.Shared.MessageContext"/> is disconnected.
    /// </summary>
    public event NetworkConnectionEventHandler? Disconnected;

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

    /// <summary>
    /// Allocates and Assign new <see cref="ParallelTCP.Shared.MessageChannel"/>
    /// </summary>
    /// <returns>the requested channel.</returns>
    public Task<MessageChannel> GetChannelAsync()
    {
        return Task.FromResult(Channels.GetOrAdd(Guid.NewGuid(), AllocChannel));
    }

    /// <summary>
    /// Allocate and Assign new <see cref="ParallelTCP.Shared.MessageChannel"/> to the
    /// <see cref="ParallelTCP.Shared.MessageContext"/> with <paramref name="guid"/>. if the <paramref name="guid"/>
    /// does not already exist, or returns the existing <see cref="ParallelTCP.Shared.MessageChannel"/> if the
    /// <paramref name="guid"/> exists.
    /// </summary>
    /// <param name="guid">The guid of the <see cref="ParallelTCP.Shared.MessageChannel"/> to allocate and assign</param>
    /// <returns>the requested channel for the <paramref name="guid"/>.</returns>
    public Task<MessageChannel> GetChannelAsync(Guid guid)
    {
        return Task.FromResult(Channels.GetOrAdd(guid, AllocChannel));
    }

    /// <summary>
    /// Starts receiving messages.
    /// </summary>
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

    /// <summary>
    /// Disconnects and Disposes this <see cref="ParallelTCP.Shared.MessageContext"/> instance and requests that the
    /// underlying TCP connection be closed.
    /// </summary>
    public async Task DisconnectAsync()
    {
        lock (LockHandle)
        {
            if (Disposed) return;
            Client.Close();
            Disposed = true;
        }

        await Disconnected.InvokeAsync(this, new NetworkConnectionEventArgs(this));
    }

    /// <inheritdoc cref="ParallelTCP.Shared.MessageContext.DisconnectAsync()"/>
    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
    }

    /// <inheritdoc cref="ParallelTCP.Shared.MessageContext.DisconnectAsync()"/>
    public void Dispose()
    {
        DisconnectAsync().Wait(CancellationToken.None);
    }
}