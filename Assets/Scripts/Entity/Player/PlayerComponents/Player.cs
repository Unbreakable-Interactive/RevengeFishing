using UnityEngine.SceneManagement;
using System.Collections.Generic;
using UnityEngine;
using RevengeFishing.Hunger;

public class Player : Entity
{
    public enum Status
    {
        Alive,
        Fished,
        Starved,
        Slain
    }

    [Header("Player Configuration")]
    [SerializeField] private PlayerConfig playerConfig;

    [Header("Player Components")]
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private PlayerPhases playerPhases;
    [SerializeField] private PlayerHooks playerHooks;
    [SerializeField] private PlayerVisuals playerVisuals;

    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Collider2D colliderToShare;
    [SerializeField] protected HungerHandler hungerHandler;
    [SerializeField] protected MouthMagnet magnet;

    public HungerHandler HungerHandler => hungerHandler;
    public MouthMagnet Magnet => magnet;
    public Collider2D ColliderToShare => colliderToShare;

    [SerializeField] protected Status status;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;

    public static Player Instance { get; private set; }

    public List<FishingProjectile> activeBitingHooks => playerHooks?.activeBitingHooks ?? new List<FishingProjectile>();
    public Animator animator => GetComponent<Animator>();

    protected override void Awake()
    {
        base.Awake();
        
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            GameLogger.LogWarning("Multiple Player instances found! Destroying duplicate.");
            Destroy(gameObject);
            return;
        }

        InitializeComponents();
    }

    private void Start()
    {
        entityType = EntityType.Player;
        Initialize();
    }

    public override void Initialize()
    {
        base.Initialize();

        if (mainCamera == null)
            mainCamera = Camera.main;

        hungerHandler = new HungerHandler(_powerLevel, entityFatigue, 0);

        SetupRigidbody();

        playerInput?.Initialize(mainCamera);
        playerMovement?.Initialize();
        playerPhases?.Initialize(_powerLevel, playerConfig);
        playerHooks?.Initialize();
        playerVisuals?.Initialize();

        SubscribeToInputEvents();
        
        DebugLog("Player initialized successfully");
    }

    private void SetupRigidbody()
    {
        if (rb != null)
        {
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.drag = underwaterDrag;
            rb.gravityScale = underwaterGravityScale;
        
            DebugLog($"Rigidbody setup - Constraints: {rb.constraints}, Drag: {rb.drag}, Gravity: {rb.gravityScale}");
        }
    }


    private void InitializeComponents()
    {
        if (playerInput == null)
            playerInput = GetComponent<PlayerInput>() ?? gameObject.AddComponent<PlayerInput>();
        
        if (playerMovement == null)
            playerMovement = GetComponent<PlayerMovement>() ?? gameObject.AddComponent<PlayerMovement>();
        
        if (playerPhases == null)
            playerPhases = GetComponent<PlayerPhases>() ?? gameObject.AddComponent<PlayerPhases>();
        
        if (playerHooks == null)
            playerHooks = GetComponent<PlayerHooks>() ?? gameObject.AddComponent<PlayerHooks>();
        
        if (playerVisuals == null)
            playerVisuals = GetComponent<PlayerVisuals>() ?? gameObject.AddComponent<PlayerVisuals>();
    }

    protected override void Update()
    {
        base.Update();

        playerVisuals?.UpdateRotationFlip(transform.rotation);
        playerPhases?.CheckForMaturation(_powerLevel);
        playerHooks?.UpdateHookConstraints();
    }

    protected override void UnderwaterBehavior()
    {
        if (playerMovement != null && playerInput != null)
        {
            playerMovement.HandleUnderwaterMovement(
                playerInput.GetCurrentMousePosition(),
                playerInput.IsMousePressed(),
                playerInput.IsMousePressed()
            );
        }
    }

    protected override void AirborneBehavior()
    {
        playerMovement?.HandleAirborneMovement();
    }

    public override void SetMovementMode(bool aboveWater)
    {
        base.SetMovementMode(aboveWater);
        playerMovement?.OnMovementModeChanged(aboveWater);
        DebugLog($"Player movement mode: {(aboveWater ? "AIRBORNE" : "UNDERWATER")}");
    }

    private void SubscribeToInputEvents()
    {
        if (playerInput != null && playerHooks != null)
        {
            playerInput.OnMouseClick += playerHooks.OnInputClick;
        }
    }

    public void GainPowerFromEating(int enemyPowerLevel)
    {
        int powerGain = Mathf.RoundToInt((float)enemyPowerLevel * 0.1f);
        _powerLevel += powerGain;
        
        entityFatigue.maxFatigue = _powerLevel;
        hungerHandler?.GainedPowerFromEating(enemyPowerLevel, _powerLevel);
        
        DebugLog($"Player gained {powerGain} power! New level: {_powerLevel}");
    }

    public void PlayerDie(Status deathType)
    {
        StartCoroutine(HandlePlayerDeath(deathType));
    }

    private System.Collections.IEnumerator HandlePlayerDeath(Status deathType)
    {
        DeathManager.SetDeathType(deathType);
        GameLogger.Log($"Player died: {deathType}");
        
        yield return new WaitForSeconds(0f);
        SceneManager.LoadScene("GameOver");
    }

    public void TriggerBite()
    {
        playerPhases?.TriggerBiteAnimation();
    }

    public void TakeFishingFatigue(float fatigueDamage)
    {
        entityFatigue.fatigue += (int)fatigueDamage;

        if (entityFatigue.fatigue >= entityFatigue.maxFatigue)
        {
            PlayerDie(Status.Fished);
        }
    }

    public void AddBitingHook(FishingProjectile hook)
    {
        playerHooks?.AddBitingHook(hook);
        TriggerBite();
    }

    public void RemoveBitingHook(FishingProjectile hook)
    {
        playerHooks?.RemoveBitingHook(hook);
    }

    public void SetPositionConstraint(Vector3 center, float radius, System.Action<Vector3> violationCallback = null)
    {
        playerHooks?.SetPositionConstraint(center, radius, violationCallback);
    }

    public void RemovePositionConstraint()
    {
        playerHooks?.RemovePositionConstraint();
    }

    public int GetFatigue() => entityFatigue.fatigue;
    public Phase GetCurrentPhase() => playerPhases?.GetCurrentPhase() ?? Phase.Infant;
    public float GetPhaseProgress() => playerPhases?.GetPhaseProgress(_powerLevel) ?? 0f;
    public bool IsMoving() => playerMovement?.IsMoving() ?? false;
    public float GetCurrentSpeed() => playerMovement?.GetCurrentSpeed() ?? 0f;

    private void DebugLog(string message)
    {
        if (enableDebugLogs) GameLogger.LogVerbose($"[Player] {message}");
    }

    private void OnDestroy()
    {
        if (playerInput != null && playerHooks != null)
        {
            playerInput.OnMouseClick -= playerHooks.OnInputClick;
        }
    }
}
