namespace Breakout.Events;

// Event fired when a new high score is achieved
public record NewHighScoreEvent(int Score, int Rank) : IGameEvent;
