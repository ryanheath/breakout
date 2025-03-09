namespace Breakout.Managers;

public class SoundManager(GameState gameState) : ManagerBase(gameState)
{
    private Dictionary<SoundType, Sound> _sounds = [];
    private Dictionary<MusicType, Music> _music = [];
    private float _currentPitch = 1.0f;
    private const float _pitchIncrement = 0.1f;
    private const float _maxPitch = 1.5f;
    private const float _defaultPitch = 1.0f;
    private MusicType _currentMusic = MusicType.None;
    private float _musicVolume = 0.5f;
    private bool _musicEnabled = true;
    private const float _normalMusicPitch = 1.0f;
    private const float _speedupMusicPitch = 1.25f; // 25% faster when speed power-up is active

    private enum SoundType
    {
        PaddleHit,
        BrickHit,
        WallHit,
        LifeLost,
        LevelUp,
        GameOver,
        GameStart,
        PowerUp,
        GunShot,
        Explosion,  // New sound type for explosive bricks
        MenuNavigation, // New sound type for menu navigation
        MenuSelection  // New sound type for menu selection
    }
    
    private enum MusicType
    {
        None,
        MainMenu,
        Gameplay,
        GameOver
    }

    public override void Initialize()
    {
        Raylib.InitAudioDevice();

        _sounds = new Dictionary<SoundType, Sound>
        {
            { SoundType.PaddleHit, Raylib.LoadSound(@"resources\186398__lloydevans09__balsa-hit-4.wav") },
            { SoundType.BrickHit, Raylib.LoadSound(@"resources\186399__lloydevans09__balsa-hit-3.wav") },
            { SoundType.WallHit, Raylib.LoadSound(@"resources\4359__noisecollector__pongblipf4.wav") },
            { SoundType.LifeLost, Raylib.LoadSound(@"resources\159408__noirenex__life-lost-game-over.wav") },
            { SoundType.LevelUp, Raylib.LoadSound(@"resources\532708__pooffles__item.wav") },
            { SoundType.GameOver, Raylib.LoadSound(@"resources\173859__jivatma07__j1game_over_mono.wav") },
            { SoundType.GameStart, Raylib.LoadSound(@"resources\758957__ksaplay__8-bit-start-game.wav") },
            { SoundType.PowerUp, Raylib.LoadSound(@"resources\503459__matrixxx__powerup-02.wav") },
            { SoundType.GunShot, Raylib.LoadSound(@"resources\417601__amrboghdady__laser-gun-shot.wav") },
            { SoundType.Explosion, Raylib.LoadSound(@"resources\506826__mrthenoronha__explosion-1-8-bit.wav") },
            { SoundType.MenuNavigation, Raylib.LoadSound(@"resources\menu_navigation.wav") },
            { SoundType.MenuSelection, Raylib.LoadSound(@"resources\menu_selection.wav") }
        };
        
        // Load background music
        _music = new Dictionary<MusicType, Music>
        {
            { MusicType.MainMenu, Raylib.LoadMusicStream(@"resources\455109__slaking_97__free-music-background-loop-001.wav") },
            { MusicType.Gameplay, Raylib.LoadMusicStream(@"resources\586098__slaking_97__free-music-background-loop-003-var-05.wav") },
            { MusicType.GameOver, Raylib.LoadMusicStream(@"resources\586097__slaking_97__free-music-background-loop-003-var-04.wav") }
        };
        
        // Set music volume
        foreach (var music in _music.Values)
        {
            Raylib.SetMusicVolume(music, _musicVolume);
        }

        // Subscribe to events
        EventBus.Subscribe<BrickHitEvent>(OnBrickHit);
        EventBus.Subscribe<WallCollisionEvent>(OnWallCollision);
        EventBus.Subscribe<PaddleCollisionEvent>(OnPaddleCollision);
        EventBus.Subscribe<PowerUpCollectedEvent>(OnPowerUpCollected);
        EventBus.Subscribe<BallLostEvent>(OnBallLost);
        EventBus.Subscribe<GameOverEvent>(OnGameOver);
        EventBus.Subscribe<LevelCompletedEvent>(OnLevelCompleted);
        EventBus.Subscribe<GameStartedEvent>(OnGameStarted);
        EventBus.Subscribe<GunShotEvent>(OnGunShot);
        EventBus.Subscribe<ExplosiveBrickDetonatedEvent>(OnExplosiveBrickDetonated);
        EventBus.Subscribe<GameStateChangedEvent>(OnGameStateChanged);
        EventBus.Subscribe<GamePausedEvent>(OnGamePaused);
        EventBus.Subscribe<GameResumedEvent>(OnGameResumed);
        EventBus.Subscribe<PowerUpActivatedEvent>(OnPowerUpActivated);
        EventBus.Subscribe<PowerUpExpiredEvent>(OnPowerUpExpired);
        
        // Start main menu music if we're in the main menu
        if (gameState.CurrentState == GameState.State.MainMenu)
        {
            PlayMusic(MusicType.MainMenu);
        }
    }

