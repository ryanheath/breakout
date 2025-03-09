namespace Breakout.Managers;

public class PowerUpManager(GameState gameState) : ManagerBase(gameState)
{
    private readonly Vector2 _originalPaddleSize = gameState.Paddle.Size;
    private readonly Color _originalPaddleColor = gameState.Paddle.Color;
    
    // Added constants for paddle blinking
    private const float BlinkThreshold = 2.0f; // Start blinking when 2 seconds remain
    private const float BlinkInterval = 0.2f; // Blink every 0.2 seconds
    private float _blinkTimer = 0;
    private bool _blinkVisible = true;
    
    // Original ball speed to restore after speedup expires
    private float _originalBallSpeed = 0.0f;
    private Color _originalBallColor;
    private float _gunCooldown = 0; // Cooldown timer for gun shots
    private const float GunCooldownTime = 0.5f; // Time between shots

    public override void Initialize()
    {
        EventBus.Subscribe<PowerUpCollectedEvent>(OnPowerUpCollected);
    }
    
    public override void Cleanup()
    {
        base.Cleanup();
        
        EventBus.Unsubscribe<PowerUpCollectedEvent>(OnPowerUpCollected);
        Reset();
    }
    
    private void OnPowerUpCollected(PowerUpCollectedEvent evt)
    {
        ActivatePowerUp(evt.PowerUp);
    }
    
    private void ActivatePowerUp(PowerUp powerUp)
    {
        switch (powerUp.PowerUpType)
        {
            // Positive power-ups
            case PowerUp.Type.PaddleGrow:
                ActivatePaddleGrow();
                break;
                
            case PowerUp.Type.ExtraBall:
                ActivateExtraBall();
                break;
                
            case PowerUp.Type.Gun:
                ActivateGun();
                break;
                
            // Negative power-ups (power-downs)
            case PowerUp.Type.PaddleShrink:
                ActivatePaddleShrink();
                break;
                
            case PowerUp.Type.SpeedUp:
                ActivateSpeedUp();
                break;
                
            case PowerUp.Type.ReverseControls:
                ActivateReverseControls();
                break;
        }
        
        EventBus.Publish(new PowerUpActivatedEvent(powerUp.PowerUpType));
    }

    private void ActivatePaddleGrow()
    {
        gameState.ActivePowerUpTimers[PowerUp.Type.PaddleGrow] = 0;
        
        var newWidth = _originalPaddleSize.X * 2;
        gameState.Paddle.Resize(newWidth, _originalPaddleSize.Y);

        // Change paddle color to green
        gameState.Paddle.Color = Color.Green;
    }
    
    private void ActivateExtraBall()
    {
        var offset = new Vector2(10, 0);
        var newBall = new Ball(
            gameState.MainBall.Position.X + offset.X,
            gameState.MainBall.Position.Y + offset.Y,
            gameState.MainBall.Speed.X,
            gameState.MainBall.Speed.Y,
            gameState.MainBall.Radius
        );
        
        newBall.Color = Color.Blue;
        gameState.ExtraBalls.Add(newBall);
    }

    private void ActivateGun()
    {
        gameState.ActivePowerUpTimers[PowerUp.Type.Gun] = 0;
        _gunCooldown = 0;
    }
    
    private void ActivatePaddleShrink()
    {
        gameState.ActivePowerUpTimers[PowerUp.Type.PaddleShrink] = 0;
        
        // Make the paddle half its original width
        var newWidth = _originalPaddleSize.X * 0.5f;
        gameState.Paddle.Resize(newWidth, _originalPaddleSize.Y);

        // Change paddle color to yellow
        gameState.Paddle.Color = Color.Yellow;
    }
    
