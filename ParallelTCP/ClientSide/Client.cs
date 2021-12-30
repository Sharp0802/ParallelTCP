using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using ParallelTCP.Common;
using ParallelTCP.Shared;

namespace ParallelTCP.ClientSide;

public class Client : IDisposable
{
    public Client(IPEndPoint remoteEndpoint)
    {
        RemoteEndpoint = remoteEndpoint;
    }

    private IPEndPoint RemoteEndpoint { get; }

    private TcpClient? TcpClient { get; set; }
    private NetworkStream? NetworkStream { get; set; }

    private ConcurrentDictionary<Guid, MessageChannel> Channels { get; } = new();
    private ConcurrentQueue<NetworkMessage> MessageQueue { get; } = new();
    private List<Task> TaskList { get; } = new();
    private object TaskListLocker { get; } = new();

    public event NetworkMessageEventHandler? MessageReceived;
    public event NetworkMessageEventHandler? MessageTransmitted;
    public event NetworkConnectionEventHandler? ServerDisconnected;
    public event NetworkConnectionEventHandler? ServerConnected;

    private void RegisterTask(Func<Task> task)
    {
        lock (TaskListLocker) TaskList.Add(task.Invoke());
    }

    public Task RunAsync(CancellationToken token)
    {
        return Task.WhenAll(Transmitter(token), Receiver(token));
    }
    
    public async Task ConnectAsync()
    {
        TcpClient = new TcpClient();
        await TcpClient.ConnectAsync(RemoteEndpoint.Address, RemoteEndpoint.Port);
        NetworkStream = TcpClient.GetStream();
        RegisterTask(() => ServerConnected.InvokeAsync(this, new NetworkConnectionEventArgs(RemoteEndpoint)));
    }

    private Task Transmitter(CancellationToken token)
    {
        if (NetworkStream is null) throw new NullReferenceException("network stream is null");
        return Task.Factory.StartNew(() =>
        {
            while (!token.IsCancellationRequested)
            {
                if (!MessageQueue.TryDequeue(out var msg)) continue;
                if (!NetworkStream.TryWriteUnsafe(msg.ToBytes()))
                {
                    RegisterTask(() => ServerDisconnected.InvokeAsync(this, new NetworkConnectionEventArgs(RemoteEndpoint)));
                    break;
                }
                RegisterTask(() => MessageTransmitted.InvokeAsync(this, new NetworkMessageEventArgs(msg)));
            }
        }, TaskCreationOptions.None);
    }

    private Task Receiver(CancellationToken token)
    {
        if (NetworkStream is null) throw new NullReferenceException("network stream is null");
        return Task.Factory.StartNew(() =>
        {
            while (!token.IsCancellationRequested)
            {
                if (!NetworkStream.TryReadUnsafe<NetworkMessageHeader>(out var header) ||
                    !NetworkStream.TryReadUnsafe(header!.Value.SharedHeader.Length, out var content))
                {
                    RegisterTask(() => ServerDisconnected.InvokeAsync(this, new NetworkConnectionEventArgs(RemoteEndpoint)));
                    break;
                }
                RegisterTask(() => MessageReceived.InvokeAsync(this,
                    new NetworkMessageEventArgs(new NetworkMessage(header.Value, content!))));
            }
        }, TaskCreationOptions.None);
    }

    private MessageChannel AllocChannel(Guid guid)
    {
        var channel = new MessageChannel(guid);
        if (!Channels.TryAdd(guid, channel))
            throw new InvalidOperationException("failed to assign a channel in dictionary");

        async Task OnReceived(object? sender, NetworkMessageEventArgs args)
        {
            if (args.Message.Header.StreamId.Equals(guid)) 
                await channel.OnReceived(new SharedMessage(args.Message.Header.SharedHeader, args.Message.Content));
        }

        async Task OnTransmitted(object? sender, NetworkMessageEventArgs args)
        {
            if (args.Message.Header.StreamId.Equals(guid)) 
                await channel.OnTransmitted(new SharedMessage(args.Message.Header.SharedHeader, args.Message.Content));
        }

        MessageReceived += OnReceived;
        MessageTransmitted += OnTransmitted;
        channel.Disposing += _ =>
        {
            MessageReceived -= OnReceived;
            MessageTransmitted -= OnTransmitted;
            Channels.TryRemove(guid, out var _);
            return Task.CompletedTask;
        };

        return channel;
    }

    public MessageChannel GetChannel(Guid guid)
    {
        return Channels.GetOrAdd(guid, AllocChannel);
    }

    [method: MethodImpl(MethodImplOptions.Synchronized)]
    public async Task EnsureDisconnectAsync()
    {
        await Task.WhenAll(TaskList.ToArray());
        NetworkStream?.Dispose();
        NetworkStream = null;
        TcpClient?.Close();
        TcpClient?.Dispose();
        TcpClient = null;
    }
    
    public void Dispose()
    {
        EnsureDisconnectAsync().Wait();
    }
}