    public override void Cleanup()
    {
        base.Cleanup();

        EventBus.Unsubscribe<BrickHitEvent>(OnBrickHit);
        EventBus.Unsubscribe<WallCollisionEvent>(OnWallCollision);
        EventBus.Unsubscribe<PaddleCollisionEvent>(OnPaddleCollision);
        EventBus.Unsubscribe<PowerUpCollectedEvent>(OnPowerUpCollected);
        EventBus.Unsubscribe<BallLostEvent>(OnBallLost);
        EventBus.Unsubscribe<GameOverEvent>(OnGameOver);
        EventBus.Unsubscribe<LevelCompletedEvent>(OnLevelCompleted);
        EventBus.Unsubscribe<GameStartedEvent>(OnGameStarted);
        EventBus.Unsubscribe<GunShotEvent>(OnGunShot); // Unsubscribe from gun shot events
        EventBus.Unsubscribe<ExplosiveBrickDetonatedEvent>(OnExplosiveBrickDetonated);
        EventBus.Unsubscribe<GameStateChangedEvent>(OnGameStateChanged);
        EventBus.Unsubscribe<GamePausedEvent>(OnGamePaused);
        EventBus.Unsubscribe<GameResumedEvent>(OnGameResumed);
        EventBus.Unsubscribe<PowerUpActivatedEvent>(OnPowerUpActivated);
        EventBus.Unsubscribe<PowerUpExpiredEvent>(OnPowerUpExpired);

        // Unload sounds
        foreach (var sound in _sounds.Values)
        {
            Raylib.UnloadSound(sound);
        }
        _sounds.Clear();
        
        // Stop and unload all music
        StopMusic();
        foreach (var music in _music.Values)
        {
            Raylib.UnloadMusicStream(music);
        }
        _music.Clear();

        Raylib.CloseAudioDevice();
    }
    
    private void OnGameStateChanged(GameStateChangedEvent evt)
    {
        // Switch music based on game state
        switch (evt.NewState)
        {
            case GameState.State.MainMenu:
                PlayMusic(MusicType.MainMenu);
                break;
                
            case GameState.State.Playing:
            case GameState.State.BallLost:
                PlayMusic(MusicType.Gameplay);
                break;
                
            case GameState.State.GameOver:
            case GameState.State.GameWon:
                PlayMusic(MusicType.GameOver);
                break;
                
            case GameState.State.PauseMenu:
                // Keep current music but reduce volume
                if (_currentMusic != MusicType.None)
                {
                    Raylib.SetMusicVolume(_music[_currentMusic], _musicVolume * 0.5f);
                }
                break;
        }
        
        // If returning from pause menu, restore normal volume
        if (evt.OldState == GameState.State.PauseMenu && _currentMusic != MusicType.None)
        {
            Raylib.SetMusicVolume(_music[_currentMusic], _musicVolume);
        }
    }
    
    public override void Update(float deltaTime)
    {
        // Check for S key to toggle music on/off
        if (Raylib.IsKeyPressed(KeyboardKey.S))
        {
            ToggleMusic();
        }
        
        // Update music streams if music is enabled
        if (_musicEnabled && _currentMusic != MusicType.None)
        {
            Raylib.UpdateMusicStream(_music[_currentMusic]);
            
            // Loop music if it ends
            if (!Raylib.IsMusicStreamPlaying(_music[_currentMusic]))
            {
                Raylib.PlayMusicStream(_music[_currentMusic]);
            }
        }
    }
    
    public void ToggleMusic()
    {
        _musicEnabled = !_musicEnabled;
        
        if (_currentMusic != MusicType.None)
        {
            if (_musicEnabled)
            {
                Raylib.ResumeMusicStream(_music[_currentMusic]);
                Raylib.SetMusicVolume(_music[_currentMusic], _musicVolume);
            }
            else
            {
                Raylib.PauseMusicStream(_music[_currentMusic]);
            }
        }
        
        // Show a notification that music was toggled
        EventBus.Publish(new MusicToggleEvent(_musicEnabled));
    }
    
