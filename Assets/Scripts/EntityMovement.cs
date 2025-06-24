using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public abstract class EntityMovement : MonoBehaviour
{
    protected Rigidbody2D rb;

    [Header("Character Stats")]
    [SerializeField] protected int _powerLevel;
    [SerializeField] protected int _fatigue;
    [SerializeField] protected int _maxFatigue;

    [Header("Water/Air Movement Settings")]
    [SerializeField] protected bool isAboveWater = true;
    [SerializeField] protected float airGravityScale = 2f;
    [SerializeField] protected float underwaterGravityScale = 0f;
    [SerializeField] protected float airDrag = 1.5f;
    [SerializeField] protected float underwaterDrag = 0.5f;
    [SerializeField] protected float airMaxSpeed = 3f;
    [SerializeField] protected float underwaterMaxSpeed = 5f;

    [Header("Entity Type")]
    [SerializeField] protected EntityType entityType = EntityType.Generic;

    // Add this property after the existing fields
    public int PowerLevel
    {
        get => _powerLevel;
    }

    public bool IsAboveWater
    {
        get => isAboveWater;
    }

    public enum EntityType
    {
        Generic,
        Player,
        Enemy,
        Hook
    }

    protected virtual void Awake()
    {
        rb = GetComponentInChildren<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }
    }

    protected virtual void Start()
    {
        Initialize(_powerLevel);
        SetMovementMode(isAboveWater);
    }

    protected abstract void Initialize(int powerLevel);

    protected virtual void Update()
    {
        if (isAboveWater)
        {
            AirborneBehavior();
        }
        else
        {
            UnderwaterBehavior();
        }
    }

    protected virtual void OnEnterWater()
    {
        Debug.Log($"{gameObject.name} entered water");
        SetMovementMode(false);
    }

    protected virtual void OnExitWater()
    {
        Debug.Log($"{gameObject.name} exited water");
        SetMovementMode(true);
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

    // Abstract methods that derived classes must implement
    protected abstract void AirborneBehavior();
    protected abstract void UnderwaterBehavior();
}
