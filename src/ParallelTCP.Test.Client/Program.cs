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
        Console.WriteLine("connection established.");
        var channel = await client.MessageContext!.GetChannelAsync(Guid.Empty);
        channel.MessageReceived += (_, args) =>
        {
            Console.WriteLine(Encoding.Default.GetString(Encoding.Convert(Encoding.UTF8, Encoding.Default, args.SharedMessage.Content)));
            return Task.CompletedTask;
        };
        while (true)
        {
            try
            {
                var line = Console.ReadLine() ?? string.Empty;
                if (line == ":q") throw new IOException();
                await channel.SendAsync(new SharedMessage(Guid.Empty, Encoding.Convert(Encoding.Default, Encoding.UTF8, Encoding.Default.GetBytes(line))));
            }
            catch (IOException)
            {
                await client.ShutdownAsync();
                Console.WriteLine("disconnected.");
                break;
            }
        }

        Console.ReadKey();
    }
}