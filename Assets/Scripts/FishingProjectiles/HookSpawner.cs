using UnityEngine;

public class HookSpawner : MonoBehaviour
{
    [Header("Hook Settings")]
    public GameObject hookPrefab;
    public Transform spawnPoint;
    public float throwForce = 8f;

    [Header("Throw Direction")]
    public Vector2 throwDirection = new Vector2(1f, 0.2f);

    [Header("Distance Control")]
    public float hookMaxDistance = 15f;

    private GameObject currentHook;

    public bool CanThrowHook()
    {
        return hookPrefab != null &&
               spawnPoint != null &&
               currentHook == null;
    }

    public void ThrowHook()
    {
        if (!CanThrowHook()) return;

        currentHook = Instantiate(hookPrefab, spawnPoint.position, spawnPoint.rotation);

        FishingProjectile fishingProjectile = currentHook.GetComponent<FishingProjectile>();
        if (fishingProjectile != null)
        {
            fishingProjectile.maxDistance = hookMaxDistance;
            fishingProjectile.SetSpawner(this);
            fishingProjectile.ThrowProjectile(throwDirection, throwForce);
        }

        Debug.Log($"Hook thrown by {gameObject.name}!");
    }

    public void RetractHook()
    {
        if (currentHook != null)
        {
            FishingProjectile fishingProjectile = currentHook.GetComponent<FishingProjectile>();
            if (fishingProjectile != null)
            {
                fishingProjectile.RetractProjectile();
            }

            currentHook = null;
            Debug.Log($"Hook retracted by {gameObject.name}!");
        }
    }

    // Simple method to change line length
    public void SetLineLength(float newLength)
    {
        hookMaxDistance = newLength;

        if (currentHook != null)
        {
            FishingProjectile fishingProjectile = currentHook.GetComponent<FishingProjectile>();
            if (fishingProjectile != null)
            {
                fishingProjectile.maxDistance = newLength;
            }
        }
    }

    public bool HasActiveHook()
    {
        return currentHook != null;
    }
}
