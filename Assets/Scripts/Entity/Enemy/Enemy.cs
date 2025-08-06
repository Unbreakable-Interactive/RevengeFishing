using System;
using UnityEngine;
using Utils;

public abstract class Enemy : Entity
{
    public enum Tier
    {
        Tier1=0,
        Tier2=1,
        Tier3=2,
        Tier4=3,
        Tier5=4,
        Tier6=5
    }

    public enum EnemyState
    {
        Alive=0,
        Defeated=1,
        Eaten=2,
        Dead=3
    }

    [SerializeField] protected Tier _tier;
    [SerializeField] protected EnemyState _state;
    public EnemyState State => _state;
    
    [Header("Player Reference")]
    [SerializeField] protected Player player;

    [Header("Pull Mechanic")]
    [SerializeField] protected bool hasReceivedFirstFatigue = false;
    [SerializeField] protected bool canPullPlayer = false;
    [SerializeField] protected float pullForce = 5f;
    [SerializeField] protected float pullDuration = 1f;

    [Header("Decision making Timer")]
    [SerializeField] protected float minActionTime = 1f;
    [SerializeField] protected float maxActionTime = 4f;
    [SerializeField] protected float nextActionTime;

    [Header("Debug")]
    [SerializeField] protected bool enableDebugMessages = false;

    [Header("References")]
    [SerializeField] protected Collider2D bodyCollider;
    [SerializeField] protected GameObject parentContainer;

    public Collider2D BodyCollider => bodyCollider;
    public GameObject ParentContainer => parentContainer;
    
    public Action<Enemy> OnEnemyDied;
    
    public float NextActionTime
    {
        get { return nextActionTime; }
        set { nextActionTime = value; }
    }

    public bool HasReceivedFirstFatigue
    {
        get { return hasReceivedFirstFatigue; }
        set { hasReceivedFirstFatigue = value; }
    }

    public bool CanPullThePlayer
    {
        get { return canPullPlayer; }
        set { canPullPlayer = value; }
    }

    #region Water Enemy Variables
    [SerializeField] protected float swimForce;
    [SerializeField] protected float minSwimSpeed;
    #endregion

    #region Base Behaviours
    protected virtual void Start()
    {
        entityType = EntityType.Enemy;
    
        if (player == null)
        {
            player = Player.Instance;
        }

        AutoAssignReferences();
        
        Initialize();
    }

    private void AutoAssignReferences()
    {
        if (bodyCollider == null)
        {
            bodyCollider = GetComponent<Collider2D>();
            if (enableDebugMessages && bodyCollider != null)
                GameLogger.LogVerbose($"{gameObject.name}: Auto-assigned bodyCollider");
        }

        if (parentContainer == null)
        {
            parentContainer = FindMyHandler();
            if (enableDebugMessages && parentContainer != null)
                GameLogger.LogVerbose($"{gameObject.name}: Auto-assigned parentContainer to {parentContainer.name}");
        }
    }

    public override void Initialize()
    {
        base.Initialize();

        if (player == null)
        {
            player = Player.Instance;
        }

        EnemySetup();
    }

    protected virtual void EnemySetup()
    {
        if (_powerLevel <= 0)
        {
            if (player != null)
            {
                _powerLevel = player.PowerLevel;
            }
            else
            {
                _powerLevel = 100;
            }
        }

        entityFatigue.fatigue = 0;
        entityFatigue.maxFatigue = _powerLevel;
        _state = EnemyState.Alive;

        CalculateTier();
    }

    public virtual void SetPowerLevel(int newPowerLevel)
    {
        _powerLevel = newPowerLevel;
        entityFatigue.maxFatigue = _powerLevel;
        entityFatigue.fatigue = 0;
        GameLogger.Log($"{gameObject.name} power level set to {_powerLevel}");
    }

    private void CalculateTier()
    {
        if (_powerLevel > 10000000) _tier = Tier.Tier6;
        else if (_powerLevel > 1000000) _tier = Tier.Tier5;
        else if (_powerLevel > 100000) _tier = Tier.Tier4;
        else if (_powerLevel > 10000) _tier = Tier.Tier3;
        else if (_powerLevel > 1000) _tier = Tier.Tier2;
        else _tier = Tier.Tier1;
    }

    protected override void Update()
    {
        base.Update();
    }

    public abstract void WaterMovement();

    public override void SetMovementMode(bool aboveWater)
    {
        base.SetMovementMode(aboveWater);
    }

    public void TakeFatigue(int playerPowerLevel)
    {
        if (!hasReceivedFirstFatigue)
        {
            hasReceivedFirstFatigue = true;
            canPullPlayer = true;
            OnFirstFatigueReceived();
            GameLogger.Log($"{gameObject.name} received first fatigue damage - can now pull player!");
        }

        entityFatigue.fatigue += (int)((float)playerPowerLevel * .05f);

        if (entityFatigue.fatigue >= entityFatigue.maxFatigue && _state == EnemyState.Alive)
        {
            TriggerDefeat();
        }
    }

    protected virtual void OnFirstFatigueReceived()
    {
        GameLogger.LogVerbose($"{gameObject.name} can now pull the player!");
    }

    public bool CanPullPlayer()
    {
        return canPullPlayer && _state == EnemyState.Alive;
    }

    public virtual void ResetFatigue()
    {
        hasReceivedFirstFatigue = false;
        canPullPlayer = false;
        entityFatigue.fatigue = 0;
        
        if (enableDebugMessages)
            GameLogger.LogVerbose($"{gameObject.name}: Fatigue reset to 0");
    }

    public virtual void ChangeState_Alive()
    {
        _state = EnemyState.Alive;
        
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.simulated = true;
            rb.gravityScale = 1f;
        }

