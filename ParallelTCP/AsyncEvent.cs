namespace ParallelTCP;

public static class AsyncEvent
{
    public static Task InvokeAsync<TEventArgs>(this Delegate? @event, object? sender, TEventArgs args)
    {
        return @event is null 
            ? Task.CompletedTask 
            : Task.WhenAll(@event.GetInvocationList().Select(d => (Task) d.DynamicInvoke(sender, args)));
    }
}