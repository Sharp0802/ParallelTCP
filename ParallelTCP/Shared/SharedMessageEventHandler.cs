namespace ParallelTCP.Shared;

public delegate Task SharedMessageEventHandler(object? sender, SharedMessageEventArgs args);

public class SharedMessageEventArgs : EventArgs
{
    public SharedMessageEventArgs(Guid streamId, SharedMessage sharedMessage)
    {
        StreamId = streamId;
        SharedMessage = sharedMessage;
    }

    public Guid StreamId { get; }
    public SharedMessage SharedMessage { get; }
}