    private void ActivateSpeedUp()
    {
        gameState.ActivePowerUpTimers[PowerUp.Type.SpeedUp] = 0;

        // Save original ball speed and color
        _originalBallSpeed = gameState.MainBall.Speed.Length();
        _originalBallColor = gameState.MainBall.Color;

        // Set the ball speed multiplier in the game state
        gameState.SetBallSpeedMultiplier(1.5f);

        // Change main ball color to red to indicate speed mode
        gameState.MainBall.Color = Color.Red;

        // Apply to main ball and extra balls
        ApplySpeedMultiplierToBall(gameState.MainBall);
        
        foreach (var ball in gameState.ExtraBalls)
        {
            ApplySpeedMultiplierToBall(ball);
            // Extra balls stay blue - no need to change their colors
        }
    }
    
    // Helper method to apply speed multiplier to a ball
    private void ApplySpeedMultiplierToBall(Ball ball)
    {
        Vector2 direction = Vector2.Normalize(ball.Speed);
        float currentSpeed = ball.Speed.Length();
        ball.Speed = direction * (currentSpeed * gameState.BallSpeedMultiplier);
    }
    
    private void ActivateReverseControls()
    {
        gameState.ActivePowerUpTimers[PowerUp.Type.ReverseControls] = 0;

        // Toggle the paddle's reverse controls flag
        gameState.Paddle.ReverseControls = true;
    }
    
    public override void Update(float deltaTime)
    {
        // Reset blink timer and flag
        _blinkTimer += deltaTime;
        if (_blinkTimer >= BlinkInterval)
        {
            _blinkTimer = 0;
            _blinkVisible = !_blinkVisible;
        }
        
        foreach (var type in gameState.ActivePowerUpTimers.Keys.ToList())
        {
            gameState.ActivePowerUpTimers[type] += deltaTime;
            float timeRemaining = gameState.PowerUpDurations[type] - gameState.ActivePowerUpTimers[type];
            
            // Handle paddle blinking for power-ups affecting paddle size
            if ((type == PowerUp.Type.PaddleGrow || type == PowerUp.Type.PaddleShrink) && 
                timeRemaining <= BlinkThreshold)
            {
                // Blink between power-up color and original color
                if (_blinkVisible)
                {
                    gameState.Paddle.Color = type == PowerUp.Type.PaddleGrow ? Color.Green : Color.Yellow;
                }
                else
                {
                    gameState.Paddle.Color = _originalPaddleColor;
                }
            }
            
            if (gameState.ActivePowerUpTimers[type] >= gameState.PowerUpDurations[type])
            {
                DeactivatePowerUp(type);
            }
        }
        
        UpdateExtraBalls();
        UpdateBullets(deltaTime);
        
        // Handle gun shooting if active
        if (gameState.IsPowerUpActive(PowerUp.Type.Gun))
        {
            _gunCooldown -= deltaTime;
            
            if (_gunCooldown <= 0 && Raylib.IsKeyDown(KeyboardKey.Space) && gameState.Bullets.Count < gameState.MaxBullets)
            {
                ShootBullet();
                _gunCooldown = GunCooldownTime;
            }
        }
    }
    
    private void UpdateExtraBalls()
    {
        for (int i = gameState.ExtraBalls.Count - 1; i >= 0; i--)
        {
            gameState.ExtraBalls[i].Update();
        }
    }

    private void UpdateBullets(float deltaTime)
    {
        for (int i = gameState.Bullets.Count - 1; i >= 0; i--)
        {
            var bullet = gameState.Bullets[i];
            bullet.Update(deltaTime);
            
            // Remove bullets that go off screen
            if (bullet.Position.Y < 0 || !bullet.IsActive)
            {
                gameState.Bullets.RemoveAt(i);
            }
        }
    }
    
    private void ShootBullet()
    {
        // Create bullet at the middle-top of the paddle
        float bulletX = gameState.Paddle.Position.X + (gameState.Paddle.Size.X / 2) - (Bullet.Width / 2);
        float bulletY = gameState.Paddle.Position.Y - Bullet.Height;

        gameState.Bullets.Add(new Bullet(bulletX, bulletY));
        
        // Play sound effect
        EventBus.Publish(new GunShotEvent());
    }
    
