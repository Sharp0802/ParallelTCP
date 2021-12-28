using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using Microsoft.IO;
using ParallelTCP.Common;
using ParallelTCP.Shared;

namespace ParallelTCP.ClientSide;

public class Client
{
    private IPEndPoint RemoteEndpoint { get; }

    private TcpClient? TcpClient { get; set; }
    private NetworkStream? NetworkStream { get; set; }

    private static RecyclableMemoryStreamManager StreamManager { get; } = StreamManagerAllocator.Allocate();
    private ConcurrentDictionary<Guid, MessageChannel> Channels { get; } = new();
    private ConcurrentQueue<NetworkMessage> MessageQueue { get; } = new();

    public event NetworkMessageEventHandler? MessageReceived;
    public event NetworkMessageEventHandler? MessageTransmitted;

    public async Task RunAsync(CancellationToken token)
    {
        await InitializeAsync();
        await Task.WhenAll(Transmitter(token), Receiver(token));
    }
    
    private async Task InitializeAsync()
    {
        TcpClient = new TcpClient();
        await TcpClient.ConnectAsync(RemoteEndpoint.Address, RemoteEndpoint.Port);
        NetworkStream = TcpClient.GetStream();
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
                    throw new InvalidOperationException("failed to write a message into stream");
                MessageTransmitted.InvokeAsync(this, new NetworkMessageEventArgs(msg));
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
                    throw new InvalidOperationException("failed to read a message from stream");
                MessageReceived.InvokeAsync(this,
                    new NetworkMessageEventArgs(new NetworkMessage(header.Value, content!)));
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
        return Channels.GetOrAdd(guid, id => AllocChannel(id));
    }
}