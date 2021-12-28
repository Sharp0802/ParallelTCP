namespace ParallelTCP.Common;

public static class Serializer
{
    public static bool TryReadUnsafe<T>(this Stream stream, out T? dst) where T : unmanaged
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

    public static bool TryReadUnsafe(this Stream stream, int length, out byte[]? dst)
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

    public static bool TryWriteUnsafe<T>(this Stream stream, T src) where T : unmanaged
    {
        try
        {
            unsafe
            {
                var buffer = new byte[sizeof(T)];
                fixed (byte* pBuffer = buffer)
                    Buffer.MemoryCopy(&src, pBuffer, sizeof(T), sizeof(T));
                stream.Write(buffer, 0, buffer.Length);
            }

            return true;
        }
        catch (Exception e)
        {
            if (e is not (IOException or ObjectDisposedException)) throw;
            return false;
        }
    }

    public static bool TryWriteUnsafe(this Stream stream, byte[] src)
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
}