using System.Collections;
using UnityEngine;

public abstract class FishingProjectile : EntityMovement
{
    [Header("Distance Constraint")]
    public float maxDistance = 15f;

    protected Vector3 spawnPoint;
    protected HookSpawner spawner;

    protected virtual void Awake()
    {
        // Call parent Start() to initialize EntityMovement
        base.Start();

        // Set entity type to Hook
        entityType = EntityType.Hook;

        InitializeProjectile();
    }

    protected override void Update()
    {
        base.Update();

        ConstrainToMaxDistance();
    }

    protected virtual void InitializeProjectile()
    {

        spawnPoint = transform.position;
        OnProjectileSpawned();
    }

    protected virtual void ConstrainToMaxDistance()
    {
        float currentDistance = Vector3.Distance(transform.position, spawnPoint);

        if (currentDistance > maxDistance)
        {
            // Calculate direction from spawn point to current position
            Vector3 direction = (transform.position - spawnPoint).normalized;

            // Position hook exactly at max distance (rope constraint)
            Vector3 constrainedPosition = spawnPoint + direction * maxDistance;
            transform.position = constrainedPosition;

            // Project velocity onto the tangent of the circle (rope physics)
            Vector2 currentVelocity = rb.velocity;
            Vector2 radialDirection = direction;
            Vector2 tangentDirection = new Vector2(-radialDirection.y, radialDirection.x); // Perpendicular to radial

            // Remove radial velocity component (can't move closer/farther from spawn point)
            float tangentVelocity = Vector2.Dot(currentVelocity, tangentDirection);

            // Apply only tangential velocity (creates swinging motion)
            rb.velocity = tangentDirection * tangentVelocity;

            Debug.Log($"Hook constrained to rope - swinging with tangent velocity: {tangentVelocity}");
        }
    }

    public virtual void ThrowProjectile(Vector2 throwDirection, float throwForce)
    {
        rb.AddForce(throwDirection.normalized * throwForce, ForceMode2D.Impulse);
        OnProjectileThrown();
    }

    public virtual void RetractProjectile()
    {
        OnProjectileRetracted();
    }

    protected IEnumerator IProjectileRetracted()
    {
        yield return new WaitUntil(() => maxDistance <= 0.1f);
        // Notify the spawner before destroying
        if (spawner != null)
        {
            spawner.OnHookDestroyed();
        }
        Destroy(gameObject);
    }

    public void SetSpawner(HookSpawner hookSpawner)
    {
        spawner = hookSpawner;
    }

    public float GetCurrentDistance()
    {
        return Vector3.Distance(transform.position, spawnPoint);
    }

    // Implement EntityMovement abstract methods
    protected override void AirborneBehavior()
    {
        // Hook behavior when in air - could add wind effects, etc.
        OnAirborneBehavior();
    }

    protected override void UnderwaterBehavior()
    {
        // Hook behavior when underwater - could add water effects, etc.
        OnUnderwaterBehavior();
    }

    // Abstract methods for specific projectile behavior
    protected abstract void OnProjectileSpawned();
    protected abstract void OnProjectileThrown();
    protected abstract void OnProjectileRetracted();

    // New abstract methods for environment behavior
    protected abstract void OnAirborneBehavior();
    protected abstract void OnUnderwaterBehavior();
}
