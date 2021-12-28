namespace ParallelTCP.Common;

public delegate Task MessageEventHandler(object? sender, MessageEventArgs args);

public class MessageEventArgs : EventArgs
{
    public MessageEventArgs(Guid streamId, Message message)
    {
        StreamId = streamId;
        Message = message;
    }

    public Guid StreamId { get; }
    public Message Message { get; }
}