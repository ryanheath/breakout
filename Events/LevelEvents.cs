namespace Breakout.Events;

// Request to advance to the next level
public record LevelAdvanceRequestEvent : IGameEvent;

// Event fired when level is successfully advanced
public record LevelAdvancedEvent(int NewLevel, int MaxLevels) : IGameEvent;

// Event fired when there are no more levels
public record AllLevelsCompletedEvent(int FinalScore) : IGameEvent;

// Event fired when levels are reset to the beginning
public record LevelResetEvent(int StartLevel) : IGameEvent;
