using UnityEngine;

public class GameBootstrap : MonoBehaviour
{
    [Header("Core Game Systems")]
    [SerializeField] private Player playerMovement;
    [SerializeField] private PlayerBounds playerBounds;
    [SerializeField] private PlayerStats playerStats;
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
                Debug.Log("GameBootstrap: Starting initialization...");

            InitializeCoreSystemsPhase();
            InitializePlayerSystemsPhase();
            InitializeSpawnSystemsPhase();

            if (enableBootstrapLogs)
                Debug.Log("GameBootstrap: All systems ready!");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"GameBootstrap failed: {e.Message}");
        }
    }

    private void InitializeCoreSystemsPhase()
    {
        if (gameCamera == null)
            gameCamera = Camera.main;
    }

    private void InitializePlayerSystemsPhase()
    {
        if (playerStats != null)
            playerStats.Initialize();

        if (playerMovement != null)
            playerMovement.Initialize();

        if (playerBounds != null && playerMovement != null)
            playerBounds.Initialize(playerMovement);
    }

    private void InitializeSpawnSystemsPhase()
    {
        if (spawnHandler != null)
            spawnHandler.Initialize();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R) && enableBootstrapLogs)
        {
            Debug.Log("GameBootstrap: Manual restart");
            InitializeGameSystems();
        }
    }
}