    private void PlayMusic(MusicType type)
    {
        // Don't do anything if we're already playing this music
        if (_currentMusic == type)
            return;
            
        // Stop current music if any
        StopMusic();
        
        // Start new music if it's not None and music is enabled
        if (type != MusicType.None)
        {
            _currentMusic = type;
            Raylib.PlayMusicStream(_music[_currentMusic]);
            
            // Only set volume > 0 if music is enabled
            Raylib.SetMusicVolume(_music[_currentMusic], _musicEnabled ? _musicVolume : 0);
            
            // If speed power-up is active, apply the speed-up to the new music too
            if (gameState.BallSpeedMultiplier > 1.0f)
            {
                Raylib.SetMusicPitch(_music[_currentMusic], _speedupMusicPitch);
            }
            else
            {
                Raylib.SetMusicPitch(_music[_currentMusic], _normalMusicPitch);
            }
            
            // If music is disabled, pause the stream
            if (!_musicEnabled)
            {
                Raylib.PauseMusicStream(_music[_currentMusic]);
            }
        }
    }
    
    private void StopMusic()
    {
        if (_currentMusic != MusicType.None)
        {
            Raylib.StopMusicStream(_music[_currentMusic]);
            _currentMusic = MusicType.None;
        }
    }
    
    public void SetMusicVolume(float volume)
    {
        _musicVolume = Math.Clamp(volume, 0f, 1f);
        
        if (_currentMusic != MusicType.None)
        {
            Raylib.SetMusicVolume(_music[_currentMusic], _musicVolume);
        }
    }

    private void OnBrickHit(BrickHitEvent _) => PlaySound(SoundType.BrickHit);
    private void OnWallCollision(WallCollisionEvent _) => PlaySound(SoundType.WallHit);
    private void OnPaddleCollision(PaddleCollisionEvent _) => PlaySound(SoundType.PaddleHit);
    private void OnPowerUpCollected(PowerUpCollectedEvent _) => PlaySound(SoundType.PowerUp);
    private void OnBallLost(BallLostEvent _) => PlaySound(SoundType.LifeLost);
    private void OnGameOver(GameOverEvent _) => PlaySound(SoundType.GameOver);
    private void OnLevelCompleted(LevelCompletedEvent _) => PlaySound(SoundType.LevelUp);
    private void OnGameStarted(GameStartedEvent _) => PlaySound(SoundType.GameStart);
    private void OnGunShot(GunShotEvent _) => PlaySound(SoundType.GunShot);

    private void OnExplosiveBrickDetonated(ExplosiveBrickDetonatedEvent evt)
    {
        // Play explosion sound with higher volume and lower pitch for a more impactful effect
        var sound = _sounds[SoundType.Explosion];
        Raylib.SetSoundVolume(sound, 0.8f);
        Raylib.SetSoundPitch(sound, 0.7f);  // Lower pitch makes it sound more powerful
        Raylib.PlaySound(sound);
        
        // Reset to default after playing
        Raylib.SetSoundPitch(sound, _defaultPitch);
        Raylib.SetSoundVolume(sound, 1.0f);
    }

    private void OnGamePaused(GamePausedEvent _)
    {
        // Play a pause sound effect
        PlaySound(SoundType.MenuNavigation);
        
        // Reduce music volume when paused
        if (_currentMusic != MusicType.None)
        {
            Raylib.SetMusicVolume(_music[_currentMusic], _musicVolume * 0.3f);
        }
    }
    
    private void OnGameResumed(GameResumedEvent _)
    {
        // Play a resume sound effect
        PlaySound(SoundType.MenuSelection);
        
        // Restore original music volume
        if (_currentMusic != MusicType.None)
        {
            Raylib.SetMusicVolume(_music[_currentMusic], _musicVolume);
        }
    }

    private void OnPowerUpActivated(PowerUpActivatedEvent evt)
    {
        if (evt.PowerUpType == PowerUp.Type.SpeedUp && _currentMusic != MusicType.None)
        {
            // Speed up the music when speed power-up is activated
            Raylib.SetMusicPitch(_music[_currentMusic], _speedupMusicPitch);
        }
    }
    
    private void OnPowerUpExpired(PowerUpExpiredEvent evt)
    {
        if (evt.PowerUpType == PowerUp.Type.SpeedUp && _currentMusic != MusicType.None)
        {
            // Reset music speed when power-up expires
            Raylib.SetMusicPitch(_music[_currentMusic], _normalMusicPitch);
        }
    }

    private void PlaySound(SoundType soundType)
    {
        var sound = _sounds[soundType];

        // If it's a brick hit, increase the pitch for combo effect
        if (soundType == SoundType.BrickHit)
        {
            _currentPitch = MathF.Min(_currentPitch + _pitchIncrement, _maxPitch);
        }
        else
        {
            _currentPitch = _defaultPitch;
        }

        // Set pitch for sounds that should use it
        if (soundType == SoundType.BrickHit)
        {
            Raylib.SetSoundPitch(sound, _currentPitch);
        }
        else
        {
            // Use default pitch for other sounds
            Raylib.SetSoundPitch(sound, _defaultPitch);
        }

        Raylib.PlaySound(sound);
    }
}
