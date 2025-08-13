using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoatVisualSystem : MonoBehaviour
{
    [Header("Visual Settings")]
    [SerializeField] private SpriteRenderer boatSpriteRenderer;
    [SerializeField] private bool useSpriteFlip = true;
    [SerializeField] private bool adaptToScaleDirection = true;

    [SerializeField] private List<GameObject> boatParts = new List<GameObject>();
    
    [Header("Performance Optimization")]
    [SerializeField] private float explosionDelayRange = 0.3f;
    [SerializeField] private int maxSimultaneousExplosions = 3;
    
    private float currentDirectionMultiplier = 1f;
    private BoatPart[] cachedBoatPartScripts;
    private bool boatPartsInitialized = false;
    
    public void Initialize()
    {
        CacheBoatPartScripts();
        UpdateDirectionMultiplier();
        DestroyEnemy(false);
    }
    
    private void CacheBoatPartScripts()
    {
        if (boatPartsInitialized) return;
        
        cachedBoatPartScripts = new BoatPart[boatParts.Count];
        
        for (int i = 0; i < boatParts.Count; i++)
        {
            if (boatParts[i] != null)
            {
                cachedBoatPartScripts[i] = boatParts[i].GetComponent<BoatPart>();
            }
        }
        
        boatPartsInitialized = true;
    }
    
    public void UpdateVisualDirection(float movementDirection)
    {
        if (useSpriteFlip && boatSpriteRenderer != null)
        {
            boatSpriteRenderer.flipX = movementDirection < 0;
        }
        
        UpdateDirectionMultiplier();
    }
    
    private void UpdateDirectionMultiplier()
    {
        if (useSpriteFlip && boatSpriteRenderer != null)
        {
            currentDirectionMultiplier = boatSpriteRenderer.flipX ? -1f : 1f;
        }
        else
        {
            currentDirectionMultiplier = transform.localScale.x >= 0 ? 1f : -1f;
        }
    }

    public void DestroyEnemy(bool isDestroyed)
    {
        if (boatSpriteRenderer != null)
        {
            boatSpriteRenderer.gameObject.SetActive(!isDestroyed);
        }

        if (!boatPartsInitialized)
        {
            CacheBoatPartScripts();
        }

        if (isDestroyed)
        {
            StartCoroutine(BatchExplosionSequence());
        }
        else
        {
            BatchResetBoatParts();
        }
    
        GameLogger.LogVerbose($"BoatVisualSystem: Boat destruction state set to {isDestroyed}");
    }
    
    private IEnumerator BatchExplosionSequence()
    {
        List<int> remainingParts = new List<int>();
        
        for (int i = 0; i < boatParts.Count; i++)
        {
            if (boatParts[i] != null)
            {
                boatParts[i].SetActive(true);
                remainingParts.Add(i);
            }
        }
        
        while (remainingParts.Count > 0)
        {
            int batchSize = Mathf.Min(maxSimultaneousExplosions, remainingParts.Count);
            
            for (int i = 0; i < batchSize; i++)
            {
                int randomIndex = Random.Range(0, remainingParts.Count);
                int partIndex = remainingParts[randomIndex];
                
                if (cachedBoatPartScripts[partIndex] != null)
                {
                    cachedBoatPartScripts[partIndex].ApplyInitialForces();
                }
                
                remainingParts.RemoveAt(randomIndex);
            }
            
            if (remainingParts.Count > 0)
            {
                yield return new WaitForSeconds(Random.Range(0.1f, explosionDelayRange));
            }
        }
    }
    
    private void BatchResetBoatParts()
    {
        for (int i = 0; i < boatParts.Count; i++)
        {
            if (boatParts[i] != null)
            {
                boatParts[i].SetActive(false);
                
                if (cachedBoatPartScripts[i] != null)
                {
                    cachedBoatPartScripts[i].ResetToOriginalPosition();
                }
            }
        }
    }

    public float GetCurrentDirectionMultiplier()
    {
        if (adaptToScaleDirection)
        {
            UpdateDirectionMultiplier();
        }
        return currentDirectionMultiplier;
    }
    
    public void SetBoatSpriteRenderer(SpriteRenderer renderer)
    {
        boatSpriteRenderer = renderer;
    }
}
