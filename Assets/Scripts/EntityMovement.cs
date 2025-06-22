using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public abstract class EntityMovement : MonoBehaviour
{
    [SerializeField] protected Rigidbody2D rb;

    [Header("Character Stats")]
    [SerializeField] protected int _powerLevel;
    [SerializeField] protected int _fatigue;
    [SerializeField] protected int _maxFatigue;

    [Header("Water/Air Movement Settings")]
    public bool isAboveWater = true;
    public float airGravityScale = 2f;
    public float underwaterGravityScale = 0f;
    public float airDrag = 1.5f;
    public float underwaterDrag = 0.5f;
    public float airMaxSpeed = 3f;
    public float underwaterMaxSpeed = 5f;

    [Header("Entity Type")]
    public EntityType entityType = EntityType.Generic;

    private bool isInitialized = false;

    public int PowerLevel
    {
        get => _powerLevel;
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
        // ENSURE RIGIDBODY2D IS ALWAYS AVAILABLE
        EnsureRigidbody2D();
    }

    public virtual void Initialize()
    {
        EnsureRigidbody2D();

        if (GetComponent<PlayerMovement>() != null)
        {
            _powerLevel = 100;
        }

        SetMovementMode(isAboveWater);
        isInitialized = true;

        Debug.Log($"{gameObject.name} - EntityMovement initialized successfully");
    }

    private void EnsureRigidbody2D()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody2D>();
                Debug.Log($"{gameObject.name} - Added missing Rigidbody2D component");
            }
            else
            {
                Debug.Log($"{gameObject.name} - Found existing Rigidbody2D component");
            }
        }
    }

    protected virtual void Update()
    {
        // SAFETY CHECK: Don't do anything if not initialized
        if (!isInitialized || rb == null)
        {
            return;
        }

        if (isAboveWater)
        {
            AirborneBehavior();
        }
        else
        {
            UnderwaterBehavior();
        }
    }

    public virtual void OnEnterWater()
    {
        Debug.Log($"{gameObject.name} entered water");
        SetMovementMode(false);
    }

    public virtual void OnExitWater()
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
