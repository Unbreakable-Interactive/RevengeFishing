using UnityEngine;

public class GameBootstrap : MonoBehaviour
{
    [Header("Core Game Systems")]
    [SerializeField] private PlayerBounds playerBounds;
    [SerializeField] private Camera gameCamera;

    [Header("Spawn Systems")]
    [SerializeField] private SpawnHandler spawnHandler;
    
    [Header("Debug")]
    [SerializeField] private bool enableBootstrapLogs = true;

    private void Start()
    {
        InitializeGameSystems();
    }
    
    private void InitializeGameSystems()
    {
        try
        {
            if (enableBootstrapLogs)
                GameLogger.Log("GameBootstrap: Starting initialization...");
            
            InitializeCoreSystemsPhase();
            InitializePlayerSystemsPhase();
            InitializeSpawnSystemsPhase();
            
            if (enableBootstrapLogs)
                GameLogger.Log("GameBootstrap: All systems ready!");
        }
        catch (System.Exception e)
        {
            GameLogger.LogError($"GameBootstrap failed: {e.Message}");
        }
    }
    
    private void InitializeCoreSystemsPhase()
    {
        if (gameCamera == null)
            gameCamera = Camera.main;
    }
    
    private void InitializePlayerSystemsPhase()
    {
        if (playerBounds != null) playerBounds.Initialize(Player.Instance);
    }

    private void InitializeSpawnSystemsPhase()
    {
        if (spawnHandler != null) spawnHandler.Initialize();
    }
}