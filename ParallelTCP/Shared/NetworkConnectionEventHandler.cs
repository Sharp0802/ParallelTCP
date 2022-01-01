using ParallelTCP.ServerSide;

namespace ParallelTCP.Shared;


public delegate Task NetworkConnectionEventHandler(object? sender, NetworkMessageEventArgs args);

public class NetworkConnectionEventArgs : EventArgs
{
    public NetworkConnectionEventArgs(MessageContext context)
    {
        Context = context;
    }

    public MessageContext Context { get; }
}