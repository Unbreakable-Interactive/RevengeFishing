using UnityEngine;

public enum GameState
{
    Load,
    SetupGame,
    Gameplay,
    Pause,
    Upgrade
}

public class GameStates : MonoBehaviour
{
    public static GameStates instance;
    
    [SerializeField] private GameState currentGameState = GameState.Load;

    private GameState previousGameState = GameState.Load;
    
    public void Initialize()
    {
        if(instance == null)
            instance = this;
        else
            Destroy(this.gameObject);
    }


    public GameState GetGameState() => currentGameState;

    public bool IsGameplayRunning() => currentGameState == GameState.Gameplay;
    
    public bool IsGamePaused() => currentGameState == GameState.Pause;
    
    public void SetGameState(GameState newGameState) => currentGameState = newGameState;

    public void SetGameState_Load() => SetGameState(GameState.Load);

    public void SetGameState_SetupGame()=>SetGameState(GameState.SetupGame);
    public void SetGameState_Gameplay() => SetGameState(GameState.Gameplay);

    public void SetGameState_Pause()
    {
        SaveOldGameState();
        SetGameState(GameState.Pause);
    }

    public void SetGameState_Upgrade()
    {
        SaveOldGameState();
        SetGameState(GameState.Upgrade);
    }

    private void SaveOldGameState() => previousGameState = currentGameState;

    public void GoBackToOldGameState() => SetGameState(previousGameState);
    
}
