using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouthMagnet : MonoBehaviour
{
    [Header("Magnet Settings")]
    [SerializeField] private float magneticForce = 10f;
    [SerializeField] private float baseMagnetRange = 1.4f;
    [SerializeField] private Vector2 magnetOffset = Vector2.zero;
    [SerializeField] private AnimationCurve forceCurve = AnimationCurve.EaseInOut(0f, 0.1f, 1f, 1f);
    
    [Header("Scaling")]
    [SerializeField] private bool scaleWithPlayer = true;

    [Header("Debug")]
    [SerializeField] private bool showDebugGizmos = true;

    private CircleCollider2D magnetCollider;
    private PlayerScaler playerScaler;
    [SerializeField] private List<Entity> attractedEntities = new List<Entity>();
    
    public float CurrentMagnetRange => scaleWithPlayer && playerScaler != null 
        ? baseMagnetRange * playerScaler.GetCurrentScaleMultiplier() 
        : baseMagnetRange;

    public Vector2 MagnetCenter
    {
        get
        {
            // Transform the local offset by the object's rotation
            Vector2 rotatedOffset = transform.TransformDirection(magnetOffset);
            return (Vector2)transform.position + rotatedOffset;
        }
    }

    void Start()
    {
        InitializeReferences();
        InitializeMagnet();
    }
    
    private void InitializeReferences()
    {
        if (scaleWithPlayer)
        {
            playerScaler = GetComponentInParent<PlayerScaler>();
            if (playerScaler == null)
            {
                Debug.LogWarning("MouthMagnet: scaleWithPlayer is enabled but no PlayerScaler found in parent objects!");
            }
        }
    }

    void FixedUpdate()
    {
        UpdateMagnetRange();
        ApplyMagneticForceToEntities();
        CleanupNullReferences();
    }
    
    private void UpdateMagnetRange()
    {
        if (magnetCollider != null && scaleWithPlayer && playerScaler != null)
        {
            float currentRange = CurrentMagnetRange;
            //magnetCollider.radius = currentRange / 2f;
        }
    }

    private void InitializeMagnet()
    {
        magnetCollider = GetComponent<CircleCollider2D>();

        if (magnetCollider == null)
        {
            Debug.LogError("MouthMagnet requires a CircleCollider2D component!");
            return;
        }

        if (!magnetCollider.isTrigger)
        {
            magnetCollider.isTrigger = true;
            Debug.Log("MouthMagnet: Set collider to trigger mode");
        }

        //float currentRange = CurrentMagnetRange;
        //if (currentRange > 0)
        //{
        //    magnetCollider.radius = currentRange / 2f;
        //}

        //Debug.Log($"MouthMagnet initialized with range: {currentRange} (base: {baseMagnetRange})");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Entity entity = other.GetComponentInParent<Entity>();

        if (entity != null && !attractedEntities.Contains(entity))
        {
            attractedEntities.Add(entity);
            Debug.Log($"MouthMagnet: Started attracting entity {entity.name} of type {entity.GetType().Name}");
            Debug.Log($"Current attracted entities count: {attractedEntities.Count}");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        Entity entity = other.GetComponentInParent<Entity>();

        if (entity != null && attractedEntities.Contains(entity))
        {
            attractedEntities.Remove(entity);
            Debug.Log($"MouthMagnet: Stopped attracting entity {entity.name}");
        }
    }

    private void ApplyMagneticForceToEntities()
    {
        for (int i = attractedEntities.Count - 1; i >= 0; i--)
        {
            Entity entity = attractedEntities[i];

            if (entity == null)
            {
                attractedEntities.RemoveAt(i);
                continue;
            }

            if (!ShouldAttractEntity(entity))
            {
                attractedEntities.RemoveAt(i);
                Debug.Log($"MouthMagnet: Removed {entity.name} - entity no longer meets attraction criteria");
                continue;
            }

            Rigidbody2D entityRb = entity.GetComponent<Rigidbody2D>();
            if (entityRb == null) continue;

            Vector2 directionToMagnet = (MagnetCenter - (Vector2)entity.transform.position);
            float distance = directionToMagnet.magnitude;

            if (distance < 0.1f) continue;

            directionToMagnet.Normalize();

            float normalizedDistance = distance / CurrentMagnetRange;
            float forceMultiplier = forceCurve.Evaluate(1f - normalizedDistance);

            Vector2 magneticPull = directionToMagnet * magneticForce * forceMultiplier;
            entityRb.AddForce(magneticPull, ForceMode2D.Force);

            if (showDebugGizmos)
            {
                Color lineColor = GetDebugColorForEntity(entity);
                Debug.DrawLine(entity.transform.position, MagnetCenter, lineColor, 0.1f);
            }
        }
    }

    private bool ShouldAttractEntity(Entity entity)
    {
        if (entity is FishingProjectile projectile)
        {
            return !projectile.isBeingHeld;
        }
        
        if (entity is Enemy enemy)
        {
            return enemy.GetState() != Enemy.EnemyState.Alive;
        }

        return false;
    }

    private Color GetDebugColorForEntity(Entity entity)
    {
        if (entity is FishingProjectile)
        {
            return Color.magenta;
        }
        
        if (entity is Enemy)
        {
            return Color.red;
        }

        return Color.white;
    }

    private void CleanupNullReferences()
    {
        attractedEntities.RemoveAll(entity => entity == null);
    }

    private void OnDrawGizmosSelected()
    {
        if (showDebugGizmos)
        {
            Vector2 magnetCenter = MagnetCenter;
            float displayRange = CurrentMagnetRange;

            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(magnetCenter, displayRange);

            Gizmos.color = new Color(1f, 0f, 1f, 0.1f);
            Gizmos.DrawSphere(magnetCenter, displayRange);

            // Draw a small cross to show the exact center
            Gizmos.color = Color.yellow;
            float crossSize = 0.2f;
            Gizmos.DrawLine(magnetCenter + Vector2.left * crossSize, magnetCenter + Vector2.right * crossSize);
            Gizmos.DrawLine(magnetCenter + Vector2.up * crossSize, magnetCenter + Vector2.down * crossSize);

            // Draw the offset vector in local space
            Gizmos.color = Color.cyan;
            Vector2 localOffsetStart = transform.position;
            Gizmos.DrawLine(localOffsetStart, magnetCenter);
        }
    }

    // Helper method to update collider offset when magnetOffset changes in inspector
    private void OnValidate()
    {
        if (magnetCollider != null)
        {
            magnetCollider.offset = magnetOffset;
        }
    }
}
