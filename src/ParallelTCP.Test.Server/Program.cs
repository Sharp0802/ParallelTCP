using ParallelTCP.Shared;
using ParallelTCP.Shared.Handlers;
using ParallelTCP.Shared.Messages;

namespace ParallelTCP.Test.Server;

internal static class Program
{
    private static async Task Main()
    {
        Console.Write("write the port to communicate:");
        if (!int.TryParse(Console.ReadLine() ?? string.Empty, out var port))
        {
            Console.WriteLine("error: cannot parse port number.");
            return;
        }
        await using var server = new ServerSide.Server(port);
        server.ClientConnected += ServerOnClientConnected;
        await server.OpenAsync();
        await server.RunAsync();
    }

    private static List<(MessageContext Context, MessageChannel Channel)> Clients { get; } = new();

    private static async Task ServerOnClientConnected(object? sender, NetworkConnectionEventArgs args)
    {
        if (args.Context is null) return;
        var context = args.Context;
        var channel = await context.GetChannelAsync(Guid.Empty);
        Clients.Add((context, channel));
        context.Disconnected += (_, _) =>
        {
            Clients.Remove((context, channel));
            return Task.CompletedTask;
        };
        channel.MessageReceived += ChannelOnMessageReceived;
    }

    private static async Task ChannelOnMessageReceived(object? sender, SharedMessageEventArgs args)
    {
        foreach (var (context, channel) in Clients)
        {
            try
            {
                await channel.SendAsync(new SharedMessage(Guid.Empty, args.SharedMessage.Content));
            }
            catch (IOException)
            {
                Clients.Remove((context, channel));
                await context.DisconnectAsync();
            }
        }
    }
}