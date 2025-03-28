namespace Breakout.Managers;

public class UIManager(GameState gameState) : ManagerBase(gameState)
{
    private HighScoreManager _highScoreManager = null!;
    private bool _showHighScores = false;
    private bool _newHighScoreAchieved = false;
    private int _newHighScoreRank = 0;
    private int _highestScore = 0;
    private bool _showMusicToggleNotification = false;
    private float _musicToggleNotificationTimer = 0f;
    private const float MusicToggleNotificationDuration = 2f;
    private bool _musicEnabled = true;
    private bool _showSoundVolumeNotification = false;
    private float _soundVolumeNotificationTimer = 0f;
    private const float SoundVolumeNotificationDuration = 2f;
    private float _currentSoundVolume = 1.0f;
    private bool _showBonusRoundMessage = false;
    private float _bonusMessageTimer = 0f;
    private const float BonusMessageDuration = 3f;
    private bool _showCheatActivatedMessage = false;
    private float _cheatMessageTimer = 0f;
    private const float CheatMessageDuration = 3f;
    private string _cheatName = "";
    
    public override void Initialize()
    {
        _highScoreManager = new HighScoreManager(gameState);
        _highScoreManager.Initialize();
        
        // Get the highest score from the high score list
        var highScores = _highScoreManager.GetHighScores();
        _highestScore = highScores.Count > 0 ? highScores[0].Score : 0;
        
        EventBus.Subscribe<NewHighScoreEvent>(OnNewHighScore);
        EventBus.Subscribe<GameRestartEvent>(OnGameRestart);
        EventBus.Subscribe<ScoreChangedEvent>(OnScoreChanged);
        EventBus.Subscribe<MusicToggleEvent>(OnMusicToggle);
        EventBus.Subscribe<ViewHighScoresEvent>(OnViewHighScores);
        EventBus.Subscribe<SoundVolumeChangedEvent>(OnSoundVolumeChanged);
        EventBus.Subscribe<BonusRoundStartedEvent>(OnBonusRoundStarted);
        EventBus.Subscribe<CheatActivatedEvent>(OnCheatActivated);
    }
    
    public override void Cleanup()
    {
        EventBus.Unsubscribe<NewHighScoreEvent>(OnNewHighScore);
        EventBus.Unsubscribe<GameRestartEvent>(OnGameRestart);
        EventBus.Unsubscribe<ScoreChangedEvent>(OnScoreChanged);
        EventBus.Unsubscribe<MusicToggleEvent>(OnMusicToggle);
        EventBus.Unsubscribe<ViewHighScoresEvent>(OnViewHighScores);
        EventBus.Unsubscribe<SoundVolumeChangedEvent>(OnSoundVolumeChanged);
        EventBus.Unsubscribe<BonusRoundStartedEvent>(OnBonusRoundStarted);
        EventBus.Unsubscribe<CheatActivatedEvent>(OnCheatActivated);
        
        _highScoreManager.Cleanup();
    }
    
    private void OnViewHighScores(ViewHighScoresEvent evt)
    {
        _showHighScores = true;
    }
    
    private void OnNewHighScore(NewHighScoreEvent evt)
    {
        _newHighScoreAchieved = true;
        _newHighScoreRank = evt.Rank;
        
        // Update highest score if this is a new #1
        if (evt.Rank == 1)
        {
            _highestScore = evt.Score;
        }
    }
    
    private void OnGameRestart(GameRestartEvent evt)
    {
        _showHighScores = false;
        _newHighScoreAchieved = false;
    }
    
    private void OnScoreChanged(ScoreChangedEvent evt)
    {
        // Check if current score exceeds the high score
        if (evt.NewScore > _highestScore)
        {
            _highestScore = evt.NewScore;
        }
    }

    private void OnMusicToggle(MusicToggleEvent evt)
    {
        _showMusicToggleNotification = true;
        _musicToggleNotificationTimer = 0f;
        _musicEnabled = evt.Enabled;
    }

    private void OnSoundVolumeChanged(SoundVolumeChangedEvent evt)
    {
        _showSoundVolumeNotification = true;
        _soundVolumeNotificationTimer = 0f;
        _currentSoundVolume = evt.Volume;
    }