        if (bodyCollider != null)
        {
            bodyCollider.isTrigger = false;
        }
        
        if (this is LandEnemy landEnemy)
        {
            landEnemy.HasStartedFloating = false;
        }
        
        if (enableDebugMessages)
            GameLogger.LogVerbose($"{gameObject.name}: State changed to Alive, physics reset");
    }

    public virtual void ScheduleNextAction()
    {
        float actionDuration = UnityEngine.Random.Range(minActionTime, maxActionTime);
        nextActionTime = Time.time + actionDuration;
    }

    public virtual void TriggerAlive()
    {
        ChangeState_Alive();
        ResetFatigue();

        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.gravityScale = 1f;
            rb.simulated = true;
            rb.freezeRotation = true;
        }

        if (bodyCollider != null)
        {
            bodyCollider.isTrigger = false;
        }

        GameLogger.Log($"{gameObject.name} state reset to Alive with complete physics reset");
    }

    protected virtual void TriggerDefeat()
    {
        GameLogger.Log($"{gameObject.name} has been defeated!");
        ChangeState_Defeated();
        InterruptAllActions();
        StartDefeatBehaviors();
    }

    protected virtual void TriggerEaten()
    {
        GameLogger.Log($"{gameObject.name} has been EATEN!");
        ChangeState_Eaten();
        InterruptAllActions();
        player.GetComponentInChildren<MouthMagnet>().RemoveEntity(this);
        player.TriggerBite();
        TriggerDead();
    }

    protected virtual void TriggerDead()
    {
        GameLogger.Log($"{gameObject.name} has DIED!");
        ChangeState_Dead();
        InterruptAllActions();


        if (player != null)
        {
            player.GainPowerFromEating(_powerLevel);
            GameLogger.Log($"Player consumed {gameObject.name} with power level {_powerLevel}");
        }
        
        EnemyDie();
    }

    protected virtual void TriggerEscape()
    {
        GameLogger.Log($"{gameObject.name} has ESCAPED! The player can no longer catch this enemy.");
        ReturnToPool();
    }

    protected virtual void EnemyDie()
    {
        GameLogger.Log($"{gameObject.name} has been REVERSE FISHED!");
        
        OnEnemyDied?.Invoke(this);
        
        ReturnToPool();
    }

    private void ReturnToPool()
    {
        GameObject handler = parentContainer != null ? parentContainer : FindMyHandler();
    
        if (handler == null)
        {
            GameLogger.LogError($"Could not find handler for enemy {gameObject.name}! Destroying instead.");
            Destroy(gameObject);
            return;
        }
    
        if (this is LandEnemy)
        {
            NotifySpawnHandlerOfDeath();
        }
    
        CleanupBeforePoolReturn();
    
        if (this is LandEnemy && SimpleObjectPool.Instance != null)
        {
            SimpleObjectPool.Instance.ReturnToPool("LandFisherman", handler);
        }
        else
        {
            GameLogger.LogVerbose($"{gameObject.name} lifecycle managed by BoatCrewManager");
            gameObject.SetActive(false);
        }
    }
    
    private GameObject FindMyHandler()
    {
        if (parentContainer != null)
        {
            return parentContainer;
        }

        Transform current = transform;
    
        while (current != null)
        {
            string name = current.name.ToLower();
        
            if (name.Contains("landfishermanhandler") || 
                name.Contains("fishermanhandler"))
            {
                return current.gameObject;
            }
        
            current = current.parent;
        }
    
        return null;
    }

    private void NotifySpawnHandlerOfDeath()
    {
        SpawnHandler[] allSpawnHandlers = FindObjectsOfType<SpawnHandler>();
        
        foreach (SpawnHandler handler in allSpawnHandlers)
        {
            if (handler.config != null && 
                this is LandEnemy && 
                handler.config.enemyType == SpawnHandlerConfig.EnemyType.LandFisherman)
            {
                handler.OnEnemyDestroyed(gameObject);
                break;
            }
        }
    }

    protected virtual void CleanupBeforePoolReturn()
    {
        StopAllCoroutines();
        
        if (this is LandEnemy landEnemy)
        {
            if (landEnemy.GetAssignedPlatform() != null)
            {
                landEnemy.GetAssignedPlatform().UnregisterEnemy(landEnemy);
                landEnemy.SetAssignedPlatform(null);
            }
        }
    }

    protected virtual void InterruptAllActions()
    {
        ScheduleNextAction();
        GameLogger.LogVerbose($"{gameObject.name} - All actions interrupted due to defeat");
    }

    protected virtual void StartDefeatBehaviors()
    {
        if (bodyCollider != null)
        {
            bodyCollider.isTrigger = true;
            GameLogger.LogVerbose($"{gameObject.name} - Body collider set to trigger (phasing through platforms)");
        }
        else
        {
            Collider2D enemyCollider = GetComponent<Collider2D>();
            if (enemyCollider == null)
            {
                enemyCollider = GetComponentInChildren<Collider2D>();
            }

            if (enemyCollider != null)
            {
                enemyCollider.isTrigger = true;
                GameLogger.LogVerbose($"{gameObject.name} - Fallback collider set to trigger");
            }
        }
    }
    #endregion

    #region State Management
    public EnemyState GetState() => _state;
    public virtual void ChangeState_Defeated() => _state = EnemyState.Defeated;
    public virtual void ChangeState_Eaten() => _state = EnemyState.Eaten;
    public virtual void ChangeState_Dead() => _state = EnemyState.Dead;
    #endregion

    #region Actions
    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        if (_state == EnemyState.Defeated && other.CompareTag("PlayerCollider"))
        {
            ChangeState_Eaten();
            TriggerEaten();
        }
    }

    #endregion
}

