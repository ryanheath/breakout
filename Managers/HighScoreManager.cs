namespace Breakout.Managers;

public class HighScoreManager(GameState gameState) : ManagerBase(gameState)
{
    private const string HighScoreFilePath = "highscores.json";
    private const int MaxHighScores = 5;
    private List<HighScoreEntry> _highScores = [];
    
    public class HighScoreEntry
    {
        public string PlayerName { get; set; } = "Player";
        public int Score { get; set; }
        public int Level { get; set; }
        public DateTime Date { get; set; }
        
        public HighScoreEntry(string playerName, int score, int level)
        {
            PlayerName = playerName ?? "Player";
            Score = score;
            Level = level;
            Date = DateTime.Now;
        }
        
        // Needed for JSON deserialization
        public HighScoreEntry() { }
    }
    
    public override void Initialize()
    {
        EventBus.Subscribe<GameOverEvent>(OnGameOver);
        EventBus.Subscribe<AllLevelsCompletedEvent>(OnAllLevelsCompleted);
        
        LoadHighScores();
    }
    
    public override void Cleanup()
    {
        EventBus.Unsubscribe<GameOverEvent>(OnGameOver);
        EventBus.Unsubscribe<AllLevelsCompletedEvent>(OnAllLevelsCompleted);
    }
    
    private void OnGameOver(GameOverEvent evt)
    {
        CheckAndSaveHighScore(evt.FinalScore, gameState.CurrentLevel);
    }
    
    private void OnAllLevelsCompleted(AllLevelsCompletedEvent evt)
    {
        CheckAndSaveHighScore(evt.FinalScore, gameState.MaxLevels);
    }
    
    private void CheckAndSaveHighScore(int score, int level)
    {
        // Check if this score qualifies for the high score list
        if (_highScores.Count < MaxHighScores || score > _highScores.Min(hs => hs.Score))
        {
            // Simple player name for now
            string playerName = "Player";
            
            // Add the new high score
            _highScores.Add(new HighScoreEntry(playerName, score, level));
            
            // Sort in descending order by score
            _highScores = _highScores.OrderByDescending(hs => hs.Score).ToList();
            
            // Keep only the top MaxHighScores
            if (_highScores.Count > MaxHighScores)
            {
                _highScores = _highScores.Take(MaxHighScores).ToList();
            }
            
            // Save the updated high scores
            SaveHighScores();
            
            // Publish event to notify that a new high score was achieved
            EventBus.Publish(new NewHighScoreEvent(score, _highScores.FindIndex(hs => hs.Score == score) + 1));
        }
    }
    
    private void LoadHighScores()
    {
        try
        {
            if (File.Exists(HighScoreFilePath))
            {
                string json = File.ReadAllText(HighScoreFilePath);
                _highScores = System.Text.Json.JsonSerializer.Deserialize<List<HighScoreEntry>>(json) ?? [];
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading high scores: {ex.Message}");
            _highScores = [];
        }
    }
    
    private void SaveHighScores()
    {
        try
        {
            string json = System.Text.Json.JsonSerializer.Serialize(_highScores);
            File.WriteAllText(HighScoreFilePath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving high scores: {ex.Message}");
        }
    }
    
    public List<HighScoreEntry> GetHighScores() => _highScores.ToList();
    
    public bool IsHighScore(int score)
    {
        return _highScores.Count < MaxHighScores || score > _highScores.Min(hs => hs.Score);
    }
    
    public int GetHighScoreRank(int score)
    {
        return _highScores.Count(hs => hs.Score > score) + 1;
    }
}
