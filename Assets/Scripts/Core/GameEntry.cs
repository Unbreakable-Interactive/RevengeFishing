using UnityEngine;
using UnityEngine.SceneManagement;

public class GameEntry : MonoBehaviour
{
    public static GameEntry Instance { get; private set; }
    
    [Header("Scene Management")]
    public string gameSceneName = "SampleScene";
    public string menuSceneName = "MainMenu";
    
    [Header("Global Settings")]
    public bool enableDebugLogs = true;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeGlobalSystems();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void InitializeGlobalSystems()
    {
        if (enableDebugLogs)
            GameLogger.Log("GameEntry: Global systems initialized");
    }
    
    public void LoadGameScene()
    {
        if (enableDebugLogs)
            GameLogger.Log("GameEntry: Loading game scene");
        SceneManager.LoadScene(gameSceneName);
    }
    
    public void LoadMenuScene()
    {
        if (enableDebugLogs)
            GameLogger.Log("GameEntry: Loading menu scene");
        SceneManager.LoadScene(menuSceneName);
    }
    
    public void QuitGame()
    {
        if (enableDebugLogs)
            GameLogger.Log("GameEntry: Quitting game");
        Application.Quit();
    }
}