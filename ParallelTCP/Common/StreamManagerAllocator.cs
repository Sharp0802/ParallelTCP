using Microsoft.IO;

namespace ParallelTCP.Common;

public static class StreamManagerAllocator
{
    public static RecyclableMemoryStreamManager Allocate() => Allocate(1024, 1024 * 1024);
    
    public static RecyclableMemoryStreamManager Allocate(int blockSize, int largeBufferMultiple)
    {
        var maxBufferSize = 16 * largeBufferMultiple;
        return new RecyclableMemoryStreamManager(blockSize, largeBufferMultiple, maxBufferSize)
        {
            GenerateCallStacks = true,
            AggressiveBufferReturn = true,
            MaximumFreeLargePoolBytes = maxBufferSize * 4,
            MaximumFreeSmallPoolBytes = 100 * blockSize
        };
    }
}