using System.Collections.Concurrent;

namespace ParallelTCP;

public class TcpStream
{
    public TcpStream(Stream stream)
    {
        Stream = stream;
    }
    
    private Stream Stream { get; }

    private ConcurrentQueue<Message> MessageQueue { get; } = new();

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

    private Task Transmitter(CancellationToken token, TimeSpan frequency)
    {
        return Task.Factory.StartNew(() =>
        {
            while (!token.IsCancellationRequested)
            {
                if (!MessageQueue.TryDequeue(out var msg))
                {
                    Task.Delay(frequency, token).Wait(token);
                    continue;
                }
                if (!TryWriteUnsafe(msg.ToBytes()))
                    throw new InvalidOperationException("failed to write a message into stream");
            }
        }, TaskCreationOptions.None);
    }

    private Task Receiver(CancellationToken token)
    {
        return Task.Factory.StartNew(() =>
        {
            while (!token.IsCancellationRequested)
            {
                if (!TryReadUnsafe<MessageHeader>(out var header) || !TryReadUnsafe(header!.Value.Length, out var content))
                    throw new InvalidOperationException("failed to read a message from stream");
                var msg = new Message(header.Value, content!);
            }
        }, TaskCreationOptions.None);
    }
}