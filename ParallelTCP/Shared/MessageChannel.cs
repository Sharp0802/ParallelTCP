using System.Collections.Concurrent;
using ParallelTCP.Common;

namespace ParallelTCP.Shared;

public class MessageChannel : IDisposable
{
    public MessageChannel(Guid id)
    {
        StreamId = id;
    }

    private Guid StreamId { get; }

    private ConcurrentQueue<SharedMessage> MessageQueue { get; } = new();

    public event SharedMessageEventHandler? MessageReceived;
    public event SharedMessageEventHandler? MessageTransmitted;
    public event CommonEventHandler? Disposing;

    internal Task OnTransmitted(SharedMessage msg)
    {
        return MessageTransmitted.InvokeAsync(this, new SharedMessageEventArgs(StreamId, msg));
    }

    internal Task OnReceived(SharedMessage msg)
    {
        return MessageReceived.InvokeAsync(this, new SharedMessageEventArgs(StreamId, msg));
    }

    public Task<SharedMessage?> TransmitToReceive(SharedMessage msg)
    {
        var result = default(SharedMessage);
        var waiter = new AutoResetEvent(false);

        Task Handler(object? sender, SharedMessageEventArgs args)
        {
            return Task.Factory.StartNew(() =>
            {
                try
                {
                    if (!args.SharedMessage.Header.ReplyTo.Equals(msg.Header.MessageId)) return;
                    result = args.SharedMessage;
                    waiter.Set();
                    waiter.Close();
                    waiter.Dispose();
                    MessageReceived -= Handler;
                }
                catch (Exception)
                {
#if DEBUG
                    throw;
#endif
                }
            });
        }

        MessageReceived += Handler;
        return QueueMessage(msg).ContinueWith(_ =>
        {
            if (!waiter.WaitOne())
                throw new InvalidOperationException("failed to wait one via AutoResetEvent");
            return result;
        });
    }

    public Task QueueMessage(SharedMessage msg)
    {
        var waiter = new AutoResetEvent(false);

        Task TransmitChecker(object? sender, SharedMessageEventArgs args)
        {
            return Task.Factory.StartNew(() =>
            {
                if (!args.SharedMessage.Header.MessageId.Equals(msg.Header.MessageId)) return;
                try
                {
                    waiter.Set();
                    waiter.Close();
                    waiter.Dispose();
                    MessageTransmitted -= TransmitChecker;
                }
                catch (Exception)
                {
#if DEBUG
                    throw;
#endif
                }
            });
        }

        MessageTransmitted += TransmitChecker;
        return Task.Factory.StartNew(() =>
        {
            MessageQueue.Enqueue(msg);
            if (!waiter.WaitOne())
                throw new InvalidOperationException("failed to wait one via AutoResetEvent");
        });
    }

    public void Dispose()
    {
        Disposing.InvokeAsync(this, new CommonEventArgs()).Wait();
    }
}