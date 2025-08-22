using System.Collections;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    public GameStates gameStates;

    private void Awake()
    {
        if(instance == null)
            instance = this;
        else
            Destroy(this.gameObject);
    }

    public void Start()
    {
        StartCoroutine(StartGame());
    }

    IEnumerator StartGame()
    {
        // Initialize GameStates
        gameStates.Initialize();
        
        //Load
        yield return StartCoroutine(SetupOnLoad());

        //Setup
        yield return StartCoroutine(SetupOnSetupGame());
        
        //Play
        yield return StartCoroutine(SetupOnPlay());
    }


    private IEnumerator SetupOnLoad()
    {
        GameStates.instance.SetGameState_Load();
        
        yield return null;
    }
    
    private IEnumerator SetupOnSetupGame()
    {
        GameStates.instance.SetGameState_SetupGame();
        
        yield return null;
    }
    
    private IEnumerator SetupOnPlay()
    {
        GameStates.instance.SetGameState_Gameplay();
        
        yield return null;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (GameStates.instance.IsGameplayRunning())
            {
                PauseGame();
            }
            else if (GameStates.instance.IsGamePaused())
            {
                ResumeGame();
            }
        }
    }

    private void PauseGame()
    {
        GameStates.instance.SetGameState_Pause();
        Time.timeScale = 0;
    }
    
    private void ResumeGame()
    {
        GameStates.instance.GoBackToOldGameState();
        Time.timeScale = 1;
    }
    
}
