using System.Net.Sockets;
using ParallelTCP.Common;
using ParallelTCP.Shared.Handlers;
using ParallelTCP.Shared.Messages;

namespace ParallelTCP.Shared;

public class MessageChannel
{
    internal MessageChannel(Guid channelGuid, NetworkStream stream, object lockHandle)
    {
        ChannelGuid = channelGuid;
        Stream = stream;
        LockHandle = lockHandle;
    }

    /// <summary>
    /// Guid of this channel
    /// </summary>
    public Guid ChannelGuid { get; }
    
    private NetworkStream Stream { get; }
    private object LockHandle { get; }

    /// <summary>
    /// Event handler for message received event
    /// </summary>
    public event SharedMessageEventHandler MessageReceived
    {
        add
        {
            lock (LockHandle)
            {
                InterMessageReceived += value;
            }
        }
        remove
        {
            lock (LockHandle)
            {
                InterMessageReceived -= value;
            }
        }
    }

    private event SharedMessageEventHandler? InterMessageReceived;

    internal Task OnMessageReceived(SharedMessage msg)
    {
        return InterMessageReceived.InvokeAsync(this, new SharedMessageEventArgs(ChannelGuid, msg));
    }

    /// <summary>
    /// Send a message asynchronously with channel
    /// </summary>
    /// <param name="msg">a message to send</param>
    /// <exception cref="IOException">if client/server already disconnected from server/client, then throw this exception.</exception>
    // ReSharper disable once SuggestBaseTypeForParameter
    public Task SendAsync(SharedMessage msg)
    {
        return Task.Factory.StartNew(() =>
        {
            var networkMsg = new NetworkMessage(
                new NetworkMessageHeader
                {
                    SharedHeader = msg.Header, 
                    ChannelGuid = ChannelGuid
                },
                msg.Content);
            if (!Stream.TryWriteMessage(LockHandle, networkMsg))
                throw new IOException("failed to write a message into the network stream");
        });
    }

    /// <summary>
    /// Send a message asynchronously with channel
    /// </summary>
    /// <param name="msg">a message to send</param>
    /// <param name="options">message sending options</param>
    /// <exception cref="IOException">if client/server already disconnected from server/client, then throw this exception.</exception>
    /// <returns>if succeed to send and wait for replying message(if turn on in options), true. otherwise, false.</returns>
    public async Task<bool> SendAsync(SharedMessage msg, MessageTransmitOptions options)
    {
        if (!options.WaitForReply)
        {
            await SendAsync(msg);
            return true;
        }

        using var waiter = new EventWaitHandle(false, EventResetMode.AutoReset);

        Task CheckReply(object? sender, SharedMessageEventArgs args)
        {
            if (args.SharedMessage.Header.ReplyTo == msg.Header.MessageId)
            {
                try
                {
                    // ReSharper disable once AccessToDisposedClosure
                    waiter.Set();
                }
                catch (ObjectDisposedException)
                {
                }

                MessageReceived -= CheckReply;
            }

            return Task.CompletedTask;
        }

        MessageReceived += CheckReply;
        await SendAsync(msg);

        return waiter.WaitOne(options.WaitingTimeout);
    }
}