    private void OnBonusRoundStarted(BonusRoundStartedEvent evt)
    {
        _showBonusRoundMessage = true;
        _bonusMessageTimer = 0f;
    }

    private void OnCheatActivated(CheatActivatedEvent evt)
    {
        _showCheatActivatedMessage = true;
        _cheatMessageTimer = 0f;
        _cheatName = evt.CheatName;
    }

    public override void Update(float deltaTime)
    {
        // Update music toggle notification timer
        if (_showMusicToggleNotification)
        {
            _musicToggleNotificationTimer += deltaTime;
            if (_musicToggleNotificationTimer >= MusicToggleNotificationDuration)
            {
                _showMusicToggleNotification = false;
            }
        }
        
        // Update sound volume notification timer
        if (_showSoundVolumeNotification)
        {
            _soundVolumeNotificationTimer += deltaTime;
            if (_soundVolumeNotificationTimer >= SoundVolumeNotificationDuration)
            {
                _showSoundVolumeNotification = false;
            }
        }
        
        // Update bonus round message timer
        if (_showBonusRoundMessage)
        {
            _bonusMessageTimer += deltaTime;
            if (_bonusMessageTimer >= BonusMessageDuration)
            {
                _showBonusRoundMessage = false;
            }
        }
        
        // Update cheat activation message timer
        if (_showCheatActivatedMessage)
        {
            _cheatMessageTimer += deltaTime;
            if (_cheatMessageTimer >= CheatMessageDuration)
            {
                _showCheatActivatedMessage = false;
            }
        }
        
        // Check for back button in high scores view
        if (_showHighScores && Raylib.IsKeyPressed(KeyboardKey.Backspace))
        {
            _showHighScores = false;
            
            // If we're in the main menu, make sure we stay there
            if (gameState.CurrentState == GameState.State.MainMenu)
            {
                gameState.SetMainMenu();
            }
        }
    }

