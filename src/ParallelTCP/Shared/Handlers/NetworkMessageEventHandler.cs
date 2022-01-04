using ParallelTCP.Shared.Messages;

namespace ParallelTCP.Shared.Handlers;

public delegate Task NetworkMessageEventHandler(object? sender, NetworkMessageEventArgs args);

public class NetworkMessageEventArgs : EventArgs
{
    public NetworkMessageEventArgs(NetworkMessage message)
    {
        Message = message;
    }

    public NetworkMessage Message { get; }
}