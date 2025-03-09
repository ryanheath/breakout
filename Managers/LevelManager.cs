namespace Breakout.Managers;

public class LevelManager(GameState gameState) : ManagerBase(gameState)
{
    public override void Initialize()
    {
        EventBus.Subscribe<BrickHitEvent>(OnBrickHit);
        EventBus.Subscribe<GameRestartEvent>(OnGameRestart);
        EventBus.Subscribe<LevelAdvanceRequestEvent>(OnLevelAdvanceRequest);
        EventBus.Subscribe<BonusRoundRequestEvent>(OnBonusRoundRequest);
    }
    
    public override void Cleanup()
    {
        EventBus.Unsubscribe<BrickHitEvent>(OnBrickHit);
        EventBus.Unsubscribe<GameRestartEvent>(OnGameRestart);
        EventBus.Unsubscribe<LevelAdvanceRequestEvent>(OnLevelAdvanceRequest);
        EventBus.Unsubscribe<BonusRoundRequestEvent>(OnBonusRoundRequest);
        
        gameState.ClearLevel();
    }
    
    private void OnBrickHit(BrickHitEvent evt)
    {
        if (evt.Destroyed)
        {
            // Handle explosive bricks - destroy adjacent bricks
            if (evt.Brick.Type == Brick.BrickType.Explosive)
            {
                HandleExplosiveBrick(evt.Brick);
            }
            
            // Make sure to use the correct index from the event
            ReleasePowerUp(evt.Brick);
            
            if (AreAllBricksDestroyed())
            {
                if (gameState.InBonusRound)
                {
                    EventBus.Publish(new BonusRoundCompletedEvent(gameState.Score));
                }
                else
                {
                    EventBus.Publish(new AllBricksDestroyedEvent(gameState.CurrentLevel));
                }
            }
        }
    }
    
    private void HandleExplosiveBrick(Brick explodingBrick)
    {
        Rectangle explodingRect = explodingBrick.GetRectangle();
        float explosionRadius = Math.Max(explodingRect.Width, explodingRect.Height) * 1.5f;
        Vector2 explosionCenter = new(
            explodingRect.X + explodingRect.Width / 2,
            explodingRect.Y + explodingRect.Height / 2
        );
        
        // Create a list to collect affected bricks and handle them after iteration
        List<Brick> affectedBricks = [];
        
        // Find bricks within explosion radius
        foreach (var adjacentBrick in gameState.Bricks)
        {
            // Skip the exploding brick itself and invincible bricks
            if (adjacentBrick == explodingBrick || adjacentBrick.Type == Brick.BrickType.Invincible) 
                continue;
            
            Rectangle brickRect = adjacentBrick.GetRectangle();
            Vector2 brickCenter = new(
                brickRect.X + brickRect.Width / 2,
                brickRect.Y + brickRect.Height / 2
            );
            
            // Calculate distance between explosion center and brick center
            float distance = Vector2.Distance(explosionCenter, brickCenter);
            
            // If the brick is within explosion radius, add to affected list
            if (distance <= explosionRadius)
            {
                affectedBricks.Add(adjacentBrick);
            }
        }
        
        // Now handle all the affected bricks
        foreach (var brick in affectedBricks)
        {
            bool destroyed = brick.Hit();
            if (destroyed)
            {
                ReleasePowerUp(brick);
                gameState.Bricks.Remove(brick); // Safe to remove as we're not iterating the collection
                
                // Notify that this brick was destroyed by an explosion
                EventBus.Publish(new BrickHitEvent(brick, true));
            }
        }
        
        // Publish event about the explosion affecting multiple bricks
        if (affectedBricks.Count > 0)
        {
            EventBus.Publish(new ExplosiveBrickDetonatedEvent(explodingBrick, affectedBricks));
        }
    }
    
    private void OnGameRestart(GameRestartEvent evt)
    {
        ResetToFirstLevel();
    }
    
