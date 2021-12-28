namespace ParallelTCP.Shared;

public interface IMessage
{
    public byte[] Content { get; }
    
    public byte[] ToBytes();
}