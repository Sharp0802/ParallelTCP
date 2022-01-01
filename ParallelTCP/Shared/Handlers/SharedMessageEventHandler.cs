using ParallelTCP.Shared.Messages;

namespace ParallelTCP.Shared.Handlers;

public delegate Task SharedMessageEventHandler(object? sender, SharedMessageEventArgs args);

public class SharedMessageEventArgs : EventArgs
{
    public SharedMessageEventArgs(Guid channelGuid, SharedMessage sharedMessage)
    {
        ChannelGuid = channelGuid;
        SharedMessage = sharedMessage;
    }

    public Guid ChannelGuid { get; }
    public SharedMessage SharedMessage { get; }
}