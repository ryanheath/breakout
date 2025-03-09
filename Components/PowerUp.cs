namespace Breakout.Components;

public class PowerUp(float x, float y, PowerUp.Type type, Brick brick)
{
    private const float FallSpeed = 3.0f;
    
    public enum Type
    {
        // Positive power-ups
        PaddleGrow,
        ExtraBall,
        Gun,
        
        // Negative power-ups (power-downs)
        PaddleShrink,    // Shrinks the paddle
        SpeedUp,         // Makes the ball faster
        ReverseControls  // Reverses paddle controls temporarily
    }
    
    public Type PowerUpType { get; } = type;
    public Vector2 Position { get; private set; } = new(x, y);
    public Vector2 Size { get; } = new(20, 20);
    public bool IsVisible { get; set; } = true;
    public bool IsActive { get; set; } = false;
    public Brick Brick { get; } = brick;
    
    public bool IsGood => PowerUpType is Type.PaddleGrow or Type.ExtraBall or Type.Gun;
    
    public void Update()
    {
        // Only fall if active
        if (IsActive)
        {
            // Move power-up downwards
            Position = new Vector2(Position.X, Position.Y + FallSpeed);
        }
    }
    
    public void Draw()
    {
        // Only draw if visible
        if (!IsVisible) return;
        
        // Draw power-ups with upward triangles, power-downs with downward triangles
        switch (PowerUpType)
        {
            case Type.PaddleGrow:
                DrawPowerUpSquare(Color.Green, "P");
                break;
                
            case Type.ExtraBall:
                DrawPowerUpSquare(Color.Blue, "B");
                break;
                
            case Type.Gun:
                DrawPowerUpSquare(Color.Red, "G");
                break;
                
            case Type.PaddleShrink:
                DrawPowerDownSquare(Color.Magenta, "S");
                break;
                
            case Type.SpeedUp:
                DrawPowerDownSquare(Color.Orange, "F");
                break;
                
            case Type.ReverseControls:
                DrawPowerDownSquare(Color.Purple, "R");
                break;
        }
    }
    
    private void DrawPowerUpSquare(Color color, string letter)
    {
        Raylib.DrawRectangleV(Position, Size, color);
        Raylib.DrawRectangleLinesEx(new Rectangle(Position.X, Position.Y, Size.X, Size.Y), 2, Color.White);
        Raylib.DrawText(letter, (int)Position.X + 6, (int)Position.Y + 2, 16, Color.White);
    }
    
    private void DrawPowerDownSquare(Color color, string letter)
    {
        Raylib.DrawRectangleV(Position, Size, color);
        Raylib.DrawRectangleLinesEx(new Rectangle(Position.X, Position.Y, Size.X, Size.Y), 2, Color.Black);
        
        // Draw downward triangle indicator for power-downs
        Vector2[] triangle = [
            new Vector2(Position.X + Size.X / 2, Position.Y + Size.Y - 4),
            new Vector2(Position.X + 4, Position.Y + 4),
            new Vector2(Position.X + Size.X - 4, Position.Y + 4)
        ];
        
        Raylib.DrawTriangle(triangle[0], triangle[1], triangle[2], Color.Black);
        
        // Draw letter
        Raylib.DrawText(letter, (int)Position.X + 6, (int)Position.Y + 2, 16, Color.White);
    }
    
    public Rectangle GetRectangle() => new(Position.X, Position.Y, Size.X, Size.Y);
    
    public void Release()
    {
        IsVisible = true;
        IsActive = true;
    }
    
    public void Deactivate() 
    {
        IsActive = false;
        IsVisible = false;
    }
}
