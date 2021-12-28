using System.Collections.Concurrent;

namespace ParallelTCP;

public class TcpChannel
{
    public TcpChannel(Guid id, Stream stream)
    {
        StreamId = id;
        Stream = stream;
    }

    private Guid StreamId { get; }
    private Stream Stream { get; }

    private ConcurrentQueue<Message> MessageQueue { get; } = new();

    public event MessageEventHandler? MessageReceived;
    public event MessageEventHandler? MessageTransmitted;

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
                MessageTransmitted.InvokeAsync(this, new MessageEventArgs(StreamId, msg));
            }
        }, TaskCreationOptions.None);
    }

    private Task Receiver(CancellationToken token)
    {
        return Task.Factory.StartNew(() =>
        {
            while (!token.IsCancellationRequested)
            {
                if (!TryReadUnsafe<MessageHeader>(out var header) ||
                    !TryReadUnsafe(header!.Value.Length, out var content))
                    throw new InvalidOperationException("failed to read a message from stream");
                MessageReceived.InvokeAsync(this, new MessageEventArgs(StreamId, new Message(header.Value, content!)));
            }
        }, TaskCreationOptions.None);
    }

    public Task RunAsync(CancellationToken token) => Task.WhenAll(Transmitter(token), Receiver(token));

    public Task<Message?> TransmitToReceive(Message msg)
    {
        var result = default(Message);
        var waiter = new AutoResetEvent(false);

        Task Handler(object? sender, MessageEventArgs args)
        {
            return Task.Factory.StartNew(() =>
            {
                try
                {
                    result = args.Message;
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

    public Task QueueMessage(Message msg)
    {
        var waiter = new AutoResetEvent(false);

        Task TransmitChecker(object? sender, MessageEventArgs args)
        {
            return Task.Factory.StartNew(() =>
            {
                if (!args.Message.Header.MessageId.Equals(msg.Header.MessageId)) return;
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