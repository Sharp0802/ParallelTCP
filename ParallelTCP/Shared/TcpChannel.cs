using System.Collections.Concurrent;
using ParallelTCP.Common;

namespace ParallelTCP.Shared;

public class TcpChannel
{
    public TcpChannel(Guid id, Stream stream)
    {
        StreamId = id;
        Stream = stream;
    }

    private Guid StreamId { get; }
    private Stream Stream { get; }

    private ConcurrentQueue<SharedMessage> MessageQueue { get; } = new();

    public event SharedMessageEventHandler? MessageReceived;
    public event SharedMessageEventHandler? MessageTransmitted;

    private bool TryReadUnsafe<T>(out T? dst) where T : unmanaged
    {
        try
        {
            unsafe
            {
                T literalBuffer;
                var pLiteralBuffer = &literalBuffer;
                var buffer = new byte[sizeof(T)];
                for (var i = 0; i < sizeof(T);)
                    i += Stream.Read(buffer, i, sizeof(T) - i);
                fixed (byte* pBuffer = buffer)
                    Buffer.MemoryCopy(pBuffer, pLiteralBuffer, sizeof(T), sizeof(T));
                dst = literalBuffer;
            }

            return true;
        }
        catch (Exception e)
        {
            dst = null;
            if (e is not (IOException or ObjectDisposedException)) throw;
            return false;
        }
    }

    private bool TryReadUnsafe(int length, out byte[]? dst)
    {
        try
        {
            dst = new byte[length];
            for (var i = 0; i < length;)
                i += Stream.Read(dst, i, length - i);
            return true;
        }
        catch (Exception e)
        {
            dst = null;
            if (e is not (IOException or ObjectDisposedException)) throw;
            return false;
        }
    }

    private bool TryWriteUnsafe<T>(T src) where T : unmanaged
    {
        try
        {
            unsafe
            {
                var buffer = new byte[sizeof(T)];
                fixed (byte* pBuffer = buffer)
                    Buffer.MemoryCopy(&src, pBuffer, sizeof(T), sizeof(T));
                Stream.Write(buffer, 0, buffer.Length);
            }

            return true;
        }
        catch (Exception e)
        {
            if (e is not (IOException or ObjectDisposedException)) throw;
            return false;
        }
    }

    private bool TryWriteUnsafe(byte[] src)
    {
        try
        {
            Stream.Write(src, 0, src.Length);
            return true;
        }
        catch (Exception e)
        {
            if (e is not (IOException or ObjectDisposedException)) throw;
            return false;
        }
    }

    private Task Transmitter(CancellationToken token)
    {
        return Task.Factory.StartNew(() =>
        {
            while (!token.IsCancellationRequested)
            {
                if (!MessageQueue.TryDequeue(out var msg)) continue;
                if (!TryWriteUnsafe(msg.ToBytes()))
                    throw new InvalidOperationException("failed to write a message into stream");
                MessageTransmitted.InvokeAsync(this, new SharedMessageEventArgs(StreamId, msg));
            }
        }, TaskCreationOptions.None);
    }

    private Task Receiver(CancellationToken token)
    {
        return Task.Factory.StartNew(() =>
        {
            while (!token.IsCancellationRequested)
            {
                if (!TryReadUnsafe<SharedMessageHeader>(out var header) ||
                    !TryReadUnsafe(header!.Value.Length, out var content))
                    throw new InvalidOperationException("failed to read a message from stream");
                MessageReceived.InvokeAsync(this, new SharedMessageEventArgs(StreamId, new SharedMessage(header.Value, content!)));
            }
        }, TaskCreationOptions.None);
    }

    public Task RunAsync(CancellationToken token) => Task.WhenAll(Transmitter(token), Receiver(token));

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
}