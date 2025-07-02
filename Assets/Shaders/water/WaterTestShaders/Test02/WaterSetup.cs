using UnityEngine;

public class WaterSetup : MonoBehaviour
{
    [Tooltip("The material used by water objects")]
    public Material waterMaterial;
    
    [Tooltip("Layer to be used for water objects")]
    public LayerMask waterLayer;
    
    [Tooltip("Reference to the underwater effect")]
    public UnderwaterEffectFeature underwaterEffect;
    
    void Start()
    {
        // Make sure the water object is on the water layer
        gameObject.layer = (int)Mathf.Log(waterLayer.value, 2);
        
        // Assign material
        if (waterMaterial != null)
        GetComponent<MeshRenderer>().material = waterMaterial;
        
        // Setup the underwater effect
        if (underwaterEffect != null)
        {
            underwaterEffect.settings.waterLayer = waterLayer;
        }
    }
}