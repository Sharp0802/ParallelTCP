using ParallelTCP.Shared;
using ParallelTCP.Shared.Messages;

namespace ParallelTCP.Common;

public static class Serializer
{
    private static bool TryReadUnsafe<T>(this Stream stream, out T? dst) where T : unmanaged
    {
        try
        {
            unsafe
            {
                T literalBuffer;
                var pLiteralBuffer = &literalBuffer;
                var buffer = new byte[sizeof(T)];
                for (var i = 0; i < sizeof(T);)
                    i += stream.Read(buffer, i, sizeof(T) - i);
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

    private static bool TryReadUnsafe(this Stream stream, int length, out byte[]? dst)
    {
        try
        {
            dst = new byte[length];
            for (var i = 0; i < length;)
                i += stream.Read(dst, i, length - i);
            return true;
        }
        catch (Exception e)
        {
            dst = null;
            if (e is not (IOException or ObjectDisposedException)) throw;
            return false;
        }
    }

    internal static bool TryReadNetworkMessage(this Stream? stream, object locker, out NetworkMessage? msg)
    {
        msg = null;
        NetworkMessageHeader? header;
        byte[]? content;
        lock (locker)
        {
            if (stream is null ||
                !stream.TryReadUnsafe(out header) || 
                !stream.TryReadUnsafe(header!.Value.SharedHeader.Length, out content))
                return false;
        }
        msg = new NetworkMessage(header.Value, content!);
        return true;
    }

    private static bool TryWriteUnsafe(this Stream stream, byte[] src)
    {
        try
        {
            stream.Write(src, 0, src.Length);
            return true;
        }
        catch (Exception e)
        {
            if (e is not (IOException or ObjectDisposedException)) throw;
            return false;
        }
    }

    internal static bool TryWriteMessage(this Stream? stream, object locker, IMessage msg)
    {
        var bytes = msg.ToBytes();
        lock (locker)
        {
            return stream is not null && TryWriteUnsafe(stream, bytes);
        }
    }
}