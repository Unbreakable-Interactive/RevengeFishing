using UnityEngine;

public abstract class Enemy : MonoBehaviour
{
    [Header("Enemy Stats")]
    public float powerLevel = 100f;
    public float maxFatigue = 100f;
    public float currentFatigue = 0f;

    [Header("Visual Settings")]
    public SpriteRenderer spriteRenderer;

    [Header("Debug")]
    public bool showDebugInfo = true;

    // Core components - all enemies need these
    protected Rigidbody2D rb;
    protected Collider2D enemyCollider;

    // State
    public bool isDefeated = false;

    // Events
    public System.Action<Enemy> OnEnemyDefeated;
    public System.Action<Enemy> OnEnemyEaten;
    public System.Action<Enemy> OnEnemyEscaped;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }

        enemyCollider = GetComponent<Collider2D>();
        if (enemyCollider == null)
        {
            enemyCollider = gameObject.AddComponent<BoxCollider2D>();
        }

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        // Set enemy tag
        gameObject.tag = "Enemy";
    }

    protected virtual void Start()
    {
        InitializeEnemy();
    }

    protected virtual void Update()
    {
        UpdateEnemyBehavior();

        // Check if defeated
        if (currentFatigue >= maxFatigue && !isDefeated)
        {
            BecomeDefeated();
        }
    }

    // Abstract methods - each enemy type implements these differently
    protected abstract void InitializeEnemy();
    protected abstract void UpdateEnemyBehavior();
    protected abstract void HandleDefeatedState();

    public virtual void SetPowerLevel(float newPowerLevel)
    {
        powerLevel = newPowerLevel;
    }

    public virtual void TakeFatigueDamage(float damage)
    {
        if (isDefeated) return;

        currentFatigue = Mathf.Clamp(currentFatigue + damage, 0f, maxFatigue);

        if (showDebugInfo)
        {
            Debug.Log($"{gameObject.name} took {damage} fatigue damage. Current: {currentFatigue}/{maxFatigue}");
        }
    }

    public virtual void BecomeDefeated()
    {
        if (isDefeated) return;

        isDefeated = true;
        OnEnemyDefeated?.Invoke(this);

        // Let derived classes handle specific defeat behavior
        HandleDefeatedState();

        if (showDebugInfo)
        {
            Debug.Log($"{gameObject.name} has been defeated!");
        }
    }

    public virtual void OnTriggerEnter2D(Collider2D other)
    {
        // Check if collided with player (being eaten)
        if (isDefeated && IsPlayer(other.gameObject))
        {
            BeEaten();
        }
    }

    protected virtual void BeEaten()
    {
        OnEnemyEaten?.Invoke(this);

        if (showDebugInfo)
        {
            Debug.Log($"{gameObject.name} was eaten by the player!");
        }

        Destroy(gameObject);
    }

    protected virtual bool IsPlayer(GameObject obj)
    {
        return obj.CompareTag("Player") || obj.GetComponent<PlayerMovement>() != null ||
               obj.GetComponentInParent<PlayerMovement>() != null;
    }

    // Consumption values for player stat absorption
    public virtual float GetHungerValue()
    {
        return powerLevel * 0.5f; // 50% of power level for hunger
    }

    public virtual float GetFatigueReliefValue()
    {
        return powerLevel * 0.5f; // Remaining 50% can relieve fatigue
    }

    public virtual float GetPowerLevelGain()
    {
        return powerLevel * 0.1f; // 10% of power level for player growth
    }

    void OnDrawGizmos()
    {
        if (showDebugInfo)
        {
            // Draw fatigue bar
            Gizmos.color = Color.red;
            float fatiguePercent = currentFatigue / maxFatigue;
            Vector3 fatigueBarStart = transform.position + Vector3.up * 1.2f + Vector3.left * 0.5f;
            Vector3 fatigueBarEnd = fatigueBarStart + Vector3.right * fatiguePercent;
            Gizmos.DrawLine(fatigueBarStart, fatigueBarEnd);

            // Draw power level indicator
            Gizmos.color = Color.white;
            Vector3 powerPos = transform.position + Vector3.up * 1.5f;
        }
    }
}