    private void OnLevelAdvanceRequest(LevelAdvanceRequestEvent evt)
    {
        if (gameState.CurrentLevel < gameState.MaxLevels)
        {
            gameState.CurrentLevel++;
            CreateLevel(gameState.CurrentLevel);
            
            // Publish event after level is created
            EventBus.Publish(new LevelAdvancedEvent(gameState.CurrentLevel, gameState.MaxLevels));
        }
        else
        {
            EventBus.Publish(new AllLevelsCompletedEvent(gameState.Score));
        }
    }

    private void OnBonusRoundRequest(BonusRoundRequestEvent evt)
    {
        gameState.ClearLevel();
        CreateBonusLevel();
        gameState.StartBonusRound();
        EventBus.Publish(new BonusRoundStartedEvent(evt.IsCheat));
    }
    
    public void CreateLevel()
    {
        CreateLevel(gameState.CurrentLevel);
    }
    
    public void CreateLevel(int level)
    {
        gameState.CurrentLevel = Math.Clamp(level, 1, gameState.MaxLevels);
        gameState.ClearLevel();
        
        switch (gameState.CurrentLevel)
        {
            case 1:
                CreateStandardLevel();
                break;
            case 2:
                CreateAdvancedLevel();
                break;
            case 3:
                CreateFinalLevel();
                break;
        }
        
        AddRandomPowerUps();
    }
    
    public bool AdvanceToNextLevel()
    {
        if (gameState.CurrentLevel < gameState.MaxLevels)
        {
            gameState.CurrentLevel++;
            CreateLevel(gameState.CurrentLevel);
            EventBus.Publish(new LevelAdvancedEvent(gameState.CurrentLevel, gameState.MaxLevels));
            return true;
        }
        return false;
    }
    
    public void ResetToFirstLevel()
    {
        gameState.CurrentLevel = 1;
        CreateLevel();
        EventBus.Publish(new LevelResetEvent(gameState.CurrentLevel));
    }
    
    private void AddRandomPowerUps()
    {
        if (gameState.Bricks.Count < 12) return; // Ensure enough bricks for all power-up types
        
        // Get random unique brick indices
        var indexes = GetRandomUniqueIndexes(12, 0, gameState.Bricks.Count);
        
        if (indexes.Count >= 12)
        {
            // Add positive power-ups
            AddPowerUp(gameState.Bricks[indexes[0]], PowerUp.Type.PaddleGrow);
            AddPowerUp(gameState.Bricks[indexes[1]], PowerUp.Type.PaddleGrow);
            AddPowerUp(gameState.Bricks[indexes[2]], PowerUp.Type.ExtraBall);
            AddPowerUp(gameState.Bricks[indexes[3]], PowerUp.Type.ExtraBall);
            AddPowerUp(gameState.Bricks[indexes[4]], PowerUp.Type.Gun);
            AddPowerUp(gameState.Bricks[indexes[5]], PowerUp.Type.Gun);
            
            // Add negative power-ups (power-downs)
            AddPowerUp(gameState.Bricks[indexes[6]], PowerUp.Type.PaddleShrink);
            AddPowerUp(gameState.Bricks[indexes[7]], PowerUp.Type.PaddleShrink);
            AddPowerUp(gameState.Bricks[indexes[8]], PowerUp.Type.SpeedUp);
            AddPowerUp(gameState.Bricks[indexes[9]], PowerUp.Type.SpeedUp);
            AddPowerUp(gameState.Bricks[indexes[10]], PowerUp.Type.ReverseControls);
            AddPowerUp(gameState.Bricks[indexes[11]], PowerUp.Type.ReverseControls);
        }
    }
    
    private void AddPowerUp(Brick brick, PowerUp.Type powerUpType)
    {
        var rect = brick.GetRectangle();
        
        float x = rect.X + rect.Width / 2 - 10;
        float y = rect.Y + rect.Height / 2 - 10;
        
        gameState.PowerUps.Add(new PowerUp(x, y, powerUpType, brick));
    }
    
