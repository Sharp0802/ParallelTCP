namespace ParallelTCP.Shared.Messages;

public class SharedMessage : IMessage
{
    /// <summary>
    /// Allocate new <see cref="SharedMessage"/>
    /// </summary>
    /// <param name="replyTo"><see cref="System.Guid"/> identifier of the message to reply</param>
    /// <param name="content">the content of a message</param>
    public SharedMessage(Guid replyTo, byte[] content)
    {
        Header = new SharedMessageHeader
        {
            Length = content.Length,
            MessageId = Guid.NewGuid(),
            ReplyTo = replyTo
        };
        Content = content;
    }

    internal SharedMessage(SharedMessageHeader header, byte[] content)
    {
        Header = header;
        Content = new byte[content.Length];
        Buffer.BlockCopy(content, 0, Content, 0, content.Length);
    }
    
    /// <summary>
    /// Gets the header
    /// </summary>
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