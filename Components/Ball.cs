namespace Breakout.Components;

public class Ball(float x, float y, float speedX, float speedY, float radius)
{
    public Vector2 Position { get; set; } = new Vector2(x, y);
    public Vector2 Speed { get; set; } = new Vector2(speedX, speedY);
    public float Radius { get; } = radius;
    public Color Color { get; set; } = Color.White;

    public void Update()
    {
        Position += Speed;
    }

    public void Draw()
    {
        Raylib.DrawCircleV(Position, Radius, Color);
    }

    public bool CheckCollision(Rectangle rect)
    {
        return Raylib.CheckCollisionCircleRec(Position, Radius, rect);
    }
}
