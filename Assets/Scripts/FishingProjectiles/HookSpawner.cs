using UnityEngine;

public class HookSpawner : MonoBehaviour
{
    [Header("Hook Settings")]
    public GameObject hookHandlerPrefab;
    public Transform spawnPoint;
    public float throwForce = 8f;

    [Header("Throw Direction")]
    public Vector2 throwDirection = new Vector2(1f, 0.2f);

    [Header("Distance Control")]
    public float hookMaxDistance = 15f;

    private float originalMaxDistance;
    private GameObject currentHookHandler;
    public FishingProjectile currentHook;

    private void Awake()
    {
        originalMaxDistance = hookMaxDistance;
    }

    public void Initialize()
    {
        Debug.Log($"HookSpawner.Initialize() called on {gameObject.name}");
        
        // AUTO-SETUP MISSING REFERENCES
        if (hookHandlerPrefab == null)
        {
            // Try to find the prefab automatically
            hookHandlerPrefab = Resources.Load<GameObject>("FishingHookHandler");
            if (hookHandlerPrefab == null)
            {
                Debug.LogError($"HookSpawner on {gameObject.name}: hookHandlerPrefab is missing! Assign it in Inspector or place FishingHookHandler in Resources folder!");
                return;
            }
            else
            {
                Debug.Log($"HookSpawner on {gameObject.name}: Auto-found hookHandlerPrefab in Resources");
            }
        }
        
        if (spawnPoint == null)
        {
            // Auto-create spawn point
            GameObject spawnGO = new GameObject("AutoHookSpawnPoint");
            spawnGO.transform.SetParent(transform);
            spawnGO.transform.localPosition = new Vector3(1f, 0f, 0f); // In front of fisherman
            spawnPoint = spawnGO.transform;
            Debug.Log($"HookSpawner on {gameObject.name}: Auto-created spawn point");
        }
        
        Debug.Log($"HookSpawner initialized successfully on {gameObject.name}: CanThrow={CanThrowHook()}");
    }

    public bool CanThrowHook()
    {
        bool canThrow = hookHandlerPrefab != null && spawnPoint != null && currentHook == null;
        Debug.Log($"CanThrowHook() on {gameObject.name}: Prefab={hookHandlerPrefab != null}, SpawnPoint={spawnPoint != null}, NoHook={currentHook == null} -> Result={canThrow}");
        return canThrow;
    }

    public void ThrowHook()
    {
        Debug.Log($"ThrowHook() called on {gameObject.name}: CanThrow={CanThrowHook()}");
        
        if (!CanThrowHook()) 
        {
            Debug.LogError($"ThrowHook() FAILED on {gameObject.name}: CanThrow=false");
            return;
        }

        hookMaxDistance = originalMaxDistance;

        // Instantiate the hook handler
        currentHookHandler = Instantiate(hookHandlerPrefab, spawnPoint.position, spawnPoint.rotation);
        Debug.Log($"✅ Instantiated hook handler: {currentHookHandler.name} at {spawnPoint.position}");

        // Find the actual fishing hook within the handler
        currentHook = currentHookHandler.GetComponentInChildren<FishingProjectile>();

        if (currentHook != null)
        {
            currentHook.maxDistance = hookMaxDistance;
            currentHook.SetSpawner(this);
            currentHook.ThrowProjectile(throwDirection, throwForce);

            SetupWaterDetection();

            Debug.Log($"✅ Hook thrown successfully by {gameObject.name}!");
        }
        else
        {
            Debug.LogError($"❌ No FishingProjectile found in {hookHandlerPrefab.name}!");
            Destroy(currentHookHandler);
            currentHookHandler = null;
        }
    }

    private void SetupWaterDetection()
    {
        WaterCheck waterCheck = currentHookHandler.GetComponentInChildren<WaterCheck>();

        if (waterCheck != null && currentHook != null)
        {
            waterCheck.targetCollider = currentHook.GetComponent<Collider2D>();

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

                Vector3 spawnPosition = currentHook.spawnPoint;
                Vector3 currentPosition = currentHook.transform.position;
                Vector3 direction = (spawnPosition - currentPosition).normalized;

                currentHook.transform.position += direction * retractionAmount * 0.5f;

                Debug.Log($"Hook being retracted gradually - remaining length: {newLength:F1}");
            }
            else
            {
                currentHook.RetractProjectile();
                Debug.Log($"Hook retraction complete - destroying hook");
            }
        }
    }

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

        hookMaxDistance = originalMaxDistance;

        Debug.Log($"Hook handler and references cleared for {gameObject.name}");
    }
}
