﻿using System.Runtime.CompilerServices;

namespace ParallelTCP.Shared;

[type: CLSCompliant(true), NativeCppClass, Serializable]
public struct NetworkMessageHeader
{
    public Guid StreamId;
    public SharedMessageHeader SharedHeader;
}