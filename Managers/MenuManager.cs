namespace Breakout.Managers;

public class MenuManager(GameState gameState) : ManagerBase(gameState)
{
    private List<MenuItem> _mainMenuItems = [];
    private List<MenuItem> _pauseMenuItems = [];
    private List<MenuItem> _modeSelectItems = [];
    private int _selectedIndex = 0;
    private MenuType _currentMenu = MenuType.Main;
    
    private enum MenuType
    {
        Main,
        Pause,
        ModeSelect
    }
    
    private class MenuItem
    {
        public string Text { get; set; }
        public Action OnSelect { get; set; }
        
        public MenuItem(string text, Action onSelect)
        {
            Text = text;
            OnSelect = onSelect;
        }
    }
    
    public override void Initialize()
    {
        // Set up main menu
        _mainMenuItems = [
            new MenuItem("Play Classic Mode", () => StartGame(GameState.Mode.Classic)),
            new MenuItem("Select Game Mode", () => _currentMenu = MenuType.ModeSelect),
            new MenuItem("View High Scores", () => EventBus.Publish(new ViewHighScoresEvent())),
            new MenuItem("Exit Game", () => gameState.ShouldExit = true)
        ];
        
        // Set up pause menu
        _pauseMenuItems = [
            new MenuItem("Resume Game", () => gameState.SetPlaying()),
            new MenuItem("Restart Level", () => EventBus.Publish(new LevelRestartEvent())),
            new MenuItem("Return to Main Menu", () => gameState.SetMainMenu()),
            new MenuItem("Exit Game", () => gameState.ShouldExit = true)
        ];
        
        // Set up mode select menu
        _modeSelectItems = [
            new MenuItem("Classic Mode", () => StartGame(GameState.Mode.Classic)),
            new MenuItem("Time Challenge", () => StartGame(GameState.Mode.TimeChallenge)),
            new MenuItem("Infinite Levels", () => StartGame(GameState.Mode.InfiniteLevels)),
            new MenuItem("Zen Mode", () => StartGame(GameState.Mode.ZenMode)),
            new MenuItem("Back", () => _currentMenu = MenuType.Main)
        ];
        
        // Subscribe to events
        EventBus.Subscribe<GameStateChangedEvent>(OnGameStateChanged);
    }
    
    public override void Cleanup()
    {
        EventBus.Unsubscribe<GameStateChangedEvent>(OnGameStateChanged);
    }
    
    private void OnGameStateChanged(GameStateChangedEvent evt)
    {
        if (evt.NewState == GameState.State.MainMenu)
        {
            _currentMenu = MenuType.Main;
            _selectedIndex = 0;
        }
        else if (evt.NewState == GameState.State.PauseMenu)
        {
            _currentMenu = MenuType.Pause;
            _selectedIndex = 0;
        }
    }
    
    private void StartGame(GameState.Mode mode)
    {
        gameState.SetGameMode(mode);
        gameState.Reset();
        EventBus.Publish(new GameRestartEvent(3));
        gameState.SetBallLost();
    }
    
    public override void Update(float deltaTime)
    {
        switch (gameState.CurrentState)
        {
            case GameState.State.MainMenu:
                UpdateMainMenu();
                break;
                
            case GameState.State.PauseMenu:
                UpdatePauseMenu();
                break;
                
            default:
                return;
        }
    }
    
    private void UpdateMainMenu()
    {
        List<MenuItem> currentItems = _currentMenu switch
        {
            MenuType.Main => _mainMenuItems,
            MenuType.Pause => _pauseMenuItems,
            MenuType.ModeSelect => _modeSelectItems,
            _ => _mainMenuItems
        };
        
        // Handle navigation
        if (Raylib.IsKeyPressed(KeyboardKey.Down))
        {
            _selectedIndex = (_selectedIndex + 1) % currentItems.Count;
            EventBus.Publish(new MenuNavigationEvent());
        }
        else if (Raylib.IsKeyPressed(KeyboardKey.Up))
        {
            _selectedIndex = (_selectedIndex - 1 + currentItems.Count) % currentItems.Count;
            EventBus.Publish(new MenuNavigationEvent());
        }
        
        // Handle selection
        if (Raylib.IsKeyPressed(KeyboardKey.Enter))
        {
            currentItems[_selectedIndex].OnSelect();
            EventBus.Publish(new MenuSelectionEvent());
        }
        
        // Handle back key
        if (Raylib.IsKeyPressed(KeyboardKey.Escape) && _currentMenu == MenuType.ModeSelect)
        {
            _currentMenu = MenuType.Main;
            _selectedIndex = 0;
            EventBus.Publish(new MenuNavigationEvent());
        }
    }
    
