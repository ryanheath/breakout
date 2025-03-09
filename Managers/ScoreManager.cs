namespace Breakout.Managers;

public class ScoreManager(GameState gameState) : ManagerBase(gameState)
{
    private int _combo = 0;
    private float _comboTimer = 0;
    private const float ComboTimeWindow = 1.5f; // Time window for combo in seconds
    private const int MaxCombo = 8; // Maximum combo multiplier
    
    private readonly List<ScorePopup> _scorePopups = [];
    
    // Keep track of recently scored points for visualization
    private class ScorePopup
    {
        public string Text { get; }
        public Vector2 Position { get; }
        public float Timer { get; set; }
        public Color Color { get; }
        public float Scale { get; set; } = 1.0f;
        
        public ScorePopup(string text, Vector2 position, Color color)
        {
            Text = text;
            Position = position;
            Timer = 1.0f; // 1 second display time
            Color = color;
        }
    }

    public override void Initialize()
    {
        // Subscribe to events
        EventBus.Subscribe<BrickHitEvent>(OnBrickHit);
        EventBus.Subscribe<BallLostEvent>(OnBallLost);
        EventBus.Subscribe<LevelCompletedEvent>(OnLevelCompleted);
        EventBus.Subscribe<PowerUpCollectedEvent>(OnPowerUpCollected);
        EventBus.Subscribe<GameRestartEvent>(OnGameRestart);
        EventBus.Subscribe<BonusRoundCompletedEvent>(OnBonusRoundCompleted);
    }
    
    public override void Cleanup()
    {
        // Unsubscribe from events
        EventBus.Unsubscribe<BrickHitEvent>(OnBrickHit);
        EventBus.Unsubscribe<BallLostEvent>(OnBallLost);
        EventBus.Unsubscribe<LevelCompletedEvent>(OnLevelCompleted);
        EventBus.Unsubscribe<PowerUpCollectedEvent>(OnPowerUpCollected);
        EventBus.Unsubscribe<GameRestartEvent>(OnGameRestart);
        EventBus.Unsubscribe<BonusRoundCompletedEvent>(OnBonusRoundCompleted);
    }
    
    private void OnBrickHit(BrickHitEvent evt)
    {
        if (evt.Destroyed)
        {
            // Increment combo if hit is within time window
            if (_comboTimer > 0)
            {
                _combo = Math.Min(_combo + 1, MaxCombo);
            }
            else
            {
                _combo = 1;
            }
            
            // Reset combo timer
            _comboTimer = ComboTimeWindow;
            
            // Calculate score with combo multiplier
            int baseScore = 10;
            int comboMultiplier = _combo;
            
            // Apply extra multiplier for MultiScore bricks
            int brickMultiplier = evt.Brick.Type == Brick.BrickType.MultiScore ? 3 : 1;
            int totalScore = baseScore * comboMultiplier * brickMultiplier;
            
            // Add score to total
            gameState.AddScore(totalScore);
            
            // Create a score popup at the brick position
            Rectangle brickRect = evt.Brick.GetRectangle();
            Vector2 position = new(
                brickRect.X + brickRect.Width / 2,
                brickRect.Y + brickRect.Height / 2
            );
            
            string text = $"+{totalScore}";
            if (comboMultiplier > 1)
            {
                text += $" x{comboMultiplier}";
            }
            if (brickMultiplier > 1)
            {
                text += $" (x{brickMultiplier})";
            }
            
            // Determine color based on combo level
            Color popupColor = GetComboColor(_combo);
            
            _scorePopups.Add(new ScorePopup(text, position, popupColor));
        }
    }
    
    private Color GetComboColor(int combo)
    {
        return combo switch
        {
            1 => Color.White,
            2 => Color.Green,
            3 => Color.Yellow,
            4 => Color.Orange,
            5 => Color.Red,
            6 => Color.Purple,
            7 => Color.Blue,
            _ => Color.Pink
        };
    }
    
    private void OnBallLost(BallLostEvent evt)
    {
        ResetCombo();
    }
    
    private void OnLevelCompleted(LevelCompletedEvent evt)
    {
        // Add bonus score for completing a level
        int levelBonus = 50 * evt.LevelNumber;
        gameState.AddScore(levelBonus);
        
        // Create a score popup for level completion bonus
        Vector2 position = new(
            gameState.ScreenWidth / 2,
            gameState.ScreenHeight / 2 - 80
        );
        
        _scorePopups.Add(new ScorePopup($"LEVEL BONUS: +{levelBonus}", position, Color.Gold));
        
        gameState.AddLife(); // Bonus life for completing level
        ResetCombo();
    }
    
