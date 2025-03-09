namespace Breakout.Managers;

public class CollisionManager(GameState gameState) : ManagerBase(gameState)
{
    private const float MinHorizontalSpeed = 3.0f;
    private const float TargetBallSpeed = 7.0f;
    
    public void HandleWallCollisions(Ball ball)
    {
        // Left wall
        if (ball.Position.X <= ball.Radius)
        {
            ball.Speed = new Vector2(-ball.Speed.X, ball.Speed.Y);
            EventBus.Publish(new WallCollisionEvent(ball, WallCollisionEvent.WallSide.Left));
        }
        // Right wall
        else if (ball.Position.X >= gameState.ScreenWidth - ball.Radius)
        {
            ball.Speed = new Vector2(-ball.Speed.X, ball.Speed.Y);
            EventBus.Publish(new WallCollisionEvent(ball, WallCollisionEvent.WallSide.Right));
        }
        
        // Top wall
        if (ball.Position.Y <= ball.Radius)
        {
            ball.Speed = new Vector2(ball.Speed.X, -ball.Speed.Y);
            EventBus.Publish(new WallCollisionEvent(ball, WallCollisionEvent.WallSide.Top));
        }
    }
    
    public bool HandlePaddleCollision(Ball ball)
    {
        if (!ball.CheckCollision(gameState.Paddle.GetRectangle()))
        {
            return false;
        }
        
        // Calculate hit position relative to paddle (0.0 = left edge, 1.0 = right edge)
        float hitPosition = (ball.Position.X - gameState.Paddle.Position.X) / gameState.Paddle.Size.X;
        
        // Map hit position to an angle between -60° and 60° (in radians)
        // Left edge = -60°, middle = 0°, right edge = 60°
        float bounceAngle = (hitPosition - 0.5f) * MathF.PI * 2/3;
        
        // Add paddle velocity influence - if paddle is moving, affect ball direction
        float paddleVelocityInfluence = 0.3f;
        float paddleVelocityFactor = gameState.Paddle.Speed.X * paddleVelocityInfluence;
        
        // Apply paddle velocity influence to the bounce angle
        bounceAngle += paddleVelocityFactor * 0.05f;
        
        // Limit the maximum angle to ensure the ball doesn't bounce too horizontally
        bounceAngle = Math.Clamp(bounceAngle, -MathF.PI/3, MathF.PI/3);
        
        // Convert angle to a velocity vector with consistent speed
        float speed = TargetBallSpeed * gameState.BallSpeedMultiplier;
        float speedX = speed * MathF.Sin(bounceAngle);
        float speedY = -speed * MathF.Cos(bounceAngle); // Negative Y to make ball go upward
        
        // Set ball velocity
        ball.Speed = new Vector2(speedX, speedY);
        
        // Prevent ball from getting stuck in paddle by moving it to paddle's top edge
        ball.Position = new Vector2(ball.Position.X, gameState.Paddle.Position.Y - ball.Radius - 1);
        
        // Publish paddle collision event
        EventBus.Publish(new PaddleCollisionEvent(ball, hitPosition));
        
        return true;
    }
    
    private bool CheckAndHandleBrickCollision<T>(T projectile, List<Brick> bricks, Action<T, int>? onHitAction) where T : class
    {
        for (int i = bricks.Count - 1; i >= 0; i--)
        {
            if (projectile is Ball ball && ball.CheckCollision(bricks[i].GetRectangle()))
            {
                HandleBallBrickCollision(ball, bricks[i], i, bricks);
                onHitAction?.Invoke(projectile, i);
                return true;
            }
            else if (projectile is Bullet bullet && Raylib.CheckCollisionRecs(bullet.GetRectangle(), bricks[i].GetRectangle()))
            {
                HandleBulletBrickCollision(bullet, bricks[i], bricks);
                onHitAction?.Invoke(projectile, i);
                return true;   
            }
        }
        
        return false;
    }
    
    public bool HandleBrickCollisions(Ball ball, List<Brick> bricks)
    {
        return CheckAndHandleBrickCollision<Ball>(ball, bricks, null);
    }
    
    private void HandleBallBrickCollision(Ball ball, Brick brick, int index, List<Brick> bricks)
    {
        // Determine which side of the brick was hit
        var brickRect = brick.GetRectangle();
        var overlapLeft = ball.Position.X + ball.Radius - brickRect.X;
        var overlapRight = brickRect.X + brickRect.Width - (ball.Position.X - ball.Radius);
        var overlapTop = ball.Position.Y + ball.Radius - brickRect.Y;
        var overlapBottom = brickRect.Y + brickRect.Height - (ball.Position.Y - ball.Radius);

        var minOverlap = MathF.Min(MathF.Min(overlapLeft, overlapRight), MathF.Min(overlapTop, overlapBottom));

        // Reverse appropriate velocity component
        if (minOverlap == overlapLeft || minOverlap == overlapRight)
        {
            ball.Speed = new Vector2(-ball.Speed.X, ball.Speed.Y);
        }
        else
        {
            ball.Speed = new Vector2(ball.Speed.X, -ball.Speed.Y);
        }

        // Ensure the ball maintains a minimum horizontal speed
        if (MathF.Abs(ball.Speed.X) < MinHorizontalSpeed)
        {
            var sign = ball.Speed.X >= 0 ? 1 : -1;
            ball.Speed = new Vector2(sign * MinHorizontalSpeed, ball.Speed.Y);
        }

        ProcessBrickHit(brick, bricks);
    }

    private void HandleBulletBrickCollision(Bullet bullet, Brick brick, List<Brick> bricks)
    {
        ProcessBrickHit(brick, bricks);
    }

    private void ProcessBrickHit(Brick brick, List<Brick> bricks)
    {
        bool brickDestroyed = brick.Hit();
            
        if (brickDestroyed)
        {
            // Instead of removing the brick immediately, mark it for removal
            // This helps ensure that the brick index in BrickHitEvent is accurate
            bricks.Remove(brick);
        }

        EventBus.Publish(new BrickHitEvent(brick, brickDestroyed));
    }
    
    public void HandlePowerUpCollisions(Paddle paddle, List<PowerUp> powerUps)
    {
        for (int i = powerUps.Count - 1; i >= 0; i--)
        {
            var powerUp = powerUps[i];

            // Only check collisions for active power-ups
            if (powerUp.IsActive && powerUp.IsVisible && Raylib.CheckCollisionRecs(powerUp.GetRectangle(), paddle.GetRectangle()))
            {
                // Process the power-up
                powerUp.Deactivate();
                
                // Publish event
                EventBus.Publish(new PowerUpCollectedEvent(powerUp));
                
                // Remove from list
                powerUps.RemoveAt(i);
            }
        }
    }
    
    public bool IsBallLost(Ball ball)
    {
        return ball.Position.Y >= gameState.ScreenHeight - ball.Radius;
    }

    public void HandleBulletCollisions(List<Bullet> bullets, List<Brick> bricks, PowerUpManager powerUpManager)
    {
        foreach (var bullet in bullets.ToList())
        {
            // Check for top wall collision
            if (bullet.Position.Y <= 0)
            {
                powerUpManager.RemoveBullet(bullet);
                continue;
            }
            
            // Check for brick collisions
            CheckAndHandleBrickCollision<Bullet>(bullet, bricks, (b, _) => powerUpManager.RemoveBullet(b));
        }
    }
}
