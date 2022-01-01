namespace ParallelTCP.Common;

public static class AsyncEvent
{
    internal static Task InvokeAsync<TEventArgs>(this Delegate? @event, object? sender, TEventArgs args) where TEventArgs : EventArgs
    {
        return @event is null 
            ? Task.CompletedTask 
            : Task.WhenAll(@event.GetInvocationList().Select(d => (Task) d.DynamicInvoke(sender, args)));
    }
}