namespace ParallelTCP.Shared;

public struct MessageTransmitOptions
{
    /// <summary>
    /// Gets a value indicating whether to wait for a reply message about this message.
    /// </summary>
    /// <returns> true if the command should wait for a reply message about message. otherwise, false.</returns>
    public bool WaitForReply { get; set; }
    
    /// <summary>
    /// Gets the waiting timeout
    /// </summary>
    public TimeSpan WaitingTimeout { get; set; }
}