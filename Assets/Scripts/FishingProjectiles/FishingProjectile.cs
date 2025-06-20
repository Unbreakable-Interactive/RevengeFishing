using System.Collections;
using UnityEngine;

public abstract class FishingProjectile : EntityMovement
{
    [Header("Distance Constraint")]
    public float maxDistance = 15f;

    public Vector3 spawnPoint;
    protected HookSpawner spawner;

    [Header("Player Interaction")]
    [SerializeField] public bool isBeingHeld = false; 
    [SerializeField] protected PlayerMovement player;
    protected CircleCollider2D hookCollider;

    // Event to notify fisherman
    public System.Action<bool> OnPlayerInteraction;

    protected virtual void Awake()
    {
        // Call parent Start() to initialize EntityMovement
        base.Start();

        // Set entity type to Hook
        entityType = EntityType.Hook;
        player = GameObject.FindGameObjectWithTag("Player")?.GetComponent<PlayerMovement>();

        InitializeProjectile();
    }

    protected override void Update()
    {
        if (isBeingHeld && player != null)
        {
            // Position hook at player center
            transform.position = player.transform.position;

            // Apply same distance constraint but to player position
            ConstrainPlayerToMaxDistance();
        }
        else
        {
            // Normal behavior
            base.Update();
        }

        ConstrainToMaxDistance();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("PlayerCollider") && !isBeingHeld)
        {
            StartHolding(player.transform);
        }
    }

    private void StartHolding(Transform player)
    {
        isBeingHeld = true;

        // Disable physics while held
        if (rb != null) rb.isKinematic = true;

        // Notify fisherman
        OnPlayerInteraction?.Invoke(true);

        Debug.Log("Player is holding the fishing hook!");
    }

    private void ConstrainPlayerToMaxDistance()
    {
        if (player == null) return;

        float currentDistance = Vector3.Distance(player.transform.position, spawnPoint);

        if (currentDistance > maxDistance)
        {
            // Same rope physics but applied to player
            Vector3 direction = (player.transform.position - spawnPoint).normalized;
            Vector3 constrainedPosition = spawnPoint + direction * maxDistance;

            player.transform.position = constrainedPosition;

            // Apply rope physics to player if they have rigidbody
            Rigidbody2D playerRb = player.transform.GetComponent<Rigidbody2D>();
            if (playerRb != null)
            {
                Vector2 currentVelocity = playerRb.velocity;
                Vector2 radialDirection = direction;
                Vector2 tangentDirection = new Vector2(-radialDirection.y, radialDirection.x);

                float tangentVelocity = Vector2.Dot(currentVelocity, tangentDirection);
                playerRb.velocity = tangentDirection * tangentVelocity;
            }
        }
    }

    protected virtual void InitializeProjectile()
    {
        hookCollider = GetComponent<CircleCollider2D>();
        if (hookCollider != null)
        {
            hookCollider.isTrigger = true;
        }

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

            //Debug.Log($"Hook constrained to rope - swinging with tangent velocity: {tangentVelocity}");
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
