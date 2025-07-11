using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouthMagnet : MonoBehaviour
{
    [Header("Magnet Settings")]
    [SerializeField] private float magneticForce = 10f;
    [SerializeField] private float maxMagnetRange = 1.4f;
    [SerializeField] private Vector2 magnetOffset = Vector2.zero;
    [SerializeField] private AnimationCurve forceCurve = AnimationCurve.EaseInOut(0f, 0.1f, 1f, 1f);

    [Header("Debug")]
    [SerializeField] private bool showDebugGizmos = true;

    private CircleCollider2D magnetCollider;
    [SerializeField] private List<FishingProjectile> attractedProjectiles = new List<FishingProjectile>();

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
        InitializeMagnet();
    }

    void FixedUpdate()
    {
        ApplyMagneticForce();
        CleanupNullReferences();
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

        if (maxMagnetRange > 0)
        {
            magnetCollider.radius = maxMagnetRange / 2f;
        }

        Debug.Log($"MouthMagnet initialized with range: {maxMagnetRange}");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        FishingProjectile projectile = other.GetComponentInParent<FishingProjectile>();

        if (projectile != null && !attractedProjectiles.Contains(projectile))
        {
            attractedProjectiles.Add(projectile);
            Debug.Log($"MouthMagnet: Started attracting {projectile.name}");
            Debug.Log($"Current attracted projectiles count: {attractedProjectiles.Count}");
        }

    }

    private void OnTriggerExit2D(Collider2D other)
    {
        FishingProjectile projectile = other.GetComponentInParent<FishingProjectile>();

        if (projectile != null && attractedProjectiles.Contains(projectile))
        {
            attractedProjectiles.Remove(projectile);
            Debug.Log($"MouthMagnet: Stopped attracting {projectile.name}");
        }
    }

    private void ApplyMagneticForce()
    {
        for (int i = attractedProjectiles.Count - 1; i >= 0; i--)
        {
            FishingProjectile projectile = attractedProjectiles[i];

            if (projectile == null)
            {
                attractedProjectiles.RemoveAt(i);
                continue;
            }

            if (projectile.isBeingHeld)
            {
                continue;
            }

            Rigidbody2D projectileRb = projectile.GetComponent<Rigidbody2D>();
            if (projectileRb == null) continue;

            Vector2 directionToMagnet = (MagnetCenter - (Vector2)projectile.transform.position);
            float distance = directionToMagnet.magnitude;

            if (distance < 0.1f) continue;

            directionToMagnet.Normalize();

            float normalizedDistance = distance / maxMagnetRange;
            float forceMultiplier = forceCurve.Evaluate(1f - normalizedDistance);

            Vector2 magneticPull = directionToMagnet * magneticForce * forceMultiplier;
            projectileRb.AddForce(magneticPull, ForceMode2D.Force);

            if (showDebugGizmos)
            {
                Debug.DrawLine(projectile.transform.position, MagnetCenter, Color.magenta, 0.1f);
            }
        }
    }

    private void CleanupNullReferences()
    {
        attractedProjectiles.RemoveAll(projectile => projectile == null);
    }

    private void OnDrawGizmosSelected()
    {
        if (showDebugGizmos)
        {
            Vector2 magnetCenter = MagnetCenter;

            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(magnetCenter, maxMagnetRange);

            Gizmos.color = new Color(1f, 0f, 1f, 0.1f);
            Gizmos.DrawSphere(magnetCenter, maxMagnetRange);

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
