namespace Breakout;

public class Game(GameState gameState)
{
    private readonly Dictionary<Type, ManagerBase> _managers = new()
    {
        { typeof(SoundManager), new SoundManager(gameState) },
        { typeof(StateManager), new StateManager(gameState) },
        { typeof(UIManager), new UIManager(gameState) },
        { typeof(CollisionManager), new CollisionManager(gameState) },
        { typeof(LevelManager), new LevelManager(gameState) },
        { typeof(ScoreManager), new ScoreManager(gameState) },
        { typeof(ExplosionManager), new ExplosionManager(gameState) },
        { typeof(TimeChallengeManager), new TimeChallengeManager(gameState) },
        { typeof(MenuManager), new MenuManager(gameState) },
        { typeof(PowerUpManager), new PowerUpManager(gameState) }
    };

    private SoundManager soundManager => (SoundManager)_managers[typeof(SoundManager)];
    private StateManager stateManager => (StateManager)_managers[typeof(StateManager)];
    private UIManager uiManager => (UIManager)_managers[typeof(UIManager)];
    private CollisionManager collisionManager => (CollisionManager)_managers[typeof(CollisionManager)];
    private LevelManager levelManager => (LevelManager)_managers[typeof(LevelManager)];
    private ScoreManager scoreManager => (ScoreManager)_managers[typeof(ScoreManager)];
    private ExplosionManager explosionManager => (ExplosionManager)_managers[typeof(ExplosionManager)];
    private TimeChallengeManager timeChallengeManager => (TimeChallengeManager)_managers[typeof(TimeChallengeManager)];
    private MenuManager menuManager => (MenuManager)_managers[typeof(MenuManager)];
    private PowerUpManager powerUpManager => (PowerUpManager)_managers[typeof(PowerUpManager)];

    private Camera2D _camera;

    public void Run()
    {
        Initialize();
        
        while (!Raylib.WindowShouldClose() && !gameState.ShouldExit)
        {
            var deltaTime = Raylib.GetFrameTime();
            Update(deltaTime);
            Draw();
        }
        
        Cleanup();
    }
    
    private void Initialize()
    {
        Raylib.SetConfigFlags(ConfigFlags.HighDpiWindow);
        Raylib.InitWindow(gameState.ScreenWidth, gameState.ScreenHeight, gameState.WindowTitle);
        Raylib.SetTargetFPS(60);
        
        foreach (var manager in _managers.Values)
        {
            manager.Initialize();
        }

        gameState.ResetBallAndPaddle();
        
        // Initialize camera with proper offset to center the game
        _camera = new Camera2D
        {
            Target = new Vector2(0, 0),
            Offset = new Vector2(gameState.ScreenWidth / 2, gameState.ScreenHeight / 2), // Center offset
            Rotation = 0.0f,
            Zoom = 1.0f
        };
        
        // Don't apply any DPI scaling - it causes more issues than it solves
        // We'll handle position/size calculation directly based on window dimensions
        
        // Start at main menu instead of directly creating level
        gameState.SetMainMenu();
    }
    
    private void Update(float deltaTime)
    {
        // Update all managers with deltaTime
        foreach (var manager in _managers.Values)
        {
            manager.Update(deltaTime);
        }
        
        // Handle only P key for pausing/unpausing (removed Escape key)
        if (Raylib.IsKeyPressed(KeyboardKey.P))
        {
            if (gameState.CurrentState == GameState.State.Playing)
            {
                gameState.SetPauseMenu();
                EventBus.Publish(new GamePausedEvent());
            }
            else if (gameState.CurrentState == GameState.State.PauseMenu)
            {
                gameState.SetPlaying();
                EventBus.Publish(new GameResumedEvent());
            }
        }

        // Only handle Playing state - other states are managed by StateManager
        if (gameState.CurrentState == GameState.State.Playing)
        {
            UpdatePlaying(deltaTime);
        }
        // Check for Enter key to restart in GameOver or GameWon states
        else if ((gameState.CurrentState == GameState.State.GameOver || 
                gameState.CurrentState == GameState.State.GameWon) && 
                Raylib.IsKeyPressed(KeyboardKey.Enter))
        {
            stateManager.RestartGame();
        }
    }
    
    private void UpdatePlaying(float deltaTime)
    {
        gameState.Paddle.Update(gameState.ScreenWidth);
        gameState.MainBall.Update();
        
        //powerUpManager.Update(deltaTime);
        levelManager.UpdatePowerUps();
        
        collisionManager.HandleWallCollisions(gameState.MainBall);
        collisionManager.HandlePaddleCollision(gameState.MainBall);
        collisionManager.HandleBrickCollisions(gameState.MainBall, gameState.Bricks);
        collisionManager.HandleBulletCollisions(gameState.Bullets, gameState.Bricks, powerUpManager);
        
        collisionManager.HandlePowerUpCollisions(gameState.Paddle, gameState.PowerUps);
        
        ProcessExtraBalls();
        
        if (collisionManager.IsBallLost(gameState.MainBall))
        {
            // Just publish the event - StateManager will handle lives and game over
            EventBus.Publish(new BallLostEvent());
        }
    }
    
    private void ProcessExtraBalls()
    {
        foreach (var ball in gameState.ExtraBalls.ToList())
        {
            collisionManager.HandleWallCollisions(ball);
            collisionManager.HandlePaddleCollision(ball);
            collisionManager.HandleBrickCollisions(ball, gameState.Bricks);
            
            if (collisionManager.IsBallLost(ball))
            {
                powerUpManager.RemoveLostBall(ball);
            }
        }
    }
    
    private void Draw()
    {
        Raylib.BeginDrawing();
        Raylib.ClearBackground(Color.Black);
        
        // Apply screen shake to camera
        explosionManager.ApplyScreenShake(_camera);
        
        // Only draw game elements when not in main menu
        if (gameState.CurrentState != GameState.State.MainMenu)
        {
            // Begin camera mode with screen shake
            //Raylib.BeginMode2D(_camera);
            
            // Draw bricks normally - removed shader effects
            levelManager.DrawBricks();
            
            // Draw explosions
            explosionManager.Draw();
            
            // Draw balls normally - removed shader effects
            gameState.MainBall.Draw();
            
            // Draw extra balls normally
            foreach (var extraBall in gameState.ExtraBalls)
            {
                extraBall.Draw();
            }
            
            // Draw paddle and bullets
            gameState.Paddle.Draw();
            powerUpManager.DrawGun();
            powerUpManager.DrawBullets();

            //Raylib.EndMode2D(); // End camera mode
            
            // Draw UI elements without camera effects (no shake)
            uiManager.DrawUI();
            uiManager.DrawPowerUpInfo(); // Updated to not need powerUpManager
            
            scoreManager.Draw();
            timeChallengeManager.Draw();
        }
        
        // Draw menu on top of everything if active
        menuManager.Draw();
        
        Raylib.EndDrawing();
    }
    
    private void Cleanup()
    {
        foreach (var manager in _managers.Values)
        {
            manager.Cleanup();
        }
        
        Raylib.CloseWindow();
    }
}