    public void DrawUI()
    {
        // If showing high scores, draw them and return
        if (_showHighScores)
        {
            DrawHighScores();
            return;
        }
        
        // Draw score with high score
        string scoreText = $"Score: {gameState.Score}";
        Raylib.DrawText(scoreText, 10, 10, 20, Color.White);
        
        // Show high score next to current score
        string highScoreText = $"High: {_highestScore}";
        int highScoreX = 10 + Raylib.MeasureText(scoreText, 20) + 20; // 20px spacing
        Color highScoreColor = gameState.Score > 0 && gameState.Score >= _highestScore ? Color.Gold : Color.Gray;
        Raylib.DrawText(highScoreText, highScoreX, 10, 20, highScoreColor);
        
        // Draw lives
        Raylib.DrawText($"Lives: {gameState.Lives}", gameState.ScreenWidth - 100, 10, 20, Color.White);
        
        // Draw level information
        var levelText = $"Level {gameState.CurrentLevel}/{gameState.MaxLevels}";
        Raylib.DrawText(levelText, gameState.ScreenWidth / 2 - 50, 10, 20, Color.White);
        
        // Draw state-specific messages
        switch (gameState.CurrentState)
        {
            case GameState.State.BallLost:
                Raylib.DrawText("Press SPACE to launch", gameState.ScreenWidth/2 - 120, gameState.ScreenHeight/2, 20, Color.White);
                break;
                
            case GameState.State.GameOver:
                if (_showHighScores)
                {
                    DrawHighScores();
                }
                else
                {
                    Raylib.DrawText("GAME OVER", gameState.ScreenWidth/2 - 100, gameState.ScreenHeight/2 - 50, 40, Color.White);
                    
                    if (_newHighScoreAchieved)
                    {
                        Raylib.DrawText($"NEW HIGH SCORE! Rank #{_newHighScoreRank}", 
                            gameState.ScreenWidth/2 - 150, gameState.ScreenHeight/2, 25, Color.Yellow);
                    }
                    
                    Raylib.DrawText("Press H to view high scores", gameState.ScreenWidth/2 - 120, gameState.ScreenHeight/2 + 40, 18, Color.White);
                    Raylib.DrawText("Press ENTER to restart", gameState.ScreenWidth/2 - 120, gameState.ScreenHeight/2 + 70, 20, Color.White);
                    
                    // Check for H key to toggle high scores
                    if (Raylib.IsKeyPressed(KeyboardKey.H))
                    {
                        _showHighScores = true;
                    }
                }
                break;
                
            case GameState.State.GameWon:
                if (gameState.CurrentLevel < gameState.MaxLevels)
                {
                    Raylib.DrawText("LEVEL COMPLETE!", gameState.ScreenWidth/2 - 120, gameState.ScreenHeight/2 - 20, 40, Color.Gold);
                    Raylib.DrawText("Advancing to next level...", gameState.ScreenWidth/2 - 140, gameState.ScreenHeight/2 + 30, 20, Color.White);
                    Raylib.DrawText("Bonus: +1 Life!", gameState.ScreenWidth/2 - 80, gameState.ScreenHeight/2 + 60, 20, Color.Green);
                }
                else
                {
                    if (_showHighScores)
                    {
                        DrawHighScores();
                    }
                    else
                    {
                        Raylib.DrawText("CONGRATULATIONS!", gameState.ScreenWidth/2 - 140, gameState.ScreenHeight/2 - 50, 40, Color.Gold);
                        Raylib.DrawText("You completed all levels!", gameState.ScreenWidth/2 - 140, gameState.ScreenHeight/2, 20, Color.White);
                        
                        if (_newHighScoreAchieved)
                        {
                            Raylib.DrawText($"NEW HIGH SCORE! Rank #{_newHighScoreRank}", 
                                gameState.ScreenWidth/2 - 150, gameState.ScreenHeight/2 + 30, 25, Color.Yellow);
                        }
                        
                        Raylib.DrawText("Press H to view high scores", gameState.ScreenWidth/2 - 120, gameState.ScreenHeight/2 + 60, 18, Color.White);
                        Raylib.DrawText("Press ENTER to play again", gameState.ScreenWidth/2 - 140, gameState.ScreenHeight/2 + 90, 20, Color.White);
                        
                        // Check for H key to toggle high scores
                        if (Raylib.IsKeyPressed(KeyboardKey.H))
                        {
                            _showHighScores = true;
                        }
                    }
                }
                break;
        }

        // Draw bonus round specific UI if needed
        if (gameState.InBonusRound)
        {
            // Add a "BONUS ROUND" indicator
            string bonusText = "BONUS ROUND";
            Raylib.DrawText(bonusText, 
                gameState.ScreenWidth - Raylib.MeasureText(bonusText, 20) - 10, 
                40, 20, Color.Gold);
                
            // Show "Double Points!" text
            Raylib.DrawText("Double Points!", 
                gameState.ScreenWidth - Raylib.MeasureText("Double Points!", 16) - 10, 
                65, 16, Color.Orange);
        }

        // Show bonus round notification if needed
        if (_showBonusRoundMessage)
        {
            float alpha = 1.0f;
            if (_bonusMessageTimer > BonusMessageDuration - 1.0f)
            {
                alpha = 1.0f - (_bonusMessageTimer - (BonusMessageDuration - 1.0f));
            }
            
            Color messageColor = Color.Gold;
            messageColor.A = (byte)(255 * alpha);
            
            Raylib.DrawRectangle(
                gameState.ScreenWidth / 2 - 200,
                gameState.ScreenHeight / 3 - 60,
                400, 120,
                new Color((byte)0, (byte)0, (byte)0, (byte)(180 * alpha))
            );
            
            string bonusTitle = "BONUS ROUND!";
            string bonusDesc = "Double points for all bricks!";
            string bonusDesc2 = "Clear all bricks for a big score bonus!";
            
            Raylib.DrawText(bonusTitle, 
                gameState.ScreenWidth / 2 - Raylib.MeasureText(bonusTitle, 30) / 2, 
                gameState.ScreenHeight / 3 - 45, 
                30, messageColor);
                
            Raylib.DrawText(bonusDesc, 
                gameState.ScreenWidth / 2 - Raylib.MeasureText(bonusDesc, 20) / 2, 
                gameState.ScreenHeight / 3, 
                20, messageColor);
                
            Raylib.DrawText(bonusDesc2, 
                gameState.ScreenWidth / 2 - Raylib.MeasureText(bonusDesc2, 18) / 2, 
                gameState.ScreenHeight / 3 + 30, 
                18, messageColor);
        }

        // Draw music toggle notification if active
        if (_showMusicToggleNotification)
        {
            string message = _musicEnabled ? "Music: ON" : "Music: OFF";
            int fontSize = 24;
            int messageWidth = Raylib.MeasureText(message, fontSize);
            
            // Calculate alpha for fade out effect
            float alpha = 1f - (_musicToggleNotificationTimer / MusicToggleNotificationDuration);
            Color textColor = Color.White;
            textColor.A = (byte)(255 * alpha);
            
            // Draw notification at bottom center of screen
            Raylib.DrawText(
                message, 
                gameState.ScreenWidth / 2 - messageWidth / 2, 
                gameState.ScreenHeight - 80, 
                fontSize, 
                textColor
            );
        }
        
        // Display music status in top right corner
        string musicStatus = _musicEnabled ? "Music: ON (S)" : "Music: OFF (S)";
        Raylib.DrawText(musicStatus, gameState.ScreenWidth - 120, 40, 16, Color.Gray);
        
        // Display sound volume notification if active
        if (_showSoundVolumeNotification)
        {
            string volumeText = $"Sound Volume: {(int)(_currentSoundVolume * 100)}%";
            int volumeWidth = Raylib.MeasureText(volumeText, 20);
            Raylib.DrawText(volumeText, 
                gameState.ScreenWidth / 2 - volumeWidth / 2, 
                gameState.ScreenHeight - 40, 
                20, 
                Color.White);
            
            // Draw volume bar
            int barWidth = 200;
            int barHeight = 10;
            int barX = gameState.ScreenWidth / 2 - barWidth / 2;
            int barY = gameState.ScreenHeight - 20;
            
            // Draw background bar
            Raylib.DrawRectangle(barX, barY, barWidth, barHeight, Color.DarkGray);
            // Draw filled portion
            Raylib.DrawRectangle(barX, barY, (int)(barWidth * _currentSoundVolume), barHeight, Color.Green);
        }

        // Draw cheat activation message if active
        if (_showCheatActivatedMessage)
        {
            float alpha = 1.0f;
            if (_cheatMessageTimer > CheatMessageDuration - 1.0f)
            {
                alpha = 1.0f - (_cheatMessageTimer - (CheatMessageDuration - 1.0f));
            }
            
            Color messageColor = Color.Purple;
            messageColor.A = (byte)(255 * alpha);
            
            Raylib.DrawRectangle(
                gameState.ScreenWidth / 2 - 150,
                gameState.ScreenHeight / 4 - 40,
                300, 80,
                new Color((byte)0, (byte)0, (byte)0, (byte)(180 * alpha))
            );
            
            string cheatTitle = "CHEAT ACTIVATED!";
            string cheatDesc = _cheatName;
            
            Raylib.DrawText(cheatTitle, 
                gameState.ScreenWidth / 2 - Raylib.MeasureText(cheatTitle, 24) / 2, 
                gameState.ScreenHeight / 4 - 30, 
                24, messageColor);
                
            Raylib.DrawText(cheatDesc, 
                gameState.ScreenWidth / 2 - Raylib.MeasureText(cheatDesc, 20) / 2, 
                gameState.ScreenHeight / 4, 
                20, messageColor);
        }
    }
    
