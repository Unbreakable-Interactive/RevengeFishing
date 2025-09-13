using UnityEngine;
using System.Collections;

public class DroppedTool : Entity
{
    [Header("Drop Physics")]
    private Vector2 dropForce;
    private bool hasAntiRotated = false;

    [Header("Auto Cleanup")]
    [SerializeField] private float maxLifetime = 60f; 
    [SerializeField] private float maxDistanceFromPlayer = 50f;
    [SerializeField] private float distanceCheckInterval = 2f;
    
    private float spawnTime;
    private Coroutine cleanupCoroutine;
    private Player player;

    protected void Start()
    {
        dropForce = new Vector2(
            Random.Range(-1f, 1f),
            Random.Range(0.2f, 1f)
        );

        spawnTime = Time.time;
        player = Player.Instance;

        Initialize();
        
        cleanupCoroutine = StartCoroutine(AutoCleanupSystem());
    }

    public override void Initialize()
    {
        base.Initialize();

        rb.AddForce(dropForce * 20, ForceMode2D.Impulse);
        rb.AddTorque(-dropForce.x * 8, ForceMode2D.Impulse);
        GameLogger.LogVerbose($"Tool dropped in direction {dropForce}");
    }

    protected override void AirborneBehavior()
    {
    }

    protected override void UnderwaterBehavior()
    {
        if (!hasAntiRotated)
        {
            rb.AddTorque(dropForce.x * 8, ForceMode2D.Impulse);
            rb.drag = 2f;
            hasAntiRotated = true;
        }

        if (rb.velocity.magnitude < 0.5f)
        {
            GameLogger.LogVerbose($"DroppedTool {gameObject.name} destroyed - low velocity underwater");
            DestroyTool();
        }
    }

    private IEnumerator AutoCleanupSystem()
    {
        while (true)
        {
            yield return new WaitForSeconds(distanceCheckInterval);

            if (Time.time - spawnTime > maxLifetime)
            {
                GameLogger.LogVerbose($"DroppedTool {gameObject.name} destroyed - exceeded max lifetime ({maxLifetime}s)");
                DestroyTool();
                yield break;
            }

            if (player != null)
            {
                float distanceToPlayer = Vector2.Distance(transform.position, player.transform.position);
                
                if (distanceToPlayer > maxDistanceFromPlayer)
                {
                    GameLogger.LogVerbose($"DroppedTool {gameObject.name} destroyed - too far from player ({distanceToPlayer:F1}m > {maxDistanceFromPlayer}m)");
                    DestroyTool();
                    yield break;
                }
            }

            if (transform.position.y < -100f)
            {
                GameLogger.LogVerbose($"DroppedTool {gameObject.name} destroyed - fell into void (y={transform.position.y:F1})");
                DestroyTool();
                yield break;
            }

            if (rb != null && rb.velocity.magnitude < 0.1f && isAboveWater)
            {
                yield return new WaitForSeconds(5f);
                
                if (rb != null && rb.velocity.magnitude < 0.1f)
                {
                    GameLogger.LogVerbose($"DroppedTool {gameObject.name} destroyed - static on surface for too long");
                    DestroyTool();
                    yield break;
                }
            }
        }
    }

    private void DestroyTool()
    {
        if (cleanupCoroutine != null)
        {
            StopCoroutine(cleanupCoroutine);
        }

        GameObject parent = transform.parent?.gameObject ?? gameObject;
        Destroy(parent);
    }

    private void OnDestroy()
    {
        if (cleanupCoroutine != null)
        {
            StopCoroutine(cleanupCoroutine);
        }
    }

    public void SetCleanupParameters(float lifetime, float maxDistance)
    {
        maxLifetime = lifetime;
        maxDistanceFromPlayer = maxDistance;
    }
}
