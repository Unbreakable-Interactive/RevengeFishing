using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public abstract class Entity : MonoBehaviour
{
    [SerializeField] protected Rigidbody2D rb;
    public Rigidbody2D Rigidbody2D => rb;
    
    [Header("Character Stats")]
    [SerializeField] protected int _powerLevel;
    [SerializeField] public EntityFatigue entityFatigue;

    [Header("Is Above Water?")]
    [SerializeField] protected bool isAboveWater = true;

    [Header("Air Movement Settings")]
    [SerializeField] protected float airGravityScale = 2f;
    [SerializeField] protected float airDrag = 1.5f;
    [SerializeField] protected float airMaxSpeed = 3f;

    [Header("Underwater Movement Settings")]
    [SerializeField] protected float underwaterGravityScale = 0f;
    [SerializeField] protected float underwaterDrag = 0.5f;
    [SerializeField] protected float underwaterMaxSpeed = 5f;

    
    
    [Header("Entity Type")]
    [SerializeField] protected EntityType entityType = EntityType.Generic;

    private bool isInitialized = false;

    public int PowerLevel => _powerLevel;
    public bool IsAboveWater => isAboveWater;

    public enum EntityType
    {
        Generic,
        Player,
        Enemy,
        FishingProjectile
    }

    protected virtual void Awake()
    {
        EnsureRigidbody2D();
    }

    private void EnsureRigidbody2D()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>() ?? gameObject.AddComponent<Rigidbody2D>();
        }
    }

    public virtual void Initialize()
    {
        EnsureRigidbody2D();

        if (GetComponent<Player>() != null)
        {
            _powerLevel = 100;
        }

        entityFatigue = new EntityFatigue(_powerLevel != 0 ? _powerLevel : 100, 0);

        SetMovementMode(isAboveWater);
        isInitialized = true;

        GameLogger.Log($"{gameObject.name} - EntityMovement initialized successfully");
    }

    protected virtual void Update()
    {
        if (GameStates.instance.IsGameplayRunning())
        {
            UpdateLogic();
        }
    }

    protected virtual void UpdateLogic()
    {
        if (!isInitialized || rb == null) return;

        if (isAboveWater)
        {
            AirborneBehavior();
        }
        else
        {
            UnderwaterBehavior();
        }
    }
    

    public virtual void SetMovementMode(bool aboveWater)
    {
        isAboveWater = aboveWater;

        if (aboveWater)
        {
            ApplyAirbornePhysics();
        }
        else
        {
            ApplyUnderwaterPhysics();
        }
    }

    public virtual void ApplyAirbornePhysics()
    {
        if (rb != null)
        {
            rb.gravityScale = airGravityScale;
            rb.drag = airDrag;
        }
    }

    public virtual void ApplyUnderwaterPhysics()
    {
        if (rb != null)
        {
            rb.gravityScale = underwaterGravityScale;
            rb.drag = underwaterDrag;
        }
    }

    protected abstract void AirborneBehavior();
    protected abstract void UnderwaterBehavior();
}