    private void DrawHighScores()
    {
        var highScores = _highScoreManager.GetHighScores();
        
        Raylib.DrawRectangle(
            gameState.ScreenWidth/2 - 200, 
            gameState.ScreenHeight/2 - 180, 
            400, 
            360, 
            new Color(0, 0, 0, 200)
        );
        
        Raylib.DrawText("HIGH SCORES", gameState.ScreenWidth/2 - 100, gameState.ScreenHeight/2 - 160, 30, Color.Gold);
        
        int yPos = gameState.ScreenHeight/2 - 110;
        
        // Draw header
        Raylib.DrawText("Rank", gameState.ScreenWidth/2 - 180, yPos, 18, Color.White);
        Raylib.DrawText("Player", gameState.ScreenWidth/2 - 120, yPos, 18, Color.White);
        Raylib.DrawText("Score", gameState.ScreenWidth/2 + 40, yPos, 18, Color.White);
        Raylib.DrawText("Level", gameState.ScreenWidth/2 + 120, yPos, 18, Color.White);
        
        yPos += 30;
        
        // Draw scores
        for (int i = 0; i < highScores.Count; i++)
        {
            var entry = highScores[i];
            Color rowColor = _newHighScoreAchieved && i + 1 == _newHighScoreRank ? Color.Yellow : Color.White;
            
            Raylib.DrawText($"#{i+1}", gameState.ScreenWidth/2 - 180, yPos, 18, rowColor);
            Raylib.DrawText(entry.PlayerName, gameState.ScreenWidth/2 - 120, yPos, 18, rowColor);
            Raylib.DrawText(entry.Score.ToString(), gameState.ScreenWidth/2 + 40, yPos, 18, rowColor);
            Raylib.DrawText(entry.Level.ToString(), gameState.ScreenWidth/2 + 120, yPos, 18, rowColor);
            
            yPos += 30;
        }
        
        // Draw empty slots
        for (int i = highScores.Count; i < 5; i++)
        {
            Raylib.DrawText($"#{i+1}", gameState.ScreenWidth/2 - 180, yPos, 18, Color.Gray);
            Raylib.DrawText("---", gameState.ScreenWidth/2 - 120, yPos, 18, Color.Gray);
            Raylib.DrawText("---", gameState.ScreenWidth/2 + 40, yPos, 18, Color.Gray);
            Raylib.DrawText("---", gameState.ScreenWidth/2 + 120, yPos, 18, Color.Gray);
            
            yPos += 30;
        }
        
        Raylib.DrawText("Press BACK to return", gameState.ScreenWidth/2 - 100, yPos + 30, 18, Color.White);
        
        // Handle back button
        if (Raylib.IsKeyPressed(KeyboardKey.Backspace))
        {
            _showHighScores = false;
        }
    }

