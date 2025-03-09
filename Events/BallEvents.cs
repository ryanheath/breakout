namespace Breakout.Events;

// Ball lost event
public record BallLostEvent : IGameEvent;

// Paddle collision event
public record PaddleCollisionEvent(Ball Ball, float HitPosition) : IGameEvent;

// Wall collision event
public class WallCollisionEvent : IGameEvent
{
    public enum WallSide
    {
        Left,
        Right,
        Top
    }

    public Ball Ball { get; }
    public WallSide Side { get; }

    public WallCollisionEvent(Ball ball, WallSide side)
    {
        Ball = ball;
        Side = side;
    }
}
