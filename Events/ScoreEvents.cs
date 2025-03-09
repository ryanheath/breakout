namespace Breakout.Events;

// Event fired when score changes
public record ScoreChangedEvent(int NewScore) : IGameEvent;

// Event fired when lives change
public record LivesChangedEvent(int NewLives) : IGameEvent;

// Note: GameOverEvent is now defined in GameEvents.cs
