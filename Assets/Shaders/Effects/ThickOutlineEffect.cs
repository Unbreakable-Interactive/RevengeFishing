using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class ThickOutlineEffect : MonoBehaviour
{
    [Range(1, 20)]
    public int outlineSize = 5;
    public Color outlineColor = Color.white;
    
    private SpriteRenderer spriteRenderer;
    private GameObject[] outlineObjects;
    private SpriteRenderer[] outlineRenderers;
    
    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        CreateOutlineRenderers();
    }
    
    void Update()
    {
        UpdateOutlineRenderers();
    }
    
    void CreateOutlineRenderers()
    {
        if (outlineObjects != null)
        {
            foreach (var obj in outlineObjects)
            {
                if (obj != null) DestroyImmediate(obj);
            }
        }
        
        outlineObjects = new GameObject[outlineSize];
        outlineRenderers = new SpriteRenderer[outlineSize];
        
        for (int i = 0; i < outlineSize; i++)
        {
            outlineObjects[i] = new GameObject($"Outline_{i}");
            outlineObjects[i].transform.SetParent(transform);
            outlineObjects[i].transform.localPosition = Vector3.zero;
            outlineObjects[i].transform.localRotation = Quaternion.identity;
            outlineObjects[i].transform.localScale = Vector3.one;
            
            outlineRenderers[i] = outlineObjects[i].AddComponent<SpriteRenderer>();
            outlineRenderers[i].sprite = spriteRenderer.sprite;
            outlineRenderers[i].color = outlineColor;
            
            // Ensure outline renders behind the main sprite
            outlineRenderers[i].sortingLayerID = spriteRenderer.sortingLayerID;
            outlineRenderers[i].sortingOrder = spriteRenderer.sortingOrder - 1;
        }
    }
    
    void UpdateOutlineRenderers()
    {
        if (outlineRenderers == null) return;
        
        // Update outline size, position, and color
        for (int i = 0; i < outlineSize; i++)
        {
            if (outlineRenderers[i] == null) continue;
            
            // Update sprite if the main sprite changed
            outlineRenderers[i].sprite = spriteRenderer.sprite;
            outlineRenderers[i].color = outlineColor;
            
            // Calculate outline position based on index and direction
            float angle = (i / (float)outlineSize) * 2 * Mathf.PI;
            float x = Mathf.Cos(angle) * (i % 3 + 1) * 0.05f;
            float y = Mathf.Sin(angle) * (i % 3 + 1) * 0.05f;
            
            outlineObjects[i].transform.localPosition = new Vector3(x, y, 0);
        }
    }
    
    void OnValidate()
    {
        if (Application.isPlaying && spriteRenderer != null)
        {
            // Recreate outline renderers when inspector values change
            CreateOutlineRenderers();
        }
    }
}
