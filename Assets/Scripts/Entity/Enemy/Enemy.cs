using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static LandEnemy;

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
    [SerializeField] protected Player player; // Reference to the player object

    [Header("Pull Mechanic")]
    [SerializeField] protected bool hasReceivedFirstFatigue = false;
    [SerializeField] protected bool canPullPlayer = false;
    [SerializeField] protected float pullForce = 5f;
    [SerializeField] protected float pullDuration = 1f;

    [Header("Decisionmaking Timer")]
    [SerializeField] protected float minActionTime; //Minimum seconds enemy will do an action, like walk, idle, or run
    [SerializeField] protected float maxActionTime; //Maximum seconds enemy will do an action, like walk, idle, or run
    [SerializeField] protected float nextActionTime; //actual seconds until next action decision

    #region Water Enemy Variables

    [SerializeField] protected float swimForce;
    [SerializeField] protected float minSwimSpeed;
    //protected float maxSwimSpeed;
    //already assigned in EntityMovement.cs as underwaterMaxSpeed

    #endregion

    #region Base Behaviours

    protected virtual void Start()
    {
        // Set entity type for water detection
        entityType = EntityType.Enemy;
        gameObject.layer = LayerMask.NameToLayer("Enemy");

        if (player == null)
        {
            player = FindObjectOfType<Player>();
        }
        
        // Enemy-specific initialization
        Initialize();
    }

    public override void Initialize()
    {
        base.Initialize();

        if (player == null)
        {
            player = FindObjectOfType<Player>();
        }

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
        // Call base Update for water detection logic
        base.Update();
    }


    public abstract void WaterMovement();


    // Override SetMovementMode to add enemy-specific behavior
    public override void SetMovementMode(bool aboveWater)
    {
        base.SetMovementMode(aboveWater); // Call base implementation
    }

    public void TakeFatigue(int playerPowerLevel)
    {
        if (!hasReceivedFirstFatigue)
        {
            hasReceivedFirstFatigue = true;
            canPullPlayer = true;
            OnFirstFatigueReceived();
            Debug.Log($"{gameObject.name} received first fatigue damage - can now pull player!");
        }

        // 5% more fatigue
        _fatigue += (int)((float)playerPowerLevel * .05f);

        // Check if enemy should be defeated
        if (_fatigue >= _maxFatigue && _state == EnemyState.Alive)
        {
            TriggerDefeat();
        }

        // return Mathf.Clamp(_fatigue, 0, _maxFatigue);
    }

    protected virtual void OnFirstFatigueReceived()
    {
        // Override in derived classes for specific enemy types
        Debug.Log($"{gameObject.name} can now pull the player!");
    }

    public bool CanPullPlayer()
    {
        return canPullPlayer && _state == EnemyState.Alive;
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
        // Transfer portion of this enemy's power to the player
        if (player != null)
        {
            player.GainPowerFromEating(_powerLevel);
            Debug.Log($"Player consumed {gameObject.name} with power level {_powerLevel}");
        }
        // Handle any specific death logic (like cleanup)
        EnemyDie();
    }

    protected virtual void TriggerEscape()
    {
        Debug.Log($"{gameObject.name} has ESCAPED! The player can no longer catch this enemy.");

        // Find the SpawnHandler to properly return this enemy to the pool
        SpawnHandler spawnHandler = FindObjectOfType<SpawnHandler>();
        if (spawnHandler != null)
        {
            // Get the root object (FishermanHandler or this object)
            GameObject objectToReturn = transform.parent != null ? transform.parent.gameObject : gameObject;

            // Return to pool instead of destroying
            spawnHandler.OnEnemyDestroyed(objectToReturn);
            Debug.Log($"{gameObject.name} returned to pool after escape");
        }
        else
        {
            Debug.LogWarning($"No SpawnHandler found! Destroying {gameObject.name} as fallback");
            GameObject objectToDestroy = transform.parent != null ? transform.parent.gameObject : gameObject;
            Destroy(objectToDestroy);
        }
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

        // Find the SpawnHandler to properly return this enemy to the pool
        SpawnHandler spawnHandler = FindObjectOfType<SpawnHandler>();
        if (spawnHandler != null)
        {
            // Get the root object (FishermanHandler or this object)
            GameObject objectToReturn = transform.parent != null ? transform.parent.gameObject : gameObject;

            // Return to pool instead of destroying
            spawnHandler.OnEnemyDestroyed(objectToReturn);
            Debug.Log($"{gameObject.name} returned to pool instead of being destroyed");
        }
        else
        {
            Debug.LogWarning($"No SpawnHandler found! Destroying {gameObject.name} as fallback");
            GameObject objectToDestroy = transform.parent != null ? transform.parent.gameObject : gameObject;
            Destroy(objectToDestroy);
        }
    }

    #endregion

    #region State Management

    // Make sure your GetState() method is public
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
