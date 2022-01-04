namespace ParallelTCP.Shared.Messages;

public class NetworkMessage : IMessage
{
    internal NetworkMessage(NetworkMessageHeader header, byte[] content)
    {
        Header = header;
        Content = content;
    }
    
    /// <summary>
    /// Gets the header
    /// </summary>
    public NetworkMessageHeader Header { get; }
    public byte[] Content { get; }

    public byte[] ToBytes()
    {
        unsafe
        {
            var buffer = new byte[sizeof(NetworkMessageHeader) + Content.Length];
            var header = Header;
            var pHeader = &header;
            fixed (byte* pBuffer = buffer)
                Buffer.MemoryCopy(pHeader, pBuffer, buffer.Length, sizeof(NetworkMessageHeader));
            Buffer.BlockCopy(Content, 0, buffer, sizeof(NetworkMessageHeader), Content.Length);
            return buffer;
        }
    }
}