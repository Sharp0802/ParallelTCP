namespace ParallelTCP.Shared.Messages;

public interface IMessage
{
    /// <summary>
    /// Content of this message
    /// </summary>
    public byte[] Content { get; }
    
    /// <summary>
    /// Serialize this message into a byte array
    /// </summary>
    /// <returns>the serialization result of this message</returns>
    public byte[] ToBytes();
}