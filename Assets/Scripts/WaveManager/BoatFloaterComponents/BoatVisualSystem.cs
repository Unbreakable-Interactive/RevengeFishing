using System.Collections.Generic;
using UnityEngine;

public class BoatVisualSystem : MonoBehaviour
{
    [Header("Visual Settings")]
    [SerializeField] private SpriteRenderer boatSpriteRenderer;
    [SerializeField] private bool useSpriteFlip = true;
    [SerializeField] private bool adaptToScaleDirection = true;

    [SerializeField] private List<SpriteRenderer> boatPartsRenderer = new List<SpriteRenderer>();
    
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
        boatSpriteRenderer.gameObject.SetActive(!isDestroyed);

        foreach (SpriteRenderer renderer in boatPartsRenderer)
        {
            renderer.gameObject.SetActive(isDestroyed);
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