    private void OnPowerUpCollected(PowerUpCollectedEvent evt)
    {
        // Award points for collecting power-ups
        gameState.AddScore(5);
        
        // Create a popup for power-up collection
        Vector2 position = new(
            gameState.Paddle.Position.X + gameState.Paddle.Size.X / 2,
            gameState.Paddle.Position.Y - 20
        );
        
        _scorePopups.Add(new ScorePopup("+5", position, Color.RayWhite));
    }
    
    private void OnGameRestart(GameRestartEvent evt)
    {
        gameState.Reset(evt.InitialLives);
        ResetCombo();
        _scorePopups.Clear();
    }
    
    private void OnBonusRoundCompleted(BonusRoundCompletedEvent evt)
    {
        // Award a big bonus for completing the bonus round
        int bonusPoints = 1000;
        gameState.AddScore(bonusPoints);
        
        Vector2 position = new(
            gameState.ScreenWidth / 2,
            gameState.ScreenHeight / 2 - 80
        );
        
        _scorePopups.Add(new ScorePopup($"BONUS ROUND COMPLETE: +{bonusPoints}", position, Color.Gold));
        
        // Award an extra life for completing the bonus round
        gameState.AddLife();
        
        // Reset combo
        ResetCombo();
    }
    
    private void ResetCombo()
    {
        _combo = 0;
        _comboTimer = 0;
    }
    
    public override void Update(float deltaTime)
    {
        // Update combo timer
        if (_comboTimer > 0)
        {
            _comboTimer -= deltaTime;
            if (_comboTimer <= 0)
            {
                ResetCombo();
            }
        }
        
        // Update score popups
        for (int i = _scorePopups.Count - 1; i >= 0; i--)
        {
            var popup = _scorePopups[i];
            popup.Timer -= deltaTime;
            
            // Animate the popup (grow initially, then shrink)
            if (popup.Timer > 0.8f)
            {
                popup.Scale = 1.0f + (1.0f - popup.Timer) * 5; // Grow effect
            }
            else
            {
                popup.Scale = popup.Timer * 1.25f; // Shrink effect
            }
            
            if (popup.Timer <= 0)
            {
                _scorePopups.RemoveAt(i);
            }
        }
    }
    
    public override void Draw()
    {
        // Draw score popups
        foreach (var popup in _scorePopups)
        {
            int fontSize = (int)(16 * popup.Scale);
            if (fontSize <= 0) continue;
            
            Color color = popup.Color;
            color.A = (byte)(255 * popup.Timer); // Fade out
            
            int textWidth = Raylib.MeasureText(popup.Text, fontSize);
            float x = popup.Position.X - textWidth / 2;
            float y = popup.Position.Y - fontSize / 2;
            
            // Ensure text is visible against any background by drawing a dark semi-transparent background
            float padding = 4;
            Raylib.DrawRectangle(
                (int)(x - padding), 
                (int)(y - padding),
                textWidth + (int)(padding * 2),
                fontSize + (int)(padding * 2),
                new Color(0, 0, 0, 150)
            );
            
            // Draw the text
            Raylib.DrawText(
                popup.Text, 
                (int)x, 
                (int)y, 
                fontSize, 
                color
            );
        }
        
        // Draw current combo if active
        if (_combo > 1)
        {
            string comboText = $"COMBO x{_combo}";
            int fontSize = 20 + _combo; // Larger font for higher combos
            
            Color comboColor = GetComboColor(_combo);
            float comboAlpha = _comboTimer / ComboTimeWindow;
            comboColor.A = (byte)(255 * comboAlpha);
            
            // Position at top of screen
            int textWidth = Raylib.MeasureText(comboText, fontSize);
            int x = gameState.ScreenWidth / 2 - textWidth / 2;
            int y = 40;
            
            // Draw background for better visibility
            Raylib.DrawRectangle(
                x - 10, 
                y - 5, 
                textWidth + 20, 
                fontSize + 10, 
                new Color(0, 0, 0, 180)
            );
            
            // Draw text with pulsing size effect
            float scale = 1.0f + (float)Math.Sin(_comboTimer * 10) * 0.1f;
            Raylib.DrawText(comboText, x, y, fontSize, comboColor);
        }
    }
}
