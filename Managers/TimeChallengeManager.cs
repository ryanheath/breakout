namespace Breakout.Managers;

public class TimeChallengeManager(GameState gameState) : ManagerBase(gameState)
{
    private float _remainingTime = 0;
    private const float InitialTimePerLevel = 60.0f; // 60 seconds per level
    private bool _isActive = false;
    
    public override void Initialize()
    {
        EventBus.Subscribe<GameStateChangedEvent>(OnGameStateChanged);
        EventBus.Subscribe<LevelAdvancedEvent>(OnLevelAdvanced);
        EventBus.Subscribe<GameRestartEvent>(OnGameRestart);
    }
    
    public override void Cleanup()
    {
        EventBus.Unsubscribe<GameStateChangedEvent>(OnGameStateChanged);
        EventBus.Unsubscribe<LevelAdvancedEvent>(OnLevelAdvanced);
        EventBus.Unsubscribe<GameRestartEvent>(OnGameRestart);
    }
    
    private void OnGameStateChanged(GameStateChangedEvent evt)
    {
        if (evt.NewState == GameState.State.Playing && gameState.GameMode == GameState.Mode.TimeChallenge)
        {
            _isActive = true;
        }
        else if (evt.NewState == GameState.State.GameOver || evt.NewState == GameState.State.GameWon)
        {
            _isActive = false;
        }
    }
    
    private void OnLevelAdvanced(LevelAdvancedEvent evt)
    {
        // Add bonus time for completing a level
        if (gameState.GameMode == GameState.Mode.TimeChallenge)
        {
            _remainingTime = InitialTimePerLevel;
            EventBus.Publish(new TimeBonusEvent(15.0f)); // 15 second bonus
        }
    }
    
    private void OnGameRestart(GameRestartEvent evt)
    {
        _remainingTime = InitialTimePerLevel;
    }
    
    public override void Update(float deltaTime)
    {
        if (!_isActive || gameState.GameMode != GameState.Mode.TimeChallenge) return;
        
        _remainingTime -= deltaTime;
        
        if (_remainingTime <= 0)
        {
            _remainingTime = 0;
            _isActive = false;
            
            // Game over due to time expiration
            EventBus.Publish(new TimerExpiredEvent());
            EventBus.Publish(new GameOverEvent(gameState.Score));
            gameState.SetGameOver();
        }
    }
    
    public override void Draw()
    {
        if (gameState.GameMode != GameState.Mode.TimeChallenge) return;
        
        // Draw time indicator at the top center of the screen
        string timeText = $"TIME: {_remainingTime:0.0}s";
        int fontSize = 20;
        int textWidth = Raylib.MeasureText(timeText, fontSize);
        
        // Change color based on remaining time
        Color timeColor = _remainingTime > 10 ? Color.White :
                          _remainingTime > 5 ? Color.Yellow : Color.Red;
        
        // Draw with pulsing effect for last 5 seconds
        if (_remainingTime <= 5)
        {
            float pulse = 1.0f + MathF.Sin(_remainingTime * 10) * 0.2f;
            fontSize = (int)(fontSize * pulse);
        }
        
        Raylib.DrawText(timeText, gameState.ScreenWidth/2 - textWidth/2, 35, fontSize, timeColor);
    }
    
    public void AddTime(float seconds)
    {
        _remainingTime += seconds;
    }
    
    public float GetRemainingTime() => _remainingTime;
}