    public void DeactivatePowerUp(PowerUp.Type type)
    {
        switch (type)
        {
            // Positive power-ups
            case PowerUp.Type.PaddleGrow:
                gameState.Paddle.Resize(_originalPaddleSize.X, _originalPaddleSize.Y);
                gameState.Paddle.Color = _originalPaddleColor; // Reset color
                break;
                
            case PowerUp.Type.Gun:
                // Clear all bullets when gun deactivates
                gameState.Bullets.Clear();
                break;
                
            // Negative power-ups
            case PowerUp.Type.PaddleShrink:
                gameState.Paddle.Resize(_originalPaddleSize.X, _originalPaddleSize.Y);
                gameState.Paddle.Color = _originalPaddleColor; // Reset color
                break;
                
            case PowerUp.Type.SpeedUp:
                // Reset the ball speed multiplier in the game state
                gameState.ResetBallSpeedMultiplier();

                // Restore original ball color
                gameState.MainBall.Color = _originalBallColor;
                
                // Restore original ball speed for main ball
                if (_originalBallSpeed > 0 && gameState.MainBall != null)
                {
                    Vector2 direction = Vector2.Normalize(gameState.MainBall.Speed);
                    gameState.MainBall.Speed = direction * _originalBallSpeed;
                    
                    // Also normalize speed of any extra balls
                    foreach (var ball in gameState.ExtraBalls)
                    {
                        Vector2 extraDirection = Vector2.Normalize(ball.Speed);
                        ball.Speed = extraDirection * _originalBallSpeed;
                        // No need to restore extra ball colors as they remain blue
                    }
                }
                break;
                
            case PowerUp.Type.ReverseControls:
                gameState.Paddle.ReverseControls = false;
                break;
        }

        gameState.ActivePowerUpTimers.Remove(type);
        EventBus.Publish(new PowerUpExpiredEvent(type));
    }
    
    public void RemoveLostBall(Ball ball)
    {
        gameState.ExtraBalls.Remove(ball);
    }
    
    public bool IsPowerUpActive(PowerUp.Type type) => gameState.ActivePowerUpTimers.ContainsKey(type);
    
    public float GetRemainingTime(PowerUp.Type type)
    {
        if (gameState.ActivePowerUpTimers.TryGetValue(type, out float time))
        {
            return gameState.PowerUpDurations[type] - time;
        }
        return 0;
    }
    
    public void RemoveBullet(Bullet bullet)
    {
        bullet.IsActive = false;
    }

    public void DrawGun()
    {
        if (gameState.IsPowerUpActive(PowerUp.Type.Gun))
        {
            // Draw gun on top of paddle
            float gunX = gameState.Paddle.Position.X + (gameState.Paddle.Size.X / 2) - 5;
            float gunY = gameState.Paddle.Position.Y - 10;
            Raylib.DrawRectangle((int)gunX, (int)gunY, 10, 10, Color.Gray);
        }
    }
    
    public void DrawBullets()
    {
        foreach (var bullet in gameState.Bullets)
        {
            bullet.Draw();
        }
    }
    
    public void Reset()
    {
        gameState.Paddle.Resize(_originalPaddleSize.X, _originalPaddleSize.Y);
        gameState.Paddle.Color = _originalPaddleColor; // Reset color
        gameState.Paddle.ReverseControls = false;
        
        // Reset ball speed if needed and reset multiplier in game state
        if (gameState.ActivePowerUpTimers.ContainsKey(PowerUp.Type.SpeedUp) && _originalBallSpeed > 0)
        {
            Vector2 currentDirection = Vector2.Normalize(gameState.MainBall.Speed);
            gameState.MainBall.Speed = currentDirection * _originalBallSpeed;
            gameState.ResetBallSpeedMultiplier();

            // Restore original ball color
            gameState.MainBall.Color = _originalBallColor;
        }
        
        _blinkTimer = 0;
        _blinkVisible = true;
        _originalBallSpeed = 0;
        gameState.ActivePowerUpTimers.Clear();
        gameState.ExtraBalls.Clear();
        gameState.Bullets.Clear();
    }
}
