using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace ParallelTCP.ServerSide;

public class Server
{
    public Server(int port)
    {
        LocalEndpoint = new IPEndPoint(IPAddress.Any, port);
    }

    private IPEndPoint LocalEndpoint { get; }
    
    private TcpListener? TcpListener { get; set; }
    private ConcurrentDictionary<Guid, TcpClient> Clients { get; } = new();

    public Task OpenAsync()
    {
        TcpListener = new TcpListener(LocalEndpoint);
        return Task.CompletedTask;
    }
}