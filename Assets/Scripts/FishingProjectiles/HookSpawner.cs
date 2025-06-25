using UnityEngine;

public class HookSpawner : MonoBehaviour
{
    [Header("Hook Settings")]
    public GameObject hookHandlerPrefab; // reference to hookHandlerPrefab with water detection
    public Transform spawnPoint; //reference to the original spawn point at the tip of the fishing rod.
    public float throwForce = 8f;

    [Header("Distance Control")]
    public float hookMaxDistance = 8f;

    // Store the original max distance
    private float originalMaxDistance;

    private GameObject currentHookHandler; // Changed from currentHook to currentHookHandler
    private FishingProjectile currentHook; // Reference to the actual hook component

    public FishingProjectile CurrentHook => currentHook;
   

    private void Start()
    {
        // Store the original max distance on awake
        originalMaxDistance = hookMaxDistance;
    }

    private void Update()
    {
        //Check if spawn point has moved
    }

    public bool CanThrowHook() => hookHandlerPrefab != null && spawnPoint != null && currentHook == null;

    public void ThrowHook()
    {
        if (!CanThrowHook()) return;

        Vector2 throwDirection = new Vector2(
                UnityEngine.Random.Range(0.2f, 1f),
                UnityEngine.Random.Range(0.2f, 0.5f)
            );

        // Set to original max distance before throwing, plus some variety
        hookMaxDistance = originalMaxDistance + (UnityEngine.Random.Range(-2f, 2f));

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

            Debug.Log($"Hook handler thrown by {gameObject.name}!");
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
                currentHook.transform.position += direction * (retractionAmount * 0.5f);

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

    
    public bool HasActiveHook() => currentHookHandler != null;

    /// <summary>
    /// Destroys currentHookHandler and clean it's value
    /// Also clean currentHook and reset hookMaxDistance
    /// </summary>
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
