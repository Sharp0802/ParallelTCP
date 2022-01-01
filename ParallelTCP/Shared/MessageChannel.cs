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
    /// Gets the identifier
    /// </summary>
    public Guid ChannelGuid { get; }
    
    private NetworkStream Stream { get; }
    private object LockHandle { get; }

    /// <summary>
    /// Raised when received a message.
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

    internal event NetworkConnectionEventHandler? MessageSendingFailed;

    internal Task OnMessageReceived(SharedMessage msg)
    {
        return InterMessageReceived.InvokeAsync(this, new SharedMessageEventArgs(ChannelGuid, msg));
    }

    /// <summary>
    /// Send <see cref="ParallelTCP.Shared.Messages.SharedMessage"/> asynchronously
    /// </summary>
    /// <param name="msg">a message to send</param>
    /// <exception cref="IOException">if already disconnected from server/client.</exception>
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
            {
                MessageSendingFailed.InvokeAsync(this, new NetworkConnectionEventArgs(null)).Wait();
                throw new IOException("failed to write a message into the network stream");
            }
        });
    }

    /// <summary>
    /// Send <see cref="ParallelTCP.Shared.Messages.SharedMessage"/> asynchronously
    /// </summary>
    /// <param name="msg">a message to send</param>
    /// <param name="options">message sending options</param>
    /// <exception cref="IOException">if client/server already disconnected from server/client.</exception>
    /// <returns>true if succeed to send and wait for replying message(if turn on in options). otherwise, false.</returns>
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