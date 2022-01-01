namespace ParallelTCP.Shared;

public struct MessageTransmitOptions
{
    public bool WaitForReply { get; set; }
    public TimeSpan WaitingTimeout { get; set; }
}