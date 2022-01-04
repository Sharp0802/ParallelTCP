namespace ParallelTCP.Shared.Messages;

public interface IMessage
{
    /// <summary>
    /// Gets the content
    /// </summary>
    public byte[] Content { get; }
    
    /// <summary>
    /// Serialize this into <see cref="byte"/>[]
    /// </summary>
    /// <returns>the serialization result</returns>
    public byte[] ToBytes();
}