namespace Breakout.Managers;

public abstract class ManagerBase(GameState gameState)
{
    protected readonly GameState gameState = gameState;

    public virtual void Initialize() { }

    public virtual void Update(float deltaTime) { }
    
    public virtual void Draw() { }

    public virtual void Cleanup() { }
}
