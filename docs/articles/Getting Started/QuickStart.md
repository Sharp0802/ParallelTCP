# Quick Start

- Create client
```c#
await using var client = new ClientSide.Client(endpoint);
await client.OpenAsync();
```

- Acquire new message channel & Set message received event
```c#
var channel = await client.MessageContext!.GetChannelAsync(Guid.Empty);
channel.MessageReceived += (sender, args) =>
{
    Console.WriteLine(Encoding.Unicode.GetString(args.SharedMessage.Content));
    return Task.CompletedTask;
};
```

- Send message via message channel
```c#
try
{
    await channel.SendAsync(new SharedMessage(Guid.Empty, Encoding.Unicode.GetBytes(line)));
}
catch (IOException)
{
    await client.ShutdownAsync();
}
```