namespace Breakout.Managers;

public class StateManager(GameState gameState) : ManagerBase(gameState)
{
    private float _levelCompletionTimer = 0;
    private const float LevelAdvanceDelay = 2.0f;
    private bool _isBonusRoundAvailable = false;
    private bool _inBonusRound = false;
    private readonly List<KeyboardKey> _cheatCodeSequence = new List<KeyboardKey> { KeyboardKey.B, KeyboardKey.O, KeyboardKey.N, KeyboardKey.U, KeyboardKey.S };
    private readonly List<KeyboardKey> _currentCheatInput = new List<KeyboardKey>();
    private float _cheatInputTimer = 0f;
    private const float CheatInputTimeout = 2.0f; // 2 seconds timeout between key presses

    public override void Initialize()
    {
        EventBus.Subscribe<BallLostEvent>(OnBallLost);
        EventBus.Subscribe<GameStateChangedEvent>(OnGameStateChanged);
        EventBus.Subscribe<LevelAdvancedEvent>(OnLevelAdvanced);
        EventBus.Subscribe<AllLevelsCompletedEvent>(OnAllLevelsCompleted);
        EventBus.Subscribe<AllBricksDestroyedEvent>(OnAllBricksDestroyed);
        EventBus.Subscribe<GameRestartEvent>(OnGameRestart);
        EventBus.Subscribe<BonusRoundRequestEvent>(OnBonusRoundRequest);
        EventBus.Subscribe<BonusRoundCompletedEvent>(OnBonusRoundCompleted);
    }

    public override void Cleanup()
    {
        EventBus.Unsubscribe<BallLostEvent>(OnBallLost);
        EventBus.Unsubscribe<GameStateChangedEvent>(OnGameStateChanged);
        EventBus.Unsubscribe<LevelAdvancedEvent>(OnLevelAdvanced);
        EventBus.Unsubscribe<AllLevelsCompletedEvent>(OnAllLevelsCompleted);
        EventBus.Unsubscribe<AllBricksDestroyedEvent>(OnAllBricksDestroyed);
        EventBus.Unsubscribe<GameRestartEvent>(OnGameRestart);
        EventBus.Unsubscribe<BonusRoundRequestEvent>(OnBonusRoundRequest);
        EventBus.Unsubscribe<BonusRoundCompletedEvent>(OnBonusRoundCompleted);
    }
    
    private void OnAllBricksDestroyed(AllBricksDestroyedEvent evt)
    {
        _levelCompletionTimer = 0;
        
        var remainingLevels = gameState.MaxLevels - evt.LevelNumber;
        EventBus.Publish(new LevelCompletedEvent(evt.LevelNumber, remainingLevels));
        
        // Set game state to GameWon to trigger level completion sequence
        gameState.SetGameWon();
    }
    
    private void OnBallLost(BallLostEvent evt)
    {
        // When we lose a ball, decrease lives and check for game over
        bool hasLivesLeft = gameState.LoseLife();
        
        if (!hasLivesLeft)
        {
            // Game over - no more lives
            gameState.SetGameOver();
            
            // No need to check for high score here - UIManager handles high scores
            EventBus.Publish(new GameOverEvent(gameState.Score));
        }
        else
        {
            // Reset ball position if we still have lives
            gameState.ResetBallAndPaddle();
            gameState.SetBallLost(); // Change state to BallLost to wait for player input
        }
    }
    
    private void OnGameStateChanged(GameStateChangedEvent evt)
    {
        // Handle any game state transitions if needed
    }
    
    private void OnLevelAdvanced(LevelAdvancedEvent evt)
    {
        // When advancing levels, add a life as a bonus (handled in LevelManager)
        gameState.ResetBallAndPaddle();
        gameState.SetBallLost(); // Set to ball lost state to wait for player input
    }
    
    private void OnAllLevelsCompleted(AllLevelsCompletedEvent evt)
    {
        // Instead of ending the game, offer a bonus round
        _levelCompletionTimer = 0;
        _isBonusRoundAvailable = true;
        gameState.SetGameWon();
    }

    private void OnGameRestart(GameRestartEvent evt)
    {
        // Reset all game state
        gameState.Reset(evt.InitialLives);
        gameState.CurrentLevel = 1;
        
        // Clear any existing power-ups, extra balls, etc.
        gameState.PowerUps.Clear();
        gameState.ExtraBalls.Clear();
        gameState.Bullets.Clear();
        gameState.ActivePowerUpTimers.Clear();
                
        // Set game state to BallLost to position ball and wait for player input
        gameState.SetBallLost();
        _isBonusRoundAvailable = false;
        _inBonusRound = false;
    }

    private void OnBonusRoundRequest(BonusRoundRequestEvent evt)
    {
        _inBonusRound = true;
        gameState.StartBonusRound();
    }

