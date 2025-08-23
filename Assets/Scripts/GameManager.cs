using System.Collections;
using Aldha.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    #region Singleton
    public static GameManager instance;
    #endregion

    #region References
    public GameStates gameStates;

    [Header("UI Panel")]
    [SerializeField] private Button startButton;
    [SerializeField] private CanvasGroup pauseCanvasGroup;
    [SerializeField] private CanvasGroup upgradeCanvasGroup;
    #endregion

    #region State
    private TweenHandle _fadePause;
    private TweenHandle _fadeUpgrade;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        if (instance == null) instance = this;
        else { Destroy(this.gameObject); return; }
    }

    private void OnEnable()
    {
        if (gameStates != null)
            gameStates.OnStateChanged += HandleStateChanged;
    }

    private void OnDisable()
    {
        if (gameStates != null)
            gameStates.OnStateChanged -= HandleStateChanged;

        if (_fadePause.IsValid) _fadePause.Cancel();
        if (_fadeUpgrade.IsValid) _fadeUpgrade.Cancel();
    }

    public void Start()
    {
        StartCoroutine(StartGame());
    }
    #endregion

    #region Boot Flow
    private IEnumerator StartGame()
    {
        // Initialize GameStates
        gameStates.Initialize();

        #if UNITY_ANDROID || UNITY_IOS
        startButton.gameObject.SetActive(true);
        #endif

        yield return StartCoroutine(SetupOnLoad());
        yield return StartCoroutine(SetupOnSetupGame());
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
    #endregion

    #region Input
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (GameStates.instance.IsGameplayRunning()) PauseGame();
            else if (GameStates.instance.IsGamePaused()) ResumeGame();
        }
    }
    #endregion

    #region Public API (Triggers)
    public void PauseGame()   => GameStates.instance.SetGameState_Pause();
    public void ResumeGame()  => GameStates.instance.GoBackToOldGameState();

    // Si usas el menú de upgrades desde gameplay
    public void OpenUpgradeMenu() => GameStates.instance.SetGameState_Upgrade();

    public void RestartGame()
    {
        // Limpia UI y reanuda timeScale antes de cambiar de escena
        HidePauseUIImmediate();
        HideUpgradeUIImmediate();
        Time.timeScale = 1f;

        GameStates.instance.GoBackToOldGameState();
        GameSceneManager.LoadScene(GameScene.Gameplay);
    }

    public void ExitGame()
    {
        HidePauseUIImmediate();
        HideUpgradeUIImmediate();
        Time.timeScale = 1f;

        GameStates.instance.GoBackToOldGameState();
        GameSceneManager.LoadScene(GameScene.MainMenu);
    }
    #endregion

    #region State Handler
    private void HandleStateChanged(GameState oldState, GameState newState)
    {
        switch (newState)
        {
            case GameState.Pause:
                EnterPause();
                break;

            case GameState.Upgrade:
                EnterUpgrade();
                break;

            case GameState.Gameplay:
                // venimos de Pause/Upgrade
                ExitOverlaysAndResume();
                break;
        }
    }

    private void EnterPause()
    {
        // UI
        if (_fadePause.IsValid) _fadePause.Cancel();
        _fadePause = pauseCanvasGroup.FadeIn(0.3f, ease: Ease.OutCubic, unscaledTime: true);

        // Pausa gameplay global, UI sigue por unscaled
        Time.timeScale = 0f;
    }

    private void EnterUpgrade()
    {
        if (upgradeCanvasGroup == null)
        {
            // Si aún no implementas upgrade UI, trata esto como pause visual
            EnterPause();
            return;
        }

        if (_fadeUpgrade.IsValid) _fadeUpgrade.Cancel();
        _fadeUpgrade = upgradeCanvasGroup.FadeIn(0.3f, ease: Ease.OutCubic, unscaledTime: true);

        Time.timeScale = 0f;
    }

    private void ExitOverlaysAndResume()
    {
        // Oculta ambos overlays por si acaso
        if (_fadePause.IsValid) _fadePause.Cancel();
        if (_fadeUpgrade.IsValid) _fadeUpgrade.Cancel();

        if (pauseCanvasGroup)   _fadePause   = pauseCanvasGroup.FadeOut(0.2f, ease: Ease.InCubic,  unscaledTime: true);
        if (upgradeCanvasGroup) _fadeUpgrade = upgradeCanvasGroup.FadeOut(0.2f, ease: Ease.InCubic, unscaledTime: true);

        // Reanuda gameplay
        Time.timeScale = 1f;
    }
    #endregion

    #region UI Helpers (Immediate)
    private void HidePauseUIImmediate()
    {
        if (!pauseCanvasGroup) return;
        if (_fadePause.IsValid) _fadePause.Cancel();
        pauseCanvasGroup.alpha = 0f;
        pauseCanvasGroup.interactable = false;
        pauseCanvasGroup.blocksRaycasts = false;
    }

    private void HideUpgradeUIImmediate()
    {
        if (!upgradeCanvasGroup) return;
        if (_fadeUpgrade.IsValid) _fadeUpgrade.Cancel();
        upgradeCanvasGroup.alpha = 0f;
        upgradeCanvasGroup.interactable = false;
        upgradeCanvasGroup.blocksRaycasts = false;
    }
    #endregion
}
