using System.Net;

namespace ParallelTCP.Shared;


public delegate Task NetworkConnectionEventHandler(object? sender, NetworkMessageEventArgs args);

public class NetworkConnectionEventArgs : EventArgs
{
    public NetworkConnectionEventArgs(IPEndPoint endpoint)
    {
        Endpoint = endpoint;
    }

    public IPEndPoint Endpoint { get; }
}