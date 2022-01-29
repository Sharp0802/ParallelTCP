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

    /// <summary>
    /// Gets caused <see cref="MessageChannel"/>'s identifier
    /// </summary>
    public Guid ChannelGuid { get; }
    
    /// <summary>
    /// Gets caused <see cref="SharedMessage"/>
    /// </summary>
    public SharedMessage SharedMessage { get; }
}