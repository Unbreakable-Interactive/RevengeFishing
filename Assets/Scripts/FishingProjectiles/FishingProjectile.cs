using System.Collections;
using UnityEngine;

public abstract class FishingProjectile : EntityMovement
{
    [Header("Distance Constraint")]
    public float maxDistance = 15f;

    public Vector3 spawnPoint;
    public HookSpawner spawner;

    [Header("Player Interaction")]
    [SerializeField] public bool isBeingHeld = false; 
    [SerializeField] protected PlayerMovement player;
    protected CircleCollider2D hookCollider;

    // Event to notify fisherman
    public System.Action<bool> OnPlayerInteraction;

    [Header("Elastic Line Behavior")]
    [SerializeField] private float maxStretchDistance = 3f;    // How far beyond maxDistance player can stretch
    [SerializeField] private float stretchResistance = 15f;    // Resistance force when stretching
    [SerializeField] private float snapBackForce = 20f;        // Force applied when snapping back
    [SerializeField] private float maxStretchTime = 0.8f;      // Max time allowed in stretch zone
    [SerializeField] private AnimationCurve stretchCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    // Stretch state tracking
    private bool isStretching = false;
    private float stretchTimer = 0f;
    private float currentStretchAmount = 0f;

    [Header("Visual Events")]
    public System.Action<float, float, float> OnStretchChanged; // stretchAmount, stretchTimer, maxTime
    public System.Action<float> OnSnapBack; // snapStrength
    public System.Action OnStretchStarted;
    public System.Action OnStretchEnded;


