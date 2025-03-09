namespace Breakout;

public class GameState(int screenWidth, int screenHeight, string windowTitle)
{
    private const int MaxLives = 3;
    
    public enum State
    {
        Playing,
        BallLost,
        GameOver,
        GameWon,
        MainMenu,
        PauseMenu
    }
    
    public enum Mode
    {
        Classic,
        TimeChallenge,
        InfiniteLevels,
        ZenMode      // No lives, just play for fun
    }
    
    public State CurrentState { get; private set; } = State.MainMenu;
    public Mode GameMode { get; private set; } = Mode.Classic;
    public int Score { get; private set; } = 0;
    public int Lives { get; private set; } = 3;
    public int ScreenWidth { get; } = screenWidth;
    public int ScreenHeight { get; } = screenHeight;
    public string WindowTitle { get; } = windowTitle;
    
    public Ball MainBall { get; } = new Ball(screenWidth / 2, screenHeight / 2, 5, 5, 10);
    public Paddle Paddle { get; } = new Paddle(screenWidth / 2 - 50, screenHeight - 30, 100, 20);
    
    public bool ShouldExit { get; set; } = false;

    public List<Brick> Bricks { get; } = [];
    public List<PowerUp> PowerUps { get; } = [];
    
    public int CurrentLevel { get; set; } = 1;
    public int MaxLevels { get; set; } = 3;

    // Add a property for ball speed multiplier that can be accessed by all managers
    public float BallSpeedMultiplier { get; private set; } = 1.0f;

    // Add these properties to track PowerUp states
    public Dictionary<PowerUp.Type, float> ActivePowerUpTimers { get; } = [];
    public Dictionary<PowerUp.Type, float> PowerUpDurations { get; } = new()
    {
        // Positive power-ups
        [PowerUp.Type.PaddleGrow] = 10.0f,
        [PowerUp.Type.ExtraBall] = float.MaxValue, // Extra balls last forever until lost
        [PowerUp.Type.Gun] = 15.0f, // Gun power-up lasts for 15 seconds
        
        // Negative power-ups (power-downs)
        [PowerUp.Type.PaddleShrink] = 8.0f,    // 8 seconds
        [PowerUp.Type.SpeedUp] = 12.0f,        // 12 seconds
        [PowerUp.Type.ReverseControls] = 7.0f   // 7 seconds
    };

    public List<Ball> ExtraBalls { get; } = [];
    public List<Bullet> Bullets { get; } = [];
    public int MaxBullets => 5;

    public bool IsPowerUpActive(PowerUp.Type type) => ActivePowerUpTimers.ContainsKey(type);

    public float GetRemainingTime(PowerUp.Type type)
    {
        if (!IsPowerUpActive(type)) return 0;
        return PowerUpDurations[type] - ActivePowerUpTimers[type];
    }

    public void SetBallSpeedMultiplier(float multiplier)
    {
        BallSpeedMultiplier = multiplier;
    }
    
    public void ResetBallSpeedMultiplier()
    {
        BallSpeedMultiplier = 1.0f;
    }

    public void ResetBallAndPaddle()
    {
        Paddle.Position = new Vector2(ScreenWidth / 2 - Paddle.Size.X / 2, ScreenHeight - 30);
        MainBall.Position = new Vector2(Paddle.Position.X + Paddle.Size.X / 2, Paddle.Position.Y - MainBall.Radius);
        MainBall.Speed = Vector2.Zero; // Ball will launch when Space is pressed
    }
    
    public void ClearLevel()
    {
        Bricks.Clear();
        PowerUps.Clear();
    }
    
    public void SetState(State newState)
    {
        var oldState = CurrentState;
        CurrentState = newState;
        EventBus.Publish(new GameStateChangedEvent(oldState, newState));
    }
    
    public void SetPlaying() => SetState(State.Playing);
    public void SetBallLost() => SetState(State.BallLost);
    public void SetGameOver() => SetState(State.GameOver);
    public void SetGameWon() => SetState(State.GameWon);
    public void SetMainMenu() => SetState(State.MainMenu);
    public void SetPauseMenu() => SetState(State.PauseMenu);
    
    public bool InBonusRound { get; private set; } = false;
    private int _scoreMultiplier = 1;

    public void StartBonusRound()
    {
        InBonusRound = true;
        // Any other special bonus round settings can go here
    }

    public void EndBonusRound()
    {
        InBonusRound = false;
        _scoreMultiplier = 1;
    }

    public void SetScoreMultiplier(int multiplier)
    {
        _scoreMultiplier = multiplier;
    }

    public void AddScore(int points)
    {
        Score += (points * _scoreMultiplier);
        EventBus.Publish(new ScoreChangedEvent(Score));
    }

    public bool LoseLife()
    {
        Lives--;
        EventBus.Publish(new LivesChangedEvent(Lives));
        
        // Remove GameOverEvent from here to avoid duplication
        // Let StateManager handle determining game over state
        return Lives > 0;
    }
    
    public void AddLife()
    {
        if (Lives < MaxLives)
        {
            Lives++;
            EventBus.Publish(new LivesChangedEvent(Lives));
        }
    }
    
    public void Reset(int initialLives = 3)
    {
        Score = 0;
        Lives = Math.Min(initialLives, MaxLives);
        ResetBallAndPaddle();
        InBonusRound = false;
        _scoreMultiplier = 1;
        
        EventBus.Publish(new ScoreChangedEvent(Score));
        EventBus.Publish(new LivesChangedEvent(Lives));
    }
    
    public void SetGameMode(Mode mode)
    {
        GameMode = mode;
        
        // Adjust game parameters based on mode
        switch (mode)
        {
            case Mode.Classic:
                Lives = 3;
                break;
                
            case Mode.TimeChallenge:
                Lives = 1; // One life only in time challenge
                break;
                
            case Mode.InfiniteLevels:
                Lives = 3;
                // Set MaxLevels to a very high number
                MaxLevels = 100;
                break;
                
            case Mode.ZenMode:
                Lives = int.MaxValue; // Effectively infinite lives
                break;
        }
        
        EventBus.Publish(new GameModeChangedEvent(mode));
    }
}
