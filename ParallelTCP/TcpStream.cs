using System.Net.Sockets;

namespace ParallelTCP;

public class TcpStream
{
    public TcpStream(TcpClient client)
    {
        Client = client;
        Stream = Client.GetStream();
    }

    private TcpClient Client { get; }
    private NetworkStream Stream { get; }

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

    private bool TryReadUnsafe(int count, out byte[]? dst)
    {
        try
        {
            dst = new byte[count];
            for (var i = 0; i < count;)
                i += Stream.Read(dst, i, count - i);
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
}