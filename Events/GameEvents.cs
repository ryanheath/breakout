namespace Breakout.Events;

// Game state events
public record GameStateChangedEvent(GameState.State OldState, GameState.State NewState) : IGameEvent;
public record GameStartedEvent(Ball Ball, Vector2 InitialVelocity) : IGameEvent;
public record GameRestartEvent(int InitialLives = 3) : IGameEvent;
public record GameOverEvent(int FinalScore) : IGameEvent;
public record GameWonEvent(int FinalScore) : IGameEvent;
public record GamePausedEvent : IGameEvent;
public record GameResumedEvent : IGameEvent;

// Level events
public record LevelCompletedEvent(int LevelNumber, int RemainingLevels) : IGameEvent;
public record LevelRestartEvent : IGameEvent;

// Power-up events
public record PowerUpActivatedEvent(PowerUp.Type PowerUpType) : IGameEvent;
public record PowerUpExpiredEvent(PowerUp.Type PowerUpType) : IGameEvent;
public record PowerUpCollectedEvent(PowerUp PowerUp) : IGameEvent;

// Menu events
public record MenuNavigationEvent : IGameEvent;
public record MenuSelectionEvent : IGameEvent;
public record ViewHighScoresEvent : IGameEvent;

// Game mode events
public record GameModeChangedEvent(GameState.Mode NewMode) : IGameEvent;

// Time events
public record TimeBonusEvent(float BonusSeconds) : IGameEvent;
public record TimerExpiredEvent : IGameEvent;

// Audio events
public record MusicToggleEvent(bool Enabled) : IGameEvent;
public record SoundVolumeChangedEvent(float Volume) : IGameEvent;

// Bonus round events
public record BonusRoundRequestEvent(bool IsCheat = false) : IGameEvent;
public record BonusRoundStartedEvent(bool IsCheat = false) : IGameEvent;
public record BonusRoundCompletedEvent(int Score) : IGameEvent;

// Cheat events
public record CheatActivatedEvent(string CheatName) : IGameEvent;

// Note: The following events are defined in separate files:
// - BrickHitEvent, AllBricksDestroyedEvent (in BrickEvents.cs)
// - LevelAdvanceRequestEvent, LevelAdvancedEvent, AllLevelsCompletedEvent, LevelResetEvent (in LevelEvents.cs)
// - ScoreChangedEvent, LivesChangedEvent (in ScoreEvents.cs)
// - NewHighScoreEvent (in HighScoreEvents.cs)
// - GunShotEvent (in GunShotEvent.cs)
// - BallLostEvent, PaddleCollisionEvent, WallCollisionEvent (in BallEvents.cs)
