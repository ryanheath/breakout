#pragma warning disable CS9124 // Parameter is captured into the state of the enclosing type and its value is also used to initialize a field, property, or event.

namespace Breakout.Components;

public class DebrisSystem
{
    private readonly List<Debris> _debris = [];

    class Debris(Vector2 position, Vector2 velocity, float size, float life, Color color)
    {
        private float rotation = (float)Random.Shared.NextDouble() * 360;
        public readonly float rotationSpeed = ((float)Random.Shared.NextDouble() - 0.5f) * 720; // -360 to 360 degrees per second
        private readonly float maxLife = life;

        public bool IsAlive => life > 0;

        public void Update(float deltaTime)
        {
            // Apply gravity
            velocity += new Vector2(0, 200f) * deltaTime;
            
            // Apply drag
            velocity *= 0.99f;
            
            // Update position
            position += velocity * deltaTime;
            
            // Update rotation
            rotation += rotationSpeed * deltaTime;
            
            // Decrease life
            life -= deltaTime;
        }
        
        public void Draw()
        {
            // Draw rotated rectangle
            Rectangle rect = new(position.X, position.Y, size, size);
            Vector2 origin = new(size / 2, size / 2);
            
            // Calculate alpha based on remaining life
            Color drawColor = color;
            drawColor.A = (byte)(255 * (life / maxLife));
            
            Raylib.DrawRectanglePro(rect, origin, rotation, drawColor);
        }
    }
    
    public void CreateDebrisFromBrick(Brick brick, int count = 5)
    {
        Rectangle brickRect = brick.GetRectangle();
        Vector2 center = new(
            brickRect.X + brickRect.Width / 2,
            brickRect.Y + brickRect.Height / 2
        );
        
        // Generate debris based on brick size and color
        for (int i = 0; i < count; i++)
        {
            // Random position within the brick
            Vector2 position = new(
                center.X + (Random.Shared.NextSingle() - 0.5f) * brickRect.Width * 0.8f,
                center.Y + (Random.Shared.NextSingle() - 0.5f) * brickRect.Height * 0.8f
            );
            
            // Random velocity with upward bias
            Vector2 velocity = new(
                (Random.Shared.NextSingle() - 0.5f) * 200,
                Random.Shared.NextSingle() * -100 - 50 // Upward bias
            );
            
            // Random size
            float size = Random.Shared.NextSingle() * 6 + 4;
            
            // Life based on size (bigger pieces last longer)
            float life = Random.Shared.NextSingle() * size + 0.5f;
            
            // Create debris piece with brick's color
            _debris.Add(new Debris(position, velocity, size, life, brick.GetColor()));
        }
    }
    
    public void Update(float deltaTime)
    {
        // Update debris
        for (int i = _debris.Count - 1; i >= 0; i--)
        {
            _debris[i].Update(deltaTime);
            
            // Remove expired debris
            if (_debris[i].IsAlive == false)
            {
                _debris.RemoveAt(i);
            }
        }
    }
    
    public void Draw()
    {
        foreach (var debris in _debris)
        {
            debris.Draw();
        }
    }
    
    public void Clear()
    {
        _debris.Clear();
    }
    
    public int DebrisCount => _debris.Count;
}
