namespace Breakout.Components;

public class Brick(float x, float y, float width, float height, Color color, int hitPoints = 1)
{
    private Vector2 Position { get; } = new(x, y);
    private Vector2 Size { get; } = new(width, height);
    private Color OriginalColor { get; } = color;
    private Color CurrentColor { get; set; } = color;
    
    public int HitPoints { get; private set; } = hitPoints;
    public BrickType Type { get; set; } = BrickType.Normal;
    
    public enum BrickType
    {
        Normal,
        Explosive,    // Destroys adjacent bricks when hit
        Invincible,   // Cannot be destroyed
        MultiScore    // Gives 3x points when destroyed
    }
    
    public bool Hit()
    {
        // Invincible bricks cannot be destroyed
        if (Type == BrickType.Invincible)
        {
            return false;
        }
        
        HitPoints--;
        
        // If more than one hit point, darken the color
        if (HitPoints > 0)
        {
            // Create a darker version of the color by reducing RGB components
            CurrentColor = new Color(
                (byte)(OriginalColor.R * 0.6f), 
                (byte)(OriginalColor.G * 0.6f), 
                (byte)(OriginalColor.B * 0.6f), 
                OriginalColor.A
            );
            
            return false; // Brick not destroyed yet
        }
        
        return true; // Brick destroyed
    }
    
    public void Draw()
    {
        // Draw the main brick body
        Raylib.DrawRectangleV(Position, Size, CurrentColor);
        
        // Draw border
        Raylib.DrawRectangleLinesEx(
            new Rectangle(Position.X, Position.Y, Size.X, Size.Y), 1, Color.White);
        
        // Draw special brick indicators
        switch (Type)
        {
            case BrickType.Explosive:
                // Enhanced explosive brick visual
                // Draw flame-like pattern
                Color explosiveColor = Color.Red;
                float centerX = Position.X + Size.X / 2;
                float centerY = Position.Y + Size.Y / 2;
                float radius = Math.Min(Size.X, Size.Y) * 0.3f;
                
                // Draw central circle
                Raylib.DrawCircle((int)centerX, (int)centerY, radius * 0.7f, explosiveColor);
                
                // Draw flame rays
                for (int i = 0; i < 8; i++)
                {
                    float angle = i * MathF.PI / 4;
                    float rayLength = radius * 1.2f;
                    Raylib.DrawLineEx(
                        new Vector2(centerX, centerY),
                        new Vector2(
                            centerX + rayLength * MathF.Cos(angle),
                            centerY + rayLength * MathF.Sin(angle)
                        ),
                        3.0f, explosiveColor);
                }
                
                // Draw outer explosion ring
                Raylib.DrawCircleLines((int)centerX, (int)centerY, radius, Color.Yellow);
                break;
                
            case BrickType.Invincible:
                // Draw solid border for invincible bricks
                Raylib.DrawRectangleLinesEx(
                    new Rectangle(Position.X + 2, Position.Y + 2, Size.X - 4, Size.Y - 4), 
                    2, Color.White);
                break;
                
            case BrickType.MultiScore:
                // Draw $ symbol for multi-score bricks
                Raylib.DrawText("$", (int)(Position.X + Size.X/2 - 5), (int)(Position.Y + Size.Y/2 - 8), 16, Color.White);
                break;
        }
        
        // Draw knobs/indicators if the brick has more than one hit point
        if (HitPoints > 1)
        {
            // Draw small circles as knobs to indicate reinforced brick
            float knobRadius = Size.Y / 6;
            float knobY = Position.Y + Size.Y / 2;
            
            // Draw the first knob on the left side
            float knob1X = Position.X + Size.X * 0.25f;
            Raylib.DrawCircle((int)knob1X, (int)knobY, knobRadius, Color.White);
            
            // Draw the second knob on the right side
            float knob2X = Position.X + Size.X * 0.75f;
            Raylib.DrawCircle((int)knob2X, (int)knobY, knobRadius, Color.White);
        }
    }
    
    public Rectangle GetRectangle() => new(Position.X, Position.Y, Size.X, Size.Y);
    
    // New method to get the current color
    public Color GetColor() => CurrentColor;
}
