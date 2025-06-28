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

    [Header("Player Reference")]
    [SerializeField] protected Player player;
    
    [Header("Variables for AI decisions")]
    [SerializeField] protected float minActionTime = 1f;
    [SerializeField] protected float maxActionTime = 4f;
    [SerializeField] protected float nextActionTime;
    
    [Header("Water Enemy Variables")]
    [SerializeField] protected float swimForce;
    [SerializeField] protected float minSwimSpeed;

    
    #region Base Behaviours

    protected virtual void Start()
    {
        entityType = EntityType.Enemy;
        gameObject.layer = LayerMask.NameToLayer(LayerNames.ENEMY);
        
        if (player == null)
            player = FindObjectOfType<Player>();
        
        Initialize();
    }

    public override void Initialize()
    {
        base.Initialize();
        
        if (player == null)
            player = FindObjectOfType<Player>();
        
        EnemySetup();
    }

    protected virtual void EnemySetup()
    {
        if (player != null)
        {
            _powerLevel = player.PowerLevel;
        }
        else
        {
            _powerLevel = 100;
        }
        
        _fatigue = 0;
        _maxFatigue = _powerLevel;
        _state = EnemyState.Alive;

        CalculateTier();
        
        Debug.Log($"✅ {gameObject.name} - Enemy initialized with power level {_powerLevel}");
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

    public abstract void WaterMovement();

    public override void SetMovementMode(bool aboveWater)
    {
        base.SetMovementMode(aboveWater); // Call base implementation
    }
    
    public void TakeFatigue(int playerPowerLevel)
    {
        // 5% more fatigue
        _fatigue += (int)((float)playerPowerLevel * .05f);

        // Check if enemy should be defeated
        if (_fatigue >= _maxFatigue && _state == EnemyState.Alive)
        {
            TriggerDefeat();
        }
    }
    
    public virtual void TriggerAlive()
    {
        ChangeState_Alive();
        _fatigue = 0;

        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.gravityScale = 1f;
        }

        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = false;
        }

        Debug.Log($"{gameObject.name} state reset to Alive");
    }

    protected virtual void TriggerDefeat()
    {
        Debug.Log($"{gameObject.name} has been defeated!");

        // Change state to defeated
        ChangeState_Defeated();

        // Interrupt all current actions
        InterruptAllActions();

        // Start defeat behaviors
        StartDefeatBehaviors();
    }

    protected virtual void TriggerEaten()
    {
        Debug.Log($"{gameObject.name} has been EATEN!");
        // Change state to eaten
        ChangeState_Eaten();
        // Interrupt all current actions
        InterruptAllActions();
        // Handle any specific eaten logic (like cleanup)
        TriggerDead(); 
        //this will be changed later after TriggerDead() is implemented
    }

    protected virtual void TriggerDead()
    {
        Debug.Log($"{gameObject.name} has DIED!");
        // Change state to dead
        ChangeState_Dead();
        // Interrupt all current actions
        InterruptAllActions();
        // Handle any specific death logic (like cleanup)
        EnemyDie();
    }

    protected virtual void TriggerEscape()
    {
        Debug.Log($"{gameObject.name} has ESCAPED! The player can no longer catch this enemy.");

        // Destroy the parent FishermanHandler (or this object if no parent)
        GameObject objectToDestroy = transform.parent != null ? transform.parent.gameObject : gameObject;
        // ! Instead of destroy, return to pool.
        Destroy(objectToDestroy);
    }
    
    /// <summary>
    /// Set time to next action
    /// </summary>
    protected virtual void ScheduleNextAction()
    {
        float actionDuration = UnityEngine.Random.Range(minActionTime, maxActionTime);
        nextActionTime = Time.time + actionDuration;
    }

    protected virtual void InterruptAllActions()
    {
        // Clear any scheduled actions
        ScheduleNextAction(); // Prevent further AI decisions

        Debug.Log($"{gameObject.name} - All actions interrupted due to defeat");
    }

    protected virtual void StartDefeatBehaviors()
    {
        // Make enemy phase through platforms by turning collider into trigger
        Collider2D enemyCollider = GetComponent<Collider2D>();

        // If not found, look for collider in children (like your setup)
        if (enemyCollider == null)
        {
            enemyCollider = GetComponentInChildren<Collider2D>();
        }

        if (enemyCollider != null)
        {
            enemyCollider.isTrigger = true;
            Debug.Log($"{gameObject.name} - Collider set to trigger (phasing through platforms)");
        }
    }
    
    protected virtual void EnemyDie()
    {
        Debug.Log($"{gameObject.name} has been REVERSE FISHED!");

        // Destroy the parent FishermanHandler (or this object if no parent)
        GameObject objectToDestroy = transform.parent != null ? transform.parent.gameObject : gameObject;
        Destroy(objectToDestroy);
    }

    #endregion

    #region State Management

    public EnemyState GetState() => _state;
    
    public virtual void ChangeState_Alive() => _state = EnemyState.Alive;
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

    protected virtual void OnDestroy()
    {
        // Cleanup any references to player or platform
        player = null;
    }

    #endregion
}
