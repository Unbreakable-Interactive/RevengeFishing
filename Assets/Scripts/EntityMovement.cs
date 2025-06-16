using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public abstract class EntityMovement : MonoBehaviour
{
    protected Rigidbody2D rb;

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