    private void UpdatePauseMenu()
    {
        if (Raylib.IsKeyPressed(KeyboardKey.Enter))
        {
            // Resume the game
            gameState.SetPlaying();
            EventBus.Publish(new GameResumedEvent());
            Console.WriteLine("Resumed from pause menu"); // Debug info
        }
        
        if (Raylib.IsKeyPressed(KeyboardKey.M))
        {
            // Return to main menu
            gameState.SetMainMenu();
            EventBus.Publish(new MenuNavigationEvent());
        }
        
        if (Raylib.IsKeyPressed(KeyboardKey.R))
        {
            // Restart the game
            EventBus.Publish(new GameRestartEvent());
            gameState.SetBallLost(); // Start with ball on paddle
            EventBus.Publish(new MenuSelectionEvent());
        }
    }
    
    public override void Draw()
    {
        switch (gameState.CurrentState)
        {
            case GameState.State.MainMenu:
                DrawMainMenu();
                break;
                
            case GameState.State.PauseMenu:
                DrawPauseMenu();
                break;
                
            default:
                return;
        }
    }
    
    private void DrawMainMenu()
    {
        List<MenuItem> currentItems = _currentMenu switch
        {
            MenuType.Main => _mainMenuItems,
            MenuType.Pause => _pauseMenuItems,
            MenuType.ModeSelect => _modeSelectItems,
            _ => _mainMenuItems
        };
        
        string title = _currentMenu switch
        {
            MenuType.Main => "BREAKOUT",
            MenuType.Pause => "GAME PAUSED",
            MenuType.ModeSelect => "SELECT MODE",
            _ => "MENU"
        };
        
        // Draw semi-transparent background
        Raylib.DrawRectangle(0, 0, gameState.ScreenWidth, gameState.ScreenHeight, new Color(0, 0, 0, 200));
        
        // Draw title
        int titleFontSize = 40;
        int titleWidth = Raylib.MeasureText(title, titleFontSize);
        Raylib.DrawText(title, gameState.ScreenWidth/2 - titleWidth/2, 100, titleFontSize, Color.White);
        
        // Draw menu items
        int itemY = 200;
        int itemFontSize = 24;
        int itemSpacing = 40;
        
        for (int i = 0; i < currentItems.Count; i++)
        {
            bool isSelected = i == _selectedIndex;
            Color itemColor = isSelected ? Color.Yellow : Color.White;
            string itemText = currentItems[i].Text;
            
            if (isSelected)
            {
                itemText = "> " + itemText + " <";
                // Add slight animation for selected item
                float pulse = 1.0f + MathF.Sin((float)Raylib.GetTime() * 5) * 0.05f;
                itemFontSize = (int)(24 * pulse);
            }
            
            int itemWidth = Raylib.MeasureText(itemText, itemFontSize);
            Raylib.DrawText(itemText, gameState.ScreenWidth/2 - itemWidth/2, itemY, itemFontSize, itemColor);
            
            itemY += itemSpacing;
        }
        
        // Draw instructions
        Raylib.DrawText("Use UP/DOWN arrows to navigate", 
                        gameState.ScreenWidth/2 - 150, gameState.ScreenHeight - 60, 16, Color.Gray);
        Raylib.DrawText("Press ENTER to select", 
                        gameState.ScreenWidth/2 - 80, gameState.ScreenHeight - 40, 16, Color.Gray);
    }
    
    private void DrawPauseMenu()
    {
        // Semi-transparent overlay for pause menu
        Raylib.DrawRectangle(0, 0, gameState.ScreenWidth, gameState.ScreenHeight, new Color(0, 0, 0, 150));
        
        // Pause menu title
        Raylib.DrawText("PAUSED", gameState.ScreenWidth / 2 - 100, gameState.ScreenHeight / 2 - 100, 50, Color.White);
        
        // Menu options (removed ESC from the text)
        int yPos = gameState.ScreenHeight / 2;
        Raylib.DrawText("Press P or ENTER to Resume", gameState.ScreenWidth / 2 - 140, yPos, 20, Color.White);
        yPos += 40;
        Raylib.DrawText("Press R to Restart", gameState.ScreenWidth / 2 - 100, yPos, 20, Color.White);
        yPos += 40;
        Raylib.DrawText("Press M for Main Menu", gameState.ScreenWidth / 2 - 120, yPos, 20, Color.White);
        
        // Draw current score and level at the top
        Raylib.DrawText($"Score: {gameState.Score}", 20, 20, 20, Color.Yellow);
        Raylib.DrawText($"Level: {gameState.CurrentLevel}/{gameState.MaxLevels}", gameState.ScreenWidth - 150, 20, 20, Color.Yellow);
    }
}