    private void OnBonusRoundCompleted(BonusRoundCompletedEvent evt)
    {
        _inBonusRound = false;
        gameState.SetGameWon();
        EventBus.Publish(new GameWonEvent(gameState.Score));
    }

    public override void Update(float deltaTime)
    {
        // Handle level completion timer logic here
        if (gameState.CurrentState == GameState.State.GameWon)
        {
            UpdateLevelCompletion(deltaTime);
        }
        else if (gameState.CurrentState == GameState.State.BallLost)
        {
            UpdateBallLost();
        }

        // Update cheat code input timer
        if (_currentCheatInput.Count > 0)
        {
            _cheatInputTimer += deltaTime;
            if (_cheatInputTimer >= CheatInputTimeout)
            {
                // Reset cheat code if timeout occurs
                _currentCheatInput.Clear();
                _cheatInputTimer = 0f;
            }
        }

        // Check for cheat code input when not in bonus round
        if (!gameState.InBonusRound && 
            (gameState.CurrentState == GameState.State.Playing ||
             gameState.CurrentState == GameState.State.BallLost))
        {
            CheckForCheatCode();
        }
    }
    
    private void CheckForCheatCode()
    {
        // Get the key that was just pressed
        for (KeyboardKey key = KeyboardKey.A; key <= KeyboardKey.Z; key++)
        {
            if (Raylib.IsKeyPressed(key))
            {
                // Add key to current input sequence
                _currentCheatInput.Add(key);
                _cheatInputTimer = 0f; // Reset timer on new input
                
                // If sequence is too long, remove first element
                if (_currentCheatInput.Count > _cheatCodeSequence.Count)
                {
                    _currentCheatInput.RemoveAt(0);
                }
                
                // Check if the sequence matches the cheat code
                if (_currentCheatInput.Count == _cheatCodeSequence.Count)
                {
                    bool match = true;
                    for (int i = 0; i < _cheatCodeSequence.Count; i++)
                    {
                        if (_currentCheatInput[i] != _cheatCodeSequence[i])
                        {
                            match = false;
                            break;
                        }
                    }
                    
                    if (match)
                    {
                        // Cheat code activated! Trigger bonus round
                        ActivateBonusRoundCheat();
                    }
                }
                
                break; // Only process one key press per frame
            }
        }
    }
    
    private void ActivateBonusRoundCheat()
    {
        // Clear the current cheat input
        _currentCheatInput.Clear();
        
        // Trigger the bonus round immediately
        EventBus.Publish(new BonusRoundRequestEvent(true));
        
        // Set game state to ball lost to allow player to launch
        gameState.SetBallLost();
        
        // Show message about cheat being activated
        EventBus.Publish(new CheatActivatedEvent("BONUS ROUND"));
    }

    private void UpdateLevelCompletion(float deltaTime)
    {
        _levelCompletionTimer += deltaTime;
        
        if (_levelCompletionTimer >= LevelAdvanceDelay)
        {
            _levelCompletionTimer = 0; // Reset timer
            
            // Check if there are more levels
            if (gameState.CurrentLevel < gameState.MaxLevels)
            {
                // Advance to next level
                EventBus.Publish(new LevelAdvanceRequestEvent());
                
                // After advancing, reset the state to BallLost to allow the player to launch the ball
                gameState.SetBallLost();
            }
            else if (_isBonusRoundAvailable)
            {
                // All levels completed, start the bonus round
                _isBonusRoundAvailable = false; // Ensure we only offer the bonus round once
                EventBus.Publish(new BonusRoundRequestEvent());
                gameState.SetBallLost(); // Wait for player to launch the ball
            }
            // If this was the last level and no bonus round available, keep the game won state
            // Let the player restart with Enter
        }
    }

    private void UpdateBallLost()
    {
        // Allow paddle movement before launching the ball
        gameState.Paddle.Update(gameState.ScreenWidth);
        
        // Position the ball on top of the paddle
        gameState.MainBall.Position = new Vector2(
            gameState.Paddle.Position.X + gameState.Paddle.Size.X / 2,
            gameState.Paddle.Position.Y - gameState.MainBall.Radius);
            
        // Launch the ball when Space is pressed
        if (Raylib.IsKeyPressed(KeyboardKey.Space))
        {
            var angle = Raylib.GetRandomValue(-30, 30) * MathF.PI / 180.0f;
            var speed = 7.0f;
            var initialVelocity = new Vector2(
                speed * MathF.Sin(angle),
                -speed * MathF.Cos(angle)
            );
            
            gameState.MainBall.Speed = initialVelocity;
            
            EventBus.Publish(new GameStartedEvent(gameState.MainBall, initialVelocity));
            gameState.SetPlaying();
        }
    }

    public void  RestartGame()
    {
        EventBus.Publish(new GameRestartEvent(3));
    }
}
