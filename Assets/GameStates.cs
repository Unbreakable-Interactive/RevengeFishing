using System;
using UnityEngine;

public enum GameState { Load, SetupGame, Gameplay, Pause, Upgrade }

public class GameStates : MonoBehaviour
{
    public static GameStates instance;

    [SerializeField] private GameState currentGameState = GameState.Load;
    private GameState previousGameState = GameState.Load;

    public event Action<GameState, GameState> OnStateChanged; // (old,new)

    public void Initialize()
    {
        if (instance == null) instance = this;
        else { Destroy(gameObject); return; }
    }

    public GameState GetGameState() => currentGameState;
    public bool IsGameplayRunning() => currentGameState == GameState.Gameplay;
    public bool IsGamePaused() => currentGameState == GameState.Pause;

    public void SetGameState_Load()      => SetGameState(GameState.Load);
    public void SetGameState_SetupGame() => SetGameState(GameState.SetupGame);
    public void SetGameState_Gameplay()  => SetGameState(GameState.Gameplay);

    public void SetGameState_Pause()   { SaveOld(); SetGameState(GameState.Pause); }
    public void SetGameState_Upgrade() { SaveOld(); SetGameState(GameState.Upgrade); }

    public void GoBackToOldGameState() => SetGameState(previousGameState);

    private void SaveOld() => previousGameState = currentGameState;

    public void SetGameState(GameState newState)
    {
        if (currentGameState == newState) return;
        var old = currentGameState;
        currentGameState = newState;
        OnStateChanged?.Invoke(old, newState);
    }
}