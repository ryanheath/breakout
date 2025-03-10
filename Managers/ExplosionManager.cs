namespace Breakout.Managers;

public class ExplosionManager(GameState gameState) : ManagerBase(gameState)
{
    private readonly ParticleSystem _particleSystem = new();
    private readonly DebrisSystem _debrisSystem = new();
    private float _screenShakeIntensity = 0f;
    private float _screenShakeTimer = 0f;
    private const float SHAKE_DECREASE_RATE = 10.0f;
    
    public override void Initialize()
    {
        EventBus.Subscribe<BrickHitEvent>(OnBrickHit);
        EventBus.Subscribe<GameRestartEvent>(OnGameRestart);
        EventBus.Subscribe<ExplosiveBrickDetonatedEvent>(OnExplosiveBrick);
    }
    
    public override void Cleanup()
    {
        EventBus.Unsubscribe<BrickHitEvent>(OnBrickHit);
        EventBus.Unsubscribe<GameRestartEvent>(OnGameRestart);
        EventBus.Unsubscribe<ExplosiveBrickDetonatedEvent>(OnExplosiveBrick);
        
        _particleSystem.Clear();
        _debrisSystem.Clear();
    }
    
    private void OnBrickHit(BrickHitEvent evt)
    {
        if (evt.Destroyed)
        {
            // Get the center of the brick for the explosion origin
            Rectangle brickRect = evt.Brick.GetRectangle();
            Vector2 position = new(
                brickRect.X + brickRect.Width / 2,
                brickRect.Y + brickRect.Height / 2
            );
            
            // Create an explosion with the brick's color
            int particleCount = evt.Brick.Type == Brick.BrickType.Explosive ? 50 : 30;
            _particleSystem.CreateExplosion(
                position, 
                evt.Brick.GetColor(), 
                particleCount
            );
            
            // Add debris pieces
            _debrisSystem.CreateDebrisFromBrick(evt.Brick);
        }
    }
    
    private void OnExplosiveBrick(ExplosiveBrickDetonatedEvent evt)
    {
        // Create a larger explosion for explosive bricks
        Rectangle brickRect = evt.Brick.GetRectangle();
        Vector2 position = new(
            brickRect.X + brickRect.Width / 2,
            brickRect.Y + brickRect.Height / 2
        );
        
        // Create a larger, more dramatic explosion
        _particleSystem.CreateExplosion(position, Color.Orange, 100);
        
        // Add a shockwave effect (larger particles that fade quickly)
        CreateShockwave(position);
        
        // Create debris for the exploding brick
        _debrisSystem.CreateDebrisFromBrick(evt.Brick, 10); // More debris for explosions
        
        // Create debris for affected bricks too
        foreach (var affectedBrick in evt.AffectedBricks)
        {
            _debrisSystem.CreateDebrisFromBrick(affectedBrick);
        }
        
        // Add screen shake with higher intensity
        _screenShakeIntensity = Math.Min(_screenShakeIntensity + 15.0f, 30.0f);
        _screenShakeTimer = 0.7f; // Longer shake duration
    }
    
    private void CreateShockwave(Vector2 position)
    {
        Color shockwaveColor = new(255, 200, 100, 150); // Semi-transparent orange
        
        // Create larger particles that move outward in a circle
        for (int i = 0; i < 16; i++)
        {
            float angle = i * MathF.PI / 8.0f; // Evenly spaced angles
            float speed = 300.0f; // Fast moving
            
            Vector2 velocity = new(
                MathF.Cos(angle) * speed,
                MathF.Sin(angle) * speed
            );
            
            _particleSystem.CreateParticle(
                position,
                velocity,
                10.0f, // Large size
                0.3f,  // Short life
                shockwaveColor
            );
        }
    }
    
    private void OnGameRestart(GameRestartEvent evt)
    {
        // Clear all particles when game restarts
        _particleSystem.Clear();
        _debrisSystem.Clear();
    }
    
    public override void Update(float deltaTime)
    {
        _particleSystem.Update(deltaTime);
        _debrisSystem.Update(deltaTime);
        
        // Update screen shake with smoother decay
        if (_screenShakeTimer > 0)
        {
            _screenShakeTimer -= deltaTime;
            _screenShakeIntensity = MathF.Max(
                _screenShakeIntensity - (SHAKE_DECREASE_RATE * deltaTime), 
                0f
            );
            
            if (_screenShakeTimer <= 0)
            {
                _screenShakeIntensity = 0;
            }
        }
    }
    
    public override void Draw()
    {
        // Draw debris first so particles appear on top
        _debrisSystem.Draw();
        
        // Then draw particles
        _particleSystem.Draw();
    }
    
    public Camera2D ApplyScreenShake(Camera2D camera)
    {
        if (_screenShakeIntensity > 0)
        {
            // Use perlin noise or sine waves for smoother shake
            float time = (float)Raylib.GetTime() * 10.0f;
            float shakeX = MathF.Sin(time * 1.3f) * _screenShakeIntensity;
            float shakeY = MathF.Cos(time * 1.7f) * _screenShakeIntensity;
            
            // Add random component for more naturalistic movement
            shakeX += (Random.Shared.NextSingle() * 2 - 1) * _screenShakeIntensity * 0.3f;
            shakeY += (Random.Shared.NextSingle() * 2 - 1) * _screenShakeIntensity * 0.3f;
            
            // Apply shake to camera offset
            // Note: No need to modify with screen center, camera position handles that
            camera.Offset = new Vector2(shakeX, shakeY);
        }
        else
        {
            // Reset to zero offset when no shake
            camera.Offset = Vector2.Zero;
        }

        return camera;
    }
    
    public int TotalEffectCount => _particleSystem.ParticleCount + _debrisSystem.DebrisCount;
}
