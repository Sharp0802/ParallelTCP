namespace ParallelTCP;

public class Message
{
    public Message(Guid replyTo, int length, bool allocContent)
    {
        Header = new MessageHeader
        {
            Length = length,
            MessageId = Guid.NewGuid(),
            ReplyTo = replyTo
        };
        if (allocContent)
            Content = new byte[length];
    }
    
    public MessageHeader Header { get; }
    public byte[] Content { get; }

    public byte[] ToBytes()
    {
        byte[] buffer;
        unsafe
        {
            var length = sizeof(MessageHeader) + Content.Length;
            buffer = new byte[length];
            var header = Header;
            var pHeader = &header;
            fixed (byte* pBuffer = buffer)
                Buffer.MemoryCopy(pHeader, pBuffer, length, sizeof(MessageHeader));
            Buffer.BlockCopy(Content, 0, buffer, sizeof(MessageHeader), Content.Length);
        }

        return buffer;
    }
}