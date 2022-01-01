using System.Runtime.CompilerServices;

namespace ParallelTCP.Shared.Messages;

[type: CLSCompliant(true), NativeCppClass, Serializable]
public struct SharedMessageHeader
{
    public Guid MessageId;
    public Guid ReplyTo;
    public int Length;
}