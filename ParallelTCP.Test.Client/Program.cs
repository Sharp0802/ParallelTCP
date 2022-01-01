using System.Net;
using System.Text;
using ParallelTCP.Shared.Messages;

namespace ParallelTCP.Test.Client;

internal static class Program
{
    private static async Task Main()
    {
        Console.Write("write the endpoint to communicate:");
        if (!IPEndPoint.TryParse(Console.ReadLine() ?? string.Empty, out var endpoint))
        {
            Console.WriteLine("error: cannot parse endpoint.");
            return;
        }
        await using var client = new ClientSide.Client(endpoint);
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