using System.Runtime.CompilerServices;

namespace ParallelTCP.Common;

[type: CLSCompliant(true), NativeCppClass, Serializable]
public struct MessageHeader
{
    public Guid MessageId;
    public Guid ReplyTo;
    public int Length;
}