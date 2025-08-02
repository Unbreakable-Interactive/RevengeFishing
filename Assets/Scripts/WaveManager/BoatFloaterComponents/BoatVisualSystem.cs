using UnityEngine;

public class BoatVisualSystem : MonoBehaviour
{
    [Header("Visual Settings")]
    [SerializeField] private SpriteRenderer boatSpriteRenderer;
    [SerializeField] private bool useSpriteFlip = true;
    [SerializeField] private bool adaptToScaleDirection = true;
    
    private float currentDirectionMultiplier = 1f;
    
    public void Initialize()
    {
        UpdateDirectionMultiplier();
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