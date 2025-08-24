using System.Collections;
using Aldha.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [Header("Refs")]
    public GameStates gameStates;

    [Header("UI")]
    [SerializeField] private Button startButton;
    [SerializeField] private CanvasGroup pauseCanvasGroup;
    [SerializeField] private CanvasGroup upgradeCanvasGroup;

    [Header("Upgrades Auto Open")]
    [SerializeField] private bool autoOpenUpgradeOnPhaseChange = true;

    [Header("Upgrade Shop")]
    [SerializeField] private UpgradeManagerShop upgradeShop;

    private TweenHandle _fadePause;
    private TweenHandle _fadeUpgrade;

    private Player.Phase _cachedPhase;
    private bool _upgradeVisible;
    private bool _pauseVisible;

    private void Awake()
    {
        if (instance == null) instance = this;
        else { Destroy(gameObject); return; }
    }

    private void OnEnable()
    {
        if (gameStates != null)
            gameStates.OnStateChanged += HandleStateChanged;

        if (upgradeShop != null)
            upgradeShop.OnConfirmed += HandleUpgradesConfirmed;
    }

    private void OnDisable()
    {
        if (gameStates != null)
            gameStates.OnStateChanged -= HandleStateChanged;

        if (upgradeShop != null)
            upgradeShop.OnConfirmed -= HandleUpgradesConfirmed;

        if (_fadePause.IsValid) _fadePause.Cancel();
        if (_fadeUpgrade.IsValid) _fadeUpgrade.Cancel();
    }

    public void Start()
    {
        StartCoroutine(Boot());
    }

    private IEnumerator Boot()
    {
        gameStates.Initialize();

#if UNITY_ANDROID || UNITY_IOS
        if (startButton) startButton.gameObject.SetActive(true);
#endif

        yield return StartCoroutine(SetupOnLoad());
        yield return StartCoroutine(SetupOnSetupGame());
        yield return StartCoroutine(SetupOnPlay());

        var p = Player.Instance;
        if (p != null) _cachedPhase = p.currentPhase;
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
            if (GameStates.instance.IsGameplayRunning()) PauseGame();
            else if (GameStates.instance.IsGamePaused()) ResumeGame();
        }
    }

    public void UpdateCachedPhase(Player.Phase phase)
    {
        _cachedPhase = phase;
    }

    public void PauseGame()  => GameStates.instance.SetGameState_Pause();
    public void ResumeGame() => GameStates.instance.GoBackToOldGameState();
    public void OpenUpgradeMenu() => GameStates.instance.SetGameState_Upgrade();

    public void RestartGame()
    {
        HidePauseImmediate();
        HideUpgradeImmediate();
        Time.timeScale = 1f;
        GameStates.instance.GoBackToOldGameState();
        GameSceneManager.LoadScene(GameScene.Gameplay);
    }

    public void ExitGame()
    {
        HidePauseImmediate();
        HideUpgradeImmediate();
        Time.timeScale = 1f;
        GameStates.instance.GoBackToOldGameState();
        GameSceneManager.LoadScene(GameScene.MainMenu);
    }

    private void HandleStateChanged(GameState oldState, GameState newState)
    {
        switch (newState)
        {
            case GameState.Pause:
                ShowPause();
                break;
            case GameState.Upgrade:
                ShowUpgrade();
                break;
            case GameState.Gameplay:
                HideOverlaysAndResume();
                break;
        }
    }

    private void ShowPause()
    {
        if (pauseCanvasGroup)
        {
            pauseCanvasGroup.interactable = true;
            pauseCanvasGroup.blocksRaycasts = true;
            if (_fadePause.IsValid) _fadePause.Cancel();
            _fadePause = pauseCanvasGroup.FadeIn(0.3f, ease: Ease.OutCubic, unscaledTime: true);
        }
        _pauseVisible = true;
        Time.timeScale = 0f;
    }

    private void ShowUpgrade()
    {
        if (upgradeShop != null)
        {
            var phase = Player.Instance ? Player.Instance.currentPhase : _cachedPhase;
            upgradeShop.ShowForPhase(phase);
        }

        if (upgradeCanvasGroup)
        {
            upgradeCanvasGroup.interactable = true;
            upgradeCanvasGroup.blocksRaycasts = true;
            if (_fadeUpgrade.IsValid) _fadeUpgrade.Cancel();
            _fadeUpgrade = upgradeCanvasGroup.FadeIn(0.3f, ease: Ease.OutCubic, unscaledTime: true);
        }
        _upgradeVisible = true;
        Time.timeScale = 0f;
    }

    private void HideOverlaysAndResume()
    {
        if (pauseCanvasGroup)
        {
            if (_fadePause.IsValid) _fadePause.Cancel();
            _fadePause = pauseCanvasGroup.FadeOut(0.2f, ease: Ease.InCubic, unscaledTime: true);
            pauseCanvasGroup.interactable = false;
            pauseCanvasGroup.blocksRaycasts = false;
        }
        if (upgradeCanvasGroup)
        {
            if (_fadeUpgrade.IsValid) _fadeUpgrade.Cancel();
            _fadeUpgrade = upgradeCanvasGroup.FadeOut(0.2f, ease: Ease.InCubic, unscaledTime: true);
            upgradeCanvasGroup.interactable = false;
            upgradeCanvasGroup.blocksRaycasts = false;
        }
        _pauseVisible = false;
        _upgradeVisible = false;
        Time.timeScale = 1f;
    }

    private void HidePauseImmediate()
    {
        if (!pauseCanvasGroup) return;
        if (_fadePause.IsValid) _fadePause.Cancel();
        pauseCanvasGroup.alpha = 0f;
        pauseCanvasGroup.interactable = false;
        pauseCanvasGroup.blocksRaycasts = false;
        _pauseVisible = false;
    }

    private void HideUpgradeImmediate()
    {
        if (!upgradeCanvasGroup) return;
        if (_fadeUpgrade.IsValid) _fadeUpgrade.Cancel();
        upgradeCanvasGroup.alpha = 0f;
        upgradeCanvasGroup.interactable = false;
        upgradeCanvasGroup.blocksRaycasts = false;
        _upgradeVisible = false;
    }

    private void HandleUpgradesConfirmed(System.Collections.Generic.List<UpgradeSO> purchased)
    {
        ResumeGame();
    }
}