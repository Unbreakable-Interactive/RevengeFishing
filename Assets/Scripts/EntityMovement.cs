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
    public bool isAboveWater = true;
    public float airGravityScale = 2f;
    public float underwaterGravityScale = 0f;
    public float airDrag = 1.5f;
    public float underwaterDrag = 0.5f;
    public float airMaxSpeed = 3f;
    public float underwaterMaxSpeed = 5f;

    [Header("Entity Type")]
    public EntityType entityType = EntityType.Generic;

    // Add this property after the existing fields
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

    public bool IsAboveWater
    {
        get => isAboveWater;
        set => isAboveWater = value;
    }

    protected virtual void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }

        if (GetComponent<PlayerMovement>() != null)
        {
            _powerLevel = 100;
        }
        else
        {
            //_powerLevel = 100; //change this to scale with the player later
        }

        SetMovementMode(isAboveWater);
    }

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

        if (isAboveWater)
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