    public virtual void Initialize()
    {
        // Call parent Start() to initialize EntityMovement
        base.Initialize();

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
            ConstrainToMaxDistance();

        }

    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("PlayerCollider") && !isBeingHeld)
        {
            StartHolding(player.transform);
        }
    }

    private void StartHolding(Transform playerTransform)
    {
        isBeingHeld = true;

        // Disable physics while held
        if (rb != null) rb.isKinematic = true;

        // Register this hook with the player
        if (player != null)
        {
            player.AddBitingHook(this);
        }

        // Notify fisherman
        OnPlayerInteraction?.Invoke(true);

        Debug.Log("Player is holding the fishing hook!");
    }

    // Add this method to handle when hook releases
    public void ReleasePlayer()
    {
        if (isBeingHeld && player != null)
        {
            // NEW: Unregister this hook from the player
            player.RemoveBitingHook(this);

            isBeingHeld = false;

            // Re-enable physics
            if (rb != null) rb.isKinematic = false;

            // Notify fisherman
            OnPlayerInteraction?.Invoke(false);

            Debug.Log("Hook released player!");
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

    
    private void ConstrainPlayerToMaxDistance()
    {
        if (player == null) return;

        float currentDistance = Vector3.Distance(player.transform.position, spawnPoint);
        float totalMaxDistance = maxDistance + maxStretchDistance;

        Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
        if (playerRb == null) return;

        // Normal range - no constraint needed
        if (currentDistance <= maxDistance)
        {
            // Reset stretch state if returning to normal range
            if (isStretching)
            {
                isStretching = false;
                stretchTimer = 0f;
                currentStretchAmount = 0f;
                OnStretchEnded?.Invoke();
                Debug.Log("Fishing line relaxed");
            }
            return;
        }

        // Beyond max distance - BOTH constrain player AND move hook
        if (currentDistance > maxDistance && currentDistance <= totalMaxDistance)
        {
            HandleStretchZone(currentDistance, playerRb);
            MoveHookToFollowPlayer(); // Hook follows player
        }

        //// Hard limit - force snap back if too far or stretched too long
        //if (currentDistance > totalMaxDistance || (isStretching && stretchTimer > maxStretchTime))
        //{
        //    SnapPlayerBack(playerRb);
        //}
    }

    private void MoveHookToFollowPlayer()
    {
        if (player == null) return;

        // Hook always follows player position when being held, regardless of distance
        if (isBeingHeld)
        {
            transform.position = player.transform.position;
            Debug.Log($"Hook following player to position: {player.transform.position}");
        }
    }

    private void HandleStretchZone(float currentDistance, Rigidbody2D playerRb)
    {
        currentStretchAmount = (currentDistance - maxDistance) / maxStretchDistance;
        currentStretchAmount = Mathf.Clamp01(currentStretchAmount);

        if (!isStretching)
        {
            isStretching = true;
            stretchTimer = 0f;
            OnStretchStarted?.Invoke(); // Notify visual system
            Debug.Log("Fishing line is stretching!");
        }

        stretchTimer += Time.deltaTime;

        // Notify visual system of stretch changes
        OnStretchChanged?.Invoke(currentStretchAmount, stretchTimer, maxStretchTime);

        // Apply increasing resistance force as stretch increases
        Vector3 directionToSpawn = (spawnPoint - player.transform.position).normalized;
        float resistanceMultiplier = stretchCurve.Evaluate(currentStretchAmount);
        Vector2 resistanceForce = directionToSpawn * stretchResistance * resistanceMultiplier;

        playerRb.AddForce(resistanceForce, ForceMode2D.Force);

        // Visual/audio feedback for stretching
        OnLineStretching(currentStretchAmount);

        Debug.Log($"Line stretch: {currentStretchAmount:F2} | Timer: {stretchTimer:F2} | Resistance: {resistanceMultiplier:F2}");
    }

    private void SnapPlayerBack(Rigidbody2D playerRb)
    {
        Vector3 directionToSpawn = (spawnPoint - player.transform.position).normalized;

        // Calculate snap force based on how much the line was stretched
        float snapMultiplier = Mathf.Lerp(1f, 2f, currentStretchAmount);
        Vector2 snapForce = directionToSpawn * snapBackForce * snapMultiplier;

        // Apply snap-back force
        playerRb.velocity = Vector2.zero; // Stop current movement
        playerRb.AddForce(snapForce, ForceMode2D.Impulse);

        // Position player at max allowed distance
        Vector3 allowedPosition = spawnPoint + (player.transform.position - spawnPoint).normalized * maxDistance;
        player.transform.position = allowedPosition;

        // Reset stretch state
        isStretching = false;
        stretchTimer = 0f;

        // Visual/audio feedback for snap
        // Notify visual system
        OnSnapBack?.Invoke(currentStretchAmount);
        OnStretchEnded?.Invoke();

        currentStretchAmount = 0f;

        Debug.Log($"SNAP! Player pulled back with force: {snapForce}");
    }

    // Visual and audio feedback methods
    private void OnLineStretching(float stretchAmount)
    {
        // Gradually increase tension effects
        if (stretchAmount > 0.5f)
        {
            // High tension - could add:
            // - Screen distortion
            // - Tension sound effects
            // - Line renderer color change
            // - Particle effects
            Debug.Log($"High line tension: {stretchAmount:F2}");
        }
    }

    private void OnLineSnapBack(float wasStretchAmount)
    {
        // Snap-back effects:
        // - Screen shake
        // - Snap sound effect
        // - Particle burst
        // - Visual impact

        Debug.Log($"SNAP BACK! Was stretched: {wasStretchAmount:F2}");

        // Example effects you could add:
        // CameraShake.Instance?.Shake(0.3f * wasStretchAmount, 0.5f);
        // AudioSource.PlayClipAtPoint(snapSound, player.transform.position);
        // snapEffect?.Play();
    }

    // Public methods to check stretch state (useful for UI or other systems)
    public bool IsLineStretching()
    {
        return isStretching;
    }

    public float GetStretchAmount()
    {
        return currentStretchAmount;
    }

    public float GetStretchTimer()
    {
        return stretchTimer;
    }

    // Reset stretch state method update
    private void ResetStretchState()
    {
        if (isStretching)
        {
            isStretching = false;
            stretchTimer = 0f;
            currentStretchAmount = 0f;
            OnStretchEnded?.Invoke(); // Notify visual system
            Debug.Log("Fishing line relaxed");
        }
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

    private void OnDestroy()
    {
        // Clean up player reference when hook is destroyed
        if (player != null && isBeingHeld)
        {
            player.RemoveBitingHook(this);
        }
    }

}
