# ParallelTCP

![](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=c-sharp&logoColor=white)
[![](https://img.shields.io/badge/NuGet-004880?style=for-the-badge&logo=nuget&logoColor=white)](https://www.nuget.org/packages/ParallelTCP/)

### Language

- [English](./index.md)
- [한국어](./index.ko-kr.md)

## Introduce

ParallelTCP는 간단한 TCP 클라이언트와 서버, 메시지 채널을 제공합니다. 

## Features

- TCP 서버/클라이언트
- 비동기 메시지 채널
- 높은 수준의 추상화/비동기성

## Installation

### Package Manager

```shell
Install-Package ParallelTCP -Version 1.0.3.4
```

### .NET CLI

```shell
dotnet add package ParallelTCP --version 1.0.3.4
```

### PackageReference

```xml
<PackageReference Include="ParallelTCP" Version="1.0.3.4" />
```

## Usage

추천하는 인코딩은 리틀 엔디안 UTF-16입니다.
간단히 `Encoding.Unicode`를 사용하시면 됩니다.

```c#
// 클라이언트 측 샘플 코드
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
// 서버 측 샘플 코드
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

전체 코드를 확인하고 싶으시다면,
[클라이언트](./ParallelTCP.Test.Client/Program.cs)와
[서버](./ParallelTCP.Test.Server/Program.cs)를 확인하십시오.