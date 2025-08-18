using System.Collections.Generic;
using UnityEngine;

public class IdleDetector : MonoBehaviour
{
    private Enemy parentEnemy;
    private Collider2D detectorCollider;

    private void Awake()
    {
        parentEnemy = GetComponentInParent<Enemy>();
        detectorCollider = GetComponent<Collider2D>();

        if (parentEnemy == null)
        {
            GameLogger.LogError($"IdleDetector on {gameObject.name} could not find parent Enemy component!");
        }

        if (detectorCollider == null || !detectorCollider.isTrigger)
        {
            GameLogger.LogError($"IdleDetector on {gameObject.name} needs a trigger collider!");
        }
    }

    public bool ShouldAvoidIdle()
    {
        bool canAvoidBasedOnTool = false;
        bool isOnBoat = false;
    
        if (parentEnemy is LandEnemy landEnemy)
        {
            canAvoidBasedOnTool = !landEnemy.fishingToolEquipped;
            isOnBoat = landEnemy.assignedPlatform is BoatPlatform;
        }
        else if (parentEnemy is BoatLandEnemy boatEnemy)
        {
            canAvoidBasedOnTool = !boatEnemy.fishingToolEquipped;
            isOnBoat = true;
        }
    
        if (isOnBoat)
        {
            return false; 
        }
    
        return canAvoidBasedOnTool && IsOverlappingWithIdleEnemy();
    }


    public bool IsOverlappingWithIdleEnemy()
    {
        if (detectorCollider == null) return false;

        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(1 << LayerMask.NameToLayer("IdleDetector"));
        filter.useTriggers = true;

        List<Collider2D> overlappingColliders = new List<Collider2D>();
        int numOverlapping = detectorCollider.OverlapCollider(filter, overlappingColliders);

        foreach (Collider2D overlappingCollider in overlappingColliders)
        {
            if (overlappingCollider == detectorCollider) continue;

            IdleDetector otherDetector = overlappingCollider.GetComponent<IdleDetector>();
            if (otherDetector != null && otherDetector.parentEnemy != null)
            {
                if (IsEnemyIdle(otherDetector.parentEnemy))
                {
                    GameLogger.LogVerbose($"{parentEnemy.name} found overlap with idle enemy {otherDetector.parentEnemy.name}");
                    return true;
                }
            }
        }

        return false;
    }

    public int GetOverlappingIdleEnemyCount()
    {
        if (detectorCollider == null) return 0;

        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(1 << LayerMask.NameToLayer("IdleDetector"));
        filter.useTriggers = true;

        List<Collider2D> overlappingColliders = new List<Collider2D>();
        detectorCollider.OverlapCollider(filter, overlappingColliders);

        int count = 0;
        foreach (Collider2D overlappingCollider in overlappingColliders)
        {
            if (overlappingCollider == detectorCollider) continue;

            IdleDetector otherDetector = overlappingCollider.GetComponent<IdleDetector>();
            if (otherDetector != null && IsEnemyIdle(otherDetector.parentEnemy))
            {
                count++;
            }
        }

        return count;
    }

    private bool IsEnemyIdle(Enemy enemy)
    {
        if (enemy == null) return false;
        if (enemy.GetState() != Enemy.EnemyState.Alive) return false;

        // CAMBIO: Manejar ambos tipos de enemigos
        if (enemy is LandEnemy landEnemy)
        {
            return landEnemy.MovementStateLand == LandEnemy.LandMovementState.Idle;
        }
        else if (enemy is BoatLandEnemy boatEnemy)
        {
            return boatEnemy.MovementStateLand == BoatLandEnemy.LandMovementState.Idle;
        }

        return false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;

        CircleCollider2D circleCollider = GetComponent<CircleCollider2D>();
        if (circleCollider != null)
        {
            Gizmos.DrawWireSphere((Vector3)transform.position + (Vector3)circleCollider.offset, circleCollider.radius);
        }

        BoxCollider2D boxCollider = GetComponent<BoxCollider2D>();
        if (boxCollider != null)
        {
            Gizmos.DrawWireCube((Vector3)transform.position + (Vector3)boxCollider.offset, boxCollider.size);
        }
    }
}
