namespace Breakout.Events;

// Central event bus for publishing and subscribing to game events
public static class EventBus
{
    // Dictionary of event type to list of handler actions
    private static readonly Dictionary<Type, List<Delegate>> _subscribers = [];

    // Subscribe to an event type with a handler
    public static void Subscribe<T>(Action<T> handler) where T : IGameEvent
    {
        var eventType = typeof(T);
        if (!_subscribers.ContainsKey(eventType))
        {
            _subscribers[eventType] = [];
        }
        
        _subscribers[eventType].Add(handler);
    }

    // Unsubscribe from an event type
    public static void Unsubscribe<T>(Action<T> handler) where T : IGameEvent
    {
        var eventType = typeof(T);
        if (_subscribers.ContainsKey(eventType))
        {
            _subscribers[eventType].Remove(handler);
            
            if (_subscribers[eventType].Count == 0)
            {
                _subscribers.Remove(eventType);
            }
        }
    }

    // Publish an event to all subscribers
    public static void Publish<T>(T gameEvent) where T : IGameEvent
    {
        Console.WriteLine($"Publishing event: {gameEvent.GetType().Name}");

        var eventType = typeof(T);
        if (!_subscribers.ContainsKey(eventType))
        {
            return;
        }

        // Notify all subscribers
        foreach (var handler in _subscribers[eventType].ToList())
        {
            if (handler is Action<T> typedHandler)
            {
                typedHandler(gameEvent);
            }
        }
    }
}
