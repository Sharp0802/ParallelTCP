using System.Runtime.CompilerServices;

namespace ParallelTCP.Shared.Messages;

[type: CLSCompliant(true), NativeCppClass, Serializable]
public struct NetworkMessageHeader
{
    public Guid ChannelGuid;
    public SharedMessageHeader SharedHeader;
}