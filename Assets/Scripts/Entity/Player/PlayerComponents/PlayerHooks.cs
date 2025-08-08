using System.Collections.Generic;
using UnityEngine;

public class PlayerHooks : MonoBehaviour
{
    [Header("Fishing Hook Interaction")]
    public List<FishingProjectile> activeBitingHooks = new List<FishingProjectile>();

    [Header("Line Extension")]
    [SerializeField] private float lineExtensionAmount = 1f;
    [SerializeField] private float maxLineExtension = 12f;
    [SerializeField] private float lineExtensionSpeedPenalty = 0.6f;

    private bool isAtMaxHookDistance = false;
    private float originalMaxSpeed;

    [Header("External Constraints")]
    private bool isConstrainedByExternalForce = false;
    private Vector3 constraintCenter;
    private float constraintRadius;
    private System.Action<Vector3> onConstraintViolation;

    private Vector3 cachedPlayerPosition;
    private Rigidbody2D rb;
    private PlayerMovement playerMovement;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;

    public void Initialize()
    {
        rb = GetComponent<Rigidbody2D>();
        playerMovement = GetComponent<PlayerMovement>();
        originalMaxSpeed = playerMovement?.GetCurrentSpeed() ?? 5f;
    }

    public void OnInputClick(Vector2 mousePosition)
    {
        TryTugOfWarPull();
    }

    public void UpdateHookConstraints()
    {
        if (activeBitingHooks != null && activeBitingHooks.Count > 0) 
            CheckMaxHookDistanceState();
        if (activeBitingHooks != null && activeBitingHooks.Count <= 0) 
            ResetSpeed();

        ApplyExternalConstraints();
    }

    public void AddBitingHook(FishingProjectile hook)
    {
        if (!activeBitingHooks.Contains(hook))
        {
            activeBitingHooks.Add(hook);
            GetComponentInChildren<MouthMagnet>()?.RemoveEntity(hook);
            DebugLog($"Hook {hook.name} is now biting player. Total hooks: {activeBitingHooks.Count}");
        }
    }

    public void RemoveBitingHook(FishingProjectile hook)
    {
        if (activeBitingHooks.Contains(hook))
        {
            activeBitingHooks.Remove(hook);
            DebugLog($"Hook {hook.name} released player. Total hooks: {activeBitingHooks.Count}");
        }
    }

    private void TryTugOfWarPull()
    {
        cachedPlayerPosition = transform.position;

        if (activeBitingHooks.Count > 0)
        {
            foreach (FishingProjectile hook in activeBitingHooks)
            {
                if (hook.isBeingHeld)
                {
                    Vector3 distanceVector = cachedPlayerPosition - hook.spawnPoint.position;
                    float currentDistanceSqr = distanceVector.sqrMagnitude;
                    float hookMaxDistanceSqr = hook.maxDistance * hook.maxDistance;

                    Enemy enemy = hook.spawner?.GetComponent<Enemy>();
                    
                    if (currentDistanceSqr >= hookMaxDistanceSqr * (0.99f * 0.99f))
                    {
                        TryExtendFishingLine(hook);
                    }

                    if (currentDistanceSqr >= hookMaxDistanceSqr * (0.9f * 0.9f))
                    {
                        if (enemy != null)
                        {
                            enemy.TakeFatigue(GetComponent<Player>().PowerLevel);
                            DebugLog($"Player pulls against {enemy.name}'s fishing line - enemy suffers fatigue!");
                        }
                    }
                }
            }
        }
    }

    private void TryExtendFishingLine(FishingProjectile hook)
    {
        cachedPlayerPosition = transform.position;
        if (hook.spawner == null) return;

        HookSpawner hookSpawner = hook.spawner;
        float currentLineLength = hookSpawner.GetLineLength();

        if (currentLineLength < maxLineExtension)
        {
            float newLineLength = Mathf.Min(currentLineLength + lineExtensionAmount, maxLineExtension);
            hookSpawner.SetLineLength(newLineLength);

            DebugLog($"Player extended fishing line from {currentLineLength:F1} to {newLineLength:F1}");

            Vector3 directionToHook = (hook.spawnPoint.position - cachedPlayerPosition).normalized;
            rb.AddForce(-directionToHook * 2f, ForceMode2D.Impulse);
        }
        else
        {
            DebugLog("Fishing line is already at maximum length!");
        }
    }

    private void CheckMaxHookDistanceState()
    {
        cachedPlayerPosition = transform.position;

        bool wasAtMaxDistance = isAtMaxHookDistance;
        isAtMaxHookDistance = false;

        if (activeBitingHooks.Count > 0)
        {
            foreach (FishingProjectile hook in activeBitingHooks)
            {
                if (hook.isBeingHeld)
                {
                    Vector3 distanceVector = cachedPlayerPosition - hook.spawnPoint.position;
                    float currentDistanceSqr = distanceVector.sqrMagnitude;
                    float hookMaxDistanceSqr = hook.maxDistance * hook.maxDistance;
                    float thresholdSqr = hookMaxDistanceSqr * (0.95f * 0.95f);

                    if (currentDistanceSqr >= thresholdSqr)
                    {
                        isAtMaxHookDistance = true;
                        break;
                    }
                }
            }
        }

        if (isAtMaxHookDistance != wasAtMaxDistance)
        {
            if (isAtMaxHookDistance)
            {
                playerMovement?.SetSpeedMultiplier(lineExtensionSpeedPenalty);
                DebugLog($"Player at max hook distance - speed reduced");
            }
            else
            {
                playerMovement?.ResetSpeedMultiplier();
                DebugLog($"Player moved within hook range - speed restored");
            }
        }
    }

    private void ResetSpeed()
    {
        playerMovement?.ResetSpeedMultiplier();
    }

    public void SetPositionConstraint(Vector3 center, float radius, System.Action<Vector3> violationCallback = null)
    {
        isConstrainedByExternalForce = true;
        constraintCenter = center;
        constraintRadius = radius;
        onConstraintViolation = violationCallback;
        DebugLog($"Player constraint set: Center={center}, Radius={radius}");
    }

    public void RemovePositionConstraint()
    {
        isConstrainedByExternalForce = false;
        onConstraintViolation = null;
        DebugLog("Player constraint removed");
    }

    private void ApplyExternalConstraints()
    {
        cachedPlayerPosition = transform.position;

        if (!isConstrainedByExternalForce) return;

        float currentDistance = Vector3.Distance(cachedPlayerPosition, constraintCenter);

        if (currentDistance > constraintRadius)
        {
            Vector3 direction = (cachedPlayerPosition - constraintCenter).normalized;
            Vector3 constrainedPosition = constraintCenter + direction * constraintRadius;
            transform.position = constrainedPosition;

            Vector2 currentVelocity = rb.velocity;
            Vector2 radialDirection = direction;
            Vector2 tangentDirection = new Vector2(-radialDirection.y, radialDirection.x);

            float tangentVelocity = Vector2.Dot(currentVelocity, tangentDirection);
            rb.velocity = tangentDirection * tangentVelocity;

            onConstraintViolation?.Invoke(constrainedPosition);

            DebugLog($"Player constrained to radius {constraintRadius} at position {constrainedPosition}");
        }
    }

    private void DebugLog(string message)
    {
        if (enableDebugLogs) GameLogger.LogVerbose($"[PlayerHooks] {message}");
    }
}
