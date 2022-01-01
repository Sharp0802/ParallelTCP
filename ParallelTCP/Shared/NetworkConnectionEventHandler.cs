using ParallelTCP.ServerSide;

namespace ParallelTCP.Shared;


public delegate Task NetworkConnectionEventHandler(object? sender, NetworkConnectionEventArgs args);

public class NetworkConnectionEventArgs : EventArgs
{
    public NetworkConnectionEventArgs(MessageContext? context)
    {
        Context = context;
    }

    public MessageContext? Context { get; }
}