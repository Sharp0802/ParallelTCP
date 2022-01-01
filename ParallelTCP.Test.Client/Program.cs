using System.Net;
using System.Text;
using ParallelTCP.Shared.Messages;

namespace ParallelTCP.Test.Client;

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
        await using var client = new ClientSide.Client(new IPEndPoint(IPAddress.Any, port));
        await client.OpenAsync();
        var channel = await client.MessageContext!.GetChannelAsync(Guid.Empty);
        channel.MessageReceived += (_, args) =>
        {
            Console.WriteLine(Encoding.UTF8.GetString(args.SharedMessage.Content));
            return Task.CompletedTask;
        };
        while (true)
        {
            var line = Console.ReadLine() ?? string.Empty;
            if (line == ":q")
            {
                await client.ShutdownAsync();
                break;
            }

            await channel.SendAsync(new SharedMessage(Guid.Empty, Encoding.UTF8.GetBytes(line)));
        }
    }
}