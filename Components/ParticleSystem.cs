#pragma warning disable CS9124 // Parameter is captured into the state of the enclosing type and its value is also used to initialize a field, property, or event.

namespace Breakout.Components;

public class ParticleSystem
{
    private readonly List<Particle> _particles = [];
    
    class Particle(Vector2 position, Vector2 velocity, float size, float life, Color color)
    {
        private readonly float maxLife = life;

        public bool IsAlive => life > 0;
        
        public void Update(float deltaTime)
        {
            position += velocity * deltaTime;
            life -= deltaTime;
            size -= deltaTime * (size / maxLife);
        }
        
        public void Draw()
        {
            // Calculate alpha based on remaining life
            var c = color;
            c.A = (byte)(255 * (life / maxLife));
            
            // Draw the particle
            Raylib.DrawCircleV(position, size, c);
        }
    }
    
    public void CreateExplosion(Vector2 position, Color color, int particleCount = 20)
    {
        for (int i = 0; i < particleCount; i++)
        {
            float angle = (float)Random.Shared.NextDouble() * MathF.PI * 2;
            float speed = (float)Random.Shared.NextDouble() * 200 + 50;
            Vector2 velocity = new(
                MathF.Cos(angle) * speed,
                MathF.Sin(angle) * speed
            );
            
            float size = (float)Random.Shared.NextDouble() * 4 + 2;
            float life = (float)Random.Shared.NextDouble() * 0.5f + 0.5f;
            
            _particles.Add(new Particle(position, velocity, size, life, color));
        }
    }
    
    // Add a new method to create a single particle with specific properties
    public void CreateParticle(Vector2 position, Vector2 velocity, float size, float life, Color color)
    {
        _particles.Add(new Particle(position, velocity, size, life, color));
    }
    
    public void Update(float deltaTime)
    {
        for (int i = _particles.Count - 1; i >= 0; i--)
        {
            _particles[i].Update(deltaTime);
            
            if (_particles[i].IsAlive == false)
            {
                _particles.RemoveAt(i);
            }
        }
    }
    
    public void Draw()
    {
        foreach (var particle in _particles)
        {
            particle.Draw();
        }
    }
    
    public int ParticleCount => _particles.Count;
    
    public void Clear()
    {
        _particles.Clear();
    }
}
