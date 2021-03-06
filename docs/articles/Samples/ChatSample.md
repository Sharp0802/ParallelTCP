# Chatting Via Console

This sample provides simple console chat app.

Recommended encoding is the UTF-16 format using the little endian byte order.
Use `Encoding.Unicode` to use UTF-16 little endian.

```c#
// sample code of chat client
private static async Task Main()
{
    Console.InputEncoding = Encoding.Unicode;
    Console.OutputEncoding = Encoding.Unicode;
    
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
        Console.WriteLine(Encoding.Unicode.GetString(args.SharedMessage.Content));
        return Task.CompletedTask;
    };
    
    while (true)
    {
        try
        {
            var line = Console.ReadLine() ?? string.Empty;
            if (line == ":q") throw new IOException();
            await channel.SendAsync(new SharedMessage(Guid.Empty, Encoding.Unicode.GetBytes(line)));
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
```

```c#
// sample code of chat server
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
```