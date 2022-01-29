namespace ParallelTCP.Shared.Handlers;


public delegate Task NetworkConnectionEventHandler(object? sender, NetworkConnectionEventArgs args);

public class NetworkConnectionEventArgs : EventArgs
{
    public NetworkConnectionEventArgs(MessageContext? context)
    {
        Context = context;
    }

    /// <summary>
    /// Gets connected <see cref="MessageContext"/>
    /// </summary>
    public MessageContext? Context { get; }
}