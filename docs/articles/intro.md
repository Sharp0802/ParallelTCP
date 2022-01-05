# Introduction

ParallelTCP has asynchornously implemented all APIs.

If you know & can use async/await,
The code style is rather easy to follow.

The main objects in ParallelTCP are the following:

- [MessageChannel](../api/ParallelTCP.Shared.MessageChannel.html) provides the message sending method & receiving event
- [MessageContext](../api/ParallelTCP.Shared.MessageContext.html), the manager of channels provides the message channel allocation method & automates managing many message channels
- [Client](../api/ParallelTCP.ClientSide.Client.html), the shallow wrapper of MessageContext automates managing a message context
- [Server](../api/ParallelTCP.ServerSide.Server.html), the listener of Client provides connection event & automates managing message contexts
- [SharedMessage](../api/ParallelTCP.Shared.SharedMessage.html) is implementation of IMessage used for the message channel
- [NetworkMessage](../api/ParallelTCP.Shared.NetworkMessage.html), the another implementation of IMessage used for TCP communicating