    private List<int> GetRandomUniqueIndexes(int count, int min, int max)
    {
        var indexes = new List<int>();
        if (max - min < count) return indexes;
        
        while (indexes.Count < count)
        {
            int index = Random.Shared.Next(min, max);
            if (!indexes.Contains(index))
            {
                indexes.Add(index);
            }
        }
        
        return indexes;
    }
    
    private void CreateBricksGrid(int rows, int cols, int brickWidth, int brickHeight, int spacing, int yOffset, 
                                 Func<int, int, bool> shouldCreateBrick, 
                                 Func<int, int, Color> getColor, 
                                 Func<int, int, int> getHitPoints)
    {
        // Calculate total width and left margin
        var totalWidth = cols * brickWidth + (cols - 1) * spacing;
        var leftMargin = (gameState.ScreenWidth - totalWidth) / 2;
        
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                if (!shouldCreateBrick(row, col))
                    continue;
                    
                var x = leftMargin + col * (brickWidth + spacing);
                var y = row * (brickHeight + spacing) + yOffset;
                var color = getColor(row, col);
                var hitPoints = getHitPoints(row, col);
                
                gameState.Bricks.Add(new Brick(x, y, brickWidth, brickHeight, color, hitPoints));
            }
        }
    }
    
    private void CreateStandardLevel()
    {
        const int brickRows = 5;
        const int bricksPerRow = 6;
        const int brickWidth = 70;
        const int brickHeight = 30;
        const int brickSpacing = 10;
        const int initialY = 50;
        
        var colors = new[] { Color.Red, Color.Orange, Color.Yellow, Color.Green, Color.Purple };
        
        CreateBricksGrid(
            brickRows, bricksPerRow, brickWidth, brickHeight, brickSpacing, initialY,
            // Always create brick
            (row, col) => true,
            // Color based on row
            (row, col) => colors[row % colors.Length],
            // Hit points (top row is stronger)
            (row, col) => row == 0 ? 2 : 1
        );
        
        // Add special bricks to level 1
        AddSpecialBricks(0.15f); // 15% chance for special bricks
    }
    
    private void CreateAdvancedLevel()
    {
        const int brickRows = 6;
        const int bricksPerRow = 12;
        const int brickWidth = 60;
        const int brickHeight = 25;
        const int brickSpacing = 8;
        const int initialY = 40;
        
        var colors = new[] { Color.Red, Color.Orange, Color.Yellow, Color.Green, Color.Blue, Color.Purple };
        
        CreateBricksGrid(
            brickRows, bricksPerRow, brickWidth, brickHeight, brickSpacing, initialY,
            // Create brick based on row and column
            (row, col) => !(row % 2 == 0 && col % 2 != 0) && !(row % 2 != 0 && col % 3 == 0),
            // Color based on row
            (row, col) => colors[row % colors.Length],
            // Hit points (top two rows are stronger)
            (row, col) => row < 2 ? 2 : 1
        );
        
        // Add more special bricks to level 2
        AddSpecialBricks(0.25f); // 25% chance for special bricks
    }
    
    private void CreateFinalLevel()
    {
        const int brickRows = 7;
        const int bricksPerRow = 15;
        const int brickWidth = 50;
        const int brickHeight = 20;
        const int brickSpacing = 6;
        const int initialY = 30;
        
        var colors = new[] { 
            Color.Red, Color.Orange, Color.Yellow, Color.Green, 
            Color.Blue, Color.Purple, Color.Pink 
        };
        
        CreateBricksGrid(
            brickRows, bricksPerRow, brickWidth, brickHeight, brickSpacing, initialY,
            // Create brick based on row and column
            (row, col) => row == 0 || row == brickRows - 1 || col == 0 || col == bricksPerRow - 1 || 
                          col == row || col == bricksPerRow - row - 1 || col == bricksPerRow / 2 || row == brickRows / 2,
            // Color based on row and column
            (row, col) => colors[(row + col) % colors.Length],
            // Hit points (edges and center are stronger)
            (row, col) => (row == 0 || row == brickRows - 1 || col == 0 || col == bricksPerRow - 1 || 
                           col == bricksPerRow / 2 || row == brickRows / 2) ? 2 : 1
        );
        
        // Add even more special bricks to level 3
        AddSpecialBricks(0.35f); // 35% chance for special bricks
    }
    
    private void CreateBonusLevel()
    {
        // Double the points for all bricks in the bonus round
        gameState.SetScoreMultiplier(2);

        // Create a special pattern for the bonus round
        int brickWidth = 40;
        int brickHeight = 20;
        int spacing = 5;
        int yOffset = 80;
        int rows = 7;
        int cols = 13;

        CreateBricksGrid(
            rows, cols, brickWidth, brickHeight, spacing, yOffset,
            (row, col) => true, // Always create brick
            (row, col) => {
                // Create a colorful pattern for bonus level
                return row switch {
                    0 => Color.Red,
                    1 => Color.Orange,
                    2 => Color.Yellow,
                    3 => Color.Green,
                    4 => Color.Blue,
                    5 => Color.Purple,
                    _ => Color.Pink
                };
            },
            (row, col) => {
                // Make some bricks stronger in the bonus round
                if ((row + col) % 3 == 0)
                    return 2;
                return 1;
            }
        );

        // Add more special bricks for the bonus round
        AddSpecialBricks(0.3f);  // 30% chance for special bricks
        
        // Add extra power-ups for bonus round
        AddBonusRoundPowerUps();
    }
    
    private void AddSpecialBricks(float specialBrickChance)
    {
        // Define probabilities for each special type
        float explosiveProbability = 0.4f;  // 40% of special bricks are explosive
        float invincibleProbability = 0.2f; // 20% of special bricks are invincible
        // 40% of special bricks are multi-score
        
        foreach (var brick in gameState.Bricks)
        {
            if (Random.Shared.NextDouble() < specialBrickChance)
            {
                double typeRoll = Random.Shared.NextDouble();
                
                if (typeRoll < explosiveProbability)
                {
                    brick.Type = Brick.BrickType.Explosive;
                }
                else if (typeRoll < explosiveProbability + invincibleProbability)
                {
                    brick.Type = Brick.BrickType.Invincible;
                }
                else
                {
                    brick.Type = Brick.BrickType.MultiScore;
                }
            }
        }
    }
    
    private void AddBonusRoundPowerUps()
    {
        if (gameState.Bricks.Count < 20) return;
        
        // Get random unique brick indices
        var indexes = GetRandomUniqueIndexes(20, 0, gameState.Bricks.Count);
        
        if (indexes.Count >= 20)
        {
            // Add more power-ups than usual for the bonus round
            for (int i = 0; i < 5; i++)
            {
                AddPowerUp(gameState.Bricks[indexes[i]], PowerUp.Type.PaddleGrow);
                AddPowerUp(gameState.Bricks[indexes[i+5]], PowerUp.Type.ExtraBall);
                AddPowerUp(gameState.Bricks[indexes[i+10]], PowerUp.Type.Gun);
            }
        }
    }
    
    public bool AreAllBricksDestroyed()
    {
        // Check if there are any non-invincible bricks left
        // Level should be considered complete if only invincible bricks remain
        return !gameState.Bricks.Any(brick => brick.Type != Brick.BrickType.Invincible);
    }
    
    public void DrawBricks()
    {
        foreach (var brick in gameState.Bricks)
        {
            brick.Draw();
        }
        
        foreach (var powerUp in gameState.PowerUps)
        {
            powerUp.Draw();
        }
    }
    
    public void UpdatePowerUps()
    {
        for (int i = gameState.PowerUps.Count - 1; i >= 0; i--)
        {
            var powerUp = gameState.PowerUps[i];
            powerUp.Update();
            
            if (powerUp.Position.Y > gameState.ScreenHeight)
            {
                gameState.PowerUps.RemoveAt(i);
            }
        }
    }
    
    public void ReleasePowerUp(Brick brick)
    {
        var powerUp = gameState.PowerUps.FirstOrDefault(p => p.Brick == brick);
        if (powerUp != null)
        {
            powerUp.Release();
        }
    }
}
