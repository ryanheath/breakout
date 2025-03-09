namespace Breakout.Events;

// Events related to bricks
public record BrickHitEvent(Brick Brick, bool Destroyed) : IGameEvent;

public record AllBricksDestroyedEvent(int LevelNumber) : IGameEvent;

// New event for explosive bricks
public record ExplosiveBrickDetonatedEvent(Brick Brick, List<Brick> AffectedBricks) : IGameEvent;
