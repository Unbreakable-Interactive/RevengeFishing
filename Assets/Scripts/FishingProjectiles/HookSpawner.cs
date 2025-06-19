using UnityEngine;

public class HookSpawner : MonoBehaviour
{
    [Header("Hook Settings")]
    public GameObject hookHandlerPrefab; // Changed from hookPrefab to hookHandlerPrefab
    public Transform spawnPoint;
    public float throwForce = 8f;

    [Header("Throw Direction")]
    public Vector2 throwDirection = new Vector2(1f, 0.2f);

    [Header("Distance Control")]
    public float hookMaxDistance = 15f;

    // Store the original max distance
    private float originalMaxDistance;

    private GameObject currentHookHandler; // Changed from currentHook to currentHookHandler
    private FishingProjectile currentHook; // Reference to the actual hook component

    private void Awake()
    {
        // Store the original max distance on awake
        originalMaxDistance = hookMaxDistance;
    }

    public bool CanThrowHook()
    {
        return hookHandlerPrefab != null &&
               spawnPoint != null &&
               currentHook == null;
    }

    public void ThrowHook()
    {
        if (!CanThrowHook()) return;

        // Reset to original max distance before throwing
        hookMaxDistance = originalMaxDistance;

        // Instantiate the hook handler (which contains both hook and waterline)
        currentHookHandler = Instantiate(hookHandlerPrefab, spawnPoint.position, spawnPoint.rotation);

        // Find the actual fishing hook within the handler
        currentHook = currentHookHandler.GetComponentInChildren<FishingProjectile>();

        if (currentHook != null)
        {
            currentHook.maxDistance = hookMaxDistance;
            currentHook.SetSpawner(this);
            currentHook.ThrowProjectile(throwDirection, throwForce);

            // Setup water detection
            SetupWaterDetection();

            Debug.Log($"Hook handler thrown by {gameObject.name} with water detection!");
        }
        else
        {
            Debug.LogError($"No FishingProjectile found in {hookHandlerPrefab.name}!");
            Destroy(currentHookHandler);
            currentHookHandler = null;
        }
    }

    private void SetupWaterDetection()
    {
        // Find the WaterLine component in the handler
        WaterCheck waterCheck = currentHookHandler.GetComponentInChildren<WaterCheck>();

        if (waterCheck != null && currentHook != null)
        {
            // Configure the water check to monitor our hook
            waterCheck.targetCollider = currentHook.GetComponent<Collider2D>();

            // If the hook has EntityMovement, connect it
            EntityMovement hookMovement = currentHook.GetComponent<EntityMovement>();
            if (hookMovement != null)
            {
                waterCheck.entityMovement = hookMovement;
            }

            Debug.Log("Water detection configured for fishing hook!");
        }
        else
        {
            Debug.LogWarning("WaterCheck component not found in hook handler!");
        }
    }

    public void ThrowProjectile()
    {
        if (currentHook != null)
        {
            currentHook.ThrowProjectile(throwDirection, throwForce);
        }
    }

    public void RetractHook(float retractionAmount)
    {
        if (currentHook != null)
        {
            float newLength = GetLineLength() - retractionAmount;

            if (newLength > 0.05f)
            {
                SetLineLength(newLength);

                // MOVE HOOK TOWARD SPAWN POINT
                Vector3 spawnPosition = currentHook.spawnPoint; // Access spawn point
                Vector3 currentPosition = currentHook.transform.position;
                Vector3 direction = (spawnPosition - currentPosition).normalized;

                // Move hook slightly toward spawn point for visual effect
                currentHook.transform.position += direction * retractionAmount * 0.5f;

                Debug.Log($"Hook being retracted gradually - remaining length: {newLength:F1}");
            }
            else
            {
                // When line is very short, start destruction
                currentHook.RetractProjectile();
                Debug.Log($"Hook retraction complete - destroying hook");
            }
        }
    }

    // Simple method to change line length
    public void SetLineLength(float newLength)
    {
        hookMaxDistance = newLength;

        if (currentHook != null)
        {
            currentHook.maxDistance = newLength;
        }
    }

    public float GetLineLength()
    {
        if (currentHook != null)
        {
            return currentHook.maxDistance;
        }

        return 0;
    }

    public bool HasActiveHook()
    {
        return currentHookHandler != null;
    }

    public void OnHookDestroyed()
    {
        if (currentHookHandler != null)
        {
            Destroy(currentHookHandler);
            currentHookHandler = null;
        }
        currentHook = null;

        // Reset max distance when hook is destroyed
        hookMaxDistance = originalMaxDistance;

        Debug.Log($"Hook handler and references cleared for {gameObject.name}");
    }

}
