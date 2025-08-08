using UnityEngine;
using System.Collections.Generic;

public class MouthMagnet : MonoBehaviour
{
    [Header("Magnet Settings")]
    [SerializeField] private float attractionForce = 10f;
    [SerializeField] private float maxAttractionDistance = 3f;
    [SerializeField] private AnimationCurve attractionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Entity Filtering")]
    [SerializeField] private bool attractEnemies = true;
    [SerializeField] private bool attractFishingProjectiles = true;
    [SerializeField] private bool attractDroppedTools = true;

    private List<Entity> attractedEntities = new List<Entity>();
    private Player player;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;

    private void Awake()
    {
        player = GetComponentInParent<Player>();
        if (player == null)
        {
            GameLogger.LogError("MouthMagnet: Could not find Player component in parent!");
        }
    }

    private void FixedUpdate()
    {
        if (attractedEntities.Count > 0)
        {
            ApplyMagneticForceToEntities();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Entity entity = other.GetComponentInParent<Entity>();

        if (entity == null) return;

        if (!ShouldAttractEntity(entity)) return;

        if (!attractedEntities.Contains(entity))
        {
            if (entity.GetComponent<Enemy>() != null && entity.GetComponent<Enemy>().State == Enemy.EnemyState.Alive) return;
            attractedEntities.Add(entity);
            GameLogger.LogVerbose($"MouthMagnet: Started attracting entity {entity.name} of type {entity.GetType().Name}");
            GameLogger.LogVerbose($"Current attracted entities count: {attractedEntities.Count}");
        }
        
        UpdateAnimatorObjectsInRange();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        Entity entity = other.GetComponentInParent<Entity>();
        RemoveEntity(entity);
    }

    public void RemoveEntity(Entity entity)
    {
        if (entity != null && attractedEntities.Contains(entity))
        {
            attractedEntities.Remove(entity);
            GameLogger.LogVerbose($"MouthMagnet: Stopped attracting entity {entity.name}");
        }
    
        UpdateAnimatorObjectsInRange();
    }

    private void UpdateAnimatorObjectsInRange()
    {
        if (player != null && player.animator != null)
        {
            player.animator.SetInteger("objectsInRange", attractedEntities.Count);
        }
    }

    private void ApplyMagneticForceToEntities()
    {
        Vector3 magnetCenter = transform.position;

        for (int i = attractedEntities.Count - 1; i >= 0; i--)
        {
            Entity entity = attractedEntities[i];

            if (entity == null || entity.gameObject == null)
            {
                attractedEntities.RemoveAt(i);
                continue;
            }

            Vector3 direction = magnetCenter - entity.transform.position;
            float distance = direction.magnitude;

            if (distance > maxAttractionDistance)
            {
                attractedEntities.RemoveAt(i);
                continue;
            }

            if (distance < 0.1f) continue;

            float normalizedDistance = distance / maxAttractionDistance;
            float curveValue = attractionCurve.Evaluate(1f - normalizedDistance);
            float force = attractionForce * curveValue;

            direction.Normalize();

            Rigidbody2D entityRb = entity.GetComponent<Rigidbody2D>();
            if (entityRb != null)
            {
                entityRb.AddForce(direction * force, ForceMode2D.Force);

                if (enableDebugLogs)
                {
                    GameLogger.LogVerbose($"Applying magnetic force {force:F2} to {entity.name}");
                }
            }
        }

        UpdateAnimatorObjectsInRange();
    }

    private bool ShouldAttractEntity(Entity entity)
    {
        if (entity.CurrentEntityType == Entity.EntityType.Enemy && !attractEnemies)
            return false;

        if (entity.CurrentEntityType == Entity.EntityType.FishingProjectile && !attractFishingProjectiles)
            return false;

        if (entity.GetComponent<DroppedTool>() != null && !attractDroppedTools)
            return false;

        return true;
    }

    public void SetAttractionForce(float newForce)
    {
        attractionForce = newForce;
    }

    public void SetMaxAttractionDistance(float newDistance)
    {
        maxAttractionDistance = newDistance;
    }

    public int GetAttractedEntitiesCount()
    {
        return attractedEntities.Count;
    }

    public List<Entity> GetAttractedEntities()
    {
        return new List<Entity>(attractedEntities);
    }

    private void DebugLog(string message)
    {
        if (enableDebugLogs) GameLogger.LogVerbose($"[MouthMagnet] {message}");
    }
}
