namespace ParallelTCP.Shared;

public class SharedMessage
{
    public SharedMessage(Guid replyTo, int length, bool allocContent)
    {
        Header = new SharedMessageHeader
        {
            Length = length,
            MessageId = Guid.NewGuid(),
            ReplyTo = replyTo
        };
        if (allocContent)
            Content = new byte[length];
    }

    internal SharedMessage(SharedMessageHeader header, byte[] content)
    {
        Header = header;
        Content = new byte[content.Length];
        Buffer.BlockCopy(content, 0, Content, 0, content.Length);
    }
    
    public SharedMessageHeader Header { get; }
    public byte[] Content { get; }

    public byte[] ToBytes()
    {
        byte[] buffer;
        unsafe
        {
            var length = sizeof(SharedMessageHeader) + Content.Length;
            buffer = new byte[length];
            var header = Header;
            var pHeader = &header;
            fixed (byte* pBuffer = buffer)
                Buffer.MemoryCopy(pHeader, pBuffer, length, sizeof(SharedMessageHeader));
            Buffer.BlockCopy(Content, 0, buffer, sizeof(SharedMessageHeader), Content.Length);
        }

        return buffer;
    }
}