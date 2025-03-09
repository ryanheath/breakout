namespace Breakout.Components;

public class Paddle(float x, float y, float width, float height)
{
    public Vector2 Position { get; set; } = new Vector2(x, y);
    public Vector2 Size { get; private set; } = new Vector2(width, height);
    public Color Color { get; set; } = Color.White; // Added Color property with default White
    public bool ReverseControls { get; set; } = false;
    public Vector2 Speed { get; set; } = new Vector2(8f, 0);
    
    public void Update(int screenWidth)
    {
        float direction = 0;
        
        if (Raylib.IsKeyDown(KeyboardKey.Left))
            direction = ReverseControls ? 1 : -1;
        if (Raylib.IsKeyDown(KeyboardKey.Right))
            direction = ReverseControls ? -1 : 1;
            
        Position += direction * Speed;
        
        // Keep paddle within screen bounds
        Position = new(Math.Clamp(Position.X, 0, screenWidth - Size.X), Position.Y);
    }
    
    public void Draw()
    {
        Raylib.DrawRectangleV(Position, Size, Color);
    }
    
    public Rectangle GetRectangle()
    {
        return new Rectangle(Position.X, Position.Y, Size.X, Size.Y);
    }
    
    public void Resize(float width, float height)
    {
        Size = new Vector2(width, height);
    }
}
