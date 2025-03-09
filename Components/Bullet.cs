namespace Breakout.Components;

public class Bullet(float x, float y)
{
    public const int Width = 5;
    public const int Height = 10;
    public const float Speed = 8.0f;

    public Vector2 Position { get; set; } = new Vector2(x, y);
    public bool IsActive { get; set; } = true;

    public void Update(float deltaTime)
    {
        // Move bullet upward
        Position = new Vector2(Position.X, Position.Y - Speed);
    }

    public void Draw()
    {
        if (IsActive)
        {
            Raylib.DrawRectangle((int)Position.X, (int)Position.Y, Width, Height, Color.Yellow);
        }
    }

    public Rectangle GetRectangle() => new Rectangle(Position.X, Position.Y, Width, Height);
}