    // Updated to use GameState instead of PowerUpManager
    public void DrawPowerUpInfo()
    {
        int y = 35; // Position below score and lives
        
        // Positive power-ups
        
        // Display paddle grow power-up if active
        if (gameState.IsPowerUpActive(PowerUp.Type.PaddleGrow))
        {
            float remaining = gameState.GetRemainingTime(PowerUp.Type.PaddleGrow);
            Raylib.DrawText($"Paddle Grow: {remaining:0.0}s", 10, y, 16, Color.Green);
            y += 20;
        }
        
        // Display gun power-up if active
        if (gameState.IsPowerUpActive(PowerUp.Type.Gun))
        {
            float remaining = gameState.GetRemainingTime(PowerUp.Type.Gun);
            Raylib.DrawText($"Gun: {remaining:0.0}s | Bullets: {gameState.Bullets.Count}/{gameState.MaxBullets}", 10, y, 16, Color.Red);
            y += 20;
        }
        
        // Display extra ball count if any
        var extraBallCount = gameState.ExtraBalls.Count;
        if (extraBallCount > 0)
        {
            Raylib.DrawText($"Extra Balls: {extraBallCount}", 10, y, 16, Color.Blue);
            y += 20;
        }
        
        // Negative power-ups (power-downs)
        
        // Display paddle shrink power-down if active
        if (gameState.IsPowerUpActive(PowerUp.Type.PaddleShrink))
        {
            float remaining = gameState.GetRemainingTime(PowerUp.Type.PaddleShrink);
            Raylib.DrawText($"Paddle Shrink: {remaining:0.0}s", 10, y, 16, Color.Magenta);
            y += 20;
        }
        
        // Display speed up power-down if active
        if (gameState.IsPowerUpActive(PowerUp.Type.SpeedUp))
        {
            float remaining = gameState.GetRemainingTime(PowerUp.Type.SpeedUp);
            Raylib.DrawText($"Speed Up: {remaining:0.0}s", 10, y, 16, Color.Orange);
            y += 20;
        }
        
        // Display reverse controls power-down if active
        if (gameState.IsPowerUpActive(PowerUp.Type.ReverseControls))
        {
            float remaining = gameState.GetRemainingTime(PowerUp.Type.ReverseControls);
            Raylib.DrawText($"Reverse Controls: {remaining:0.0}s", 10, y, 16, Color.Purple);
            y += 20;
        }
    }
}
