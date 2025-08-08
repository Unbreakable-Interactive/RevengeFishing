using UnityEngine;

public class BoatPart : MonoBehaviour
{
    [SerializeField] private float forceMultiplier = 3f;
    [SerializeField] private float torqueMultiplier = 2f;
    
    private Vector3 originalLocalPosition;
    private Quaternion originalLocalRotation;
    private bool hasAppliedForce = false;
    
    [SerializeField] private Rigidbody2D rb;
    
    private void Awake()
    {
        originalLocalPosition = transform.localPosition;
        originalLocalRotation = transform.localRotation;
        
        if(rb == null)
            rb = GetComponent<Rigidbody2D>();
    }
 
    public void ApplyInitialForces()
    {
        if (rb == null || hasAppliedForce) return;
        
        Vector2 dropForce = new Vector2(
            Random.Range(-0.5f, 0.5f),
            Random.Range(-0.3f, 0.1f)
        );
        
        rb.AddForce(dropForce * forceMultiplier, ForceMode2D.Impulse);
        
        float torque = Random.Range(-torqueMultiplier, torqueMultiplier);
        rb.AddTorque(torque, ForceMode2D.Impulse);
        
        hasAppliedForce = true;
        
        GameLogger.LogVerbose($"BoatPart {gameObject.name} - Applied gentle forces: {dropForce * forceMultiplier}");
    }
    
    public void ResetToOriginalPosition()
    {
        transform.localPosition = originalLocalPosition;
        transform.localRotation = originalLocalRotation;
        
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
        
        hasAppliedForce = false;
        
        GameLogger.LogVerbose($"BoatPart {gameObject.name} - Reset to original position");
    }
    
    [ContextMenu("Test Apply Forces")]
    public void TestApplyForces()
    {
        hasAppliedForce = false;
        ApplyInitialForces();
    }
    
    [ContextMenu("Test Reset Position")]
    public void TestResetPosition()
    {
        ResetToOriginalPosition();
    }
}
