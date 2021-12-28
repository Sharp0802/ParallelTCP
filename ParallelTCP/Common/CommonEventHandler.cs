namespace ParallelTCP.Common;

public delegate Task CommonEventHandler(object? sender);

public class CommonEventArgs : EventArgs
{
}