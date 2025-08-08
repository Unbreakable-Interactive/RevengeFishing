using System.Collections.Generic;
using UnityEngine;

public class BoatVisualSystem : MonoBehaviour
{
    [Header("Visual Settings")]
    [SerializeField] private SpriteRenderer boatSpriteRenderer;
    [SerializeField] private bool useSpriteFlip = true;
    [SerializeField] private bool adaptToScaleDirection = true;

    [SerializeField] private List<GameObject> boatParts = new List<GameObject>();
    
    private float currentDirectionMultiplier = 1f;
    
    public void Initialize()
    {
        UpdateDirectionMultiplier();
        DestroyEnemy(false);
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

        foreach (GameObject part in boatParts)
        {
            if (part != null)
            {
                part.SetActive(isDestroyed);
                
                if (isDestroyed)
                {
                    BoatPart boatPartScript = part.GetComponent<BoatPart>();
                    if (boatPartScript != null)
                    {
                        boatPartScript.ApplyInitialForces();
                    }
                }
                else
                {
                    // Si se está desactivando (reapareciendo barco), resetear posición
                    BoatPart boatPartScript = part.GetComponent<BoatPart>();
                    if (boatPartScript != null)
                    {
                        boatPartScript.ResetToOriginalPosition();
                    }
                }
            }
        }
        
        GameLogger.LogVerbose($"BoatVisualSystem: Boat destruction state set to {isDestroyed}");
    }

    #region Public Methods

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

    #endregion
}