namespace Breakout.Events;

// Power-up release event
public record PowerUpReleasedEvent(int BrickIndex) : IGameEvent;

// Note: PowerUpActivatedEvent, PowerUpExpiredEvent, and PowerUpCollectedEvent 
// have been moved to GameEvents.cs
