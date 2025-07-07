using UnityEngine;

public class BoatFloater : MonoBehaviour
{
    [Header("Float Points")]
    public Transform[] floatPoints = new Transform[3]; // Bow, Center, Center
    
    [Header("Settings")]
    public float buoyancyForce = 5f;       
    public float waterDrag = 0.98f;         
    public float angularDrag = 0.95f;       
    public float stabilityForce = 0.5f;    
    public float maxBuoyancyDepth = 1f;    
    
    private Rigidbody2D rb;
    private WaterPhysics waterPhysics;
    private float originalGravityScale;    
    
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        waterPhysics = WaterPhysics.Instance;
        
        
        originalGravityScale = rb.gravityScale;
        
        
        rb.gravityScale = 0.3f;  
        rb.drag = 0.2f;        
        rb.angularDrag = 0.3f;   
        
        if (floatPoints[0] == null) CreateFloatPoints();
    }
    
    void CreateFloatPoints()
    {
        Bounds bounds = GetComponent<Collider2D>().bounds;
        
        for (int i = 0; i < 3; i++)
        {
            GameObject point = new GameObject($"FloatPoint_{i}");
            point.transform.SetParent(transform);
            floatPoints[i] = point.transform;
        }
        
        floatPoints[0].localPosition = new Vector3(-bounds.size.x * 0.4f, -bounds.size.y * 0.1f, 0); // Bow
        floatPoints[1].localPosition = new Vector3(0, -bounds.size.y * 0.1f, 0);                     // Center
        floatPoints[2].localPosition = new Vector3(bounds.size.x * 0.4f, -bounds.size.y * 0.1f, 0);  // Stern
    }
    
    void FixedUpdate()
    {
        if (waterPhysics == null) return;
        
        ApplyBuoyancy();
        ApplyWaterResistance();
        ApplyStability();
    }
    
    void ApplyBuoyancy()
    {
        int submergedPoints = 0;
        
        foreach (Transform point in floatPoints)
        {
            if (point == null) continue;
            
            Vector2 worldPos = point.position;
            float waterHeight = waterPhysics.GetWaterHeightAt(worldPos);
            float submersion = waterHeight - worldPos.y;
            
            if (submersion > 0)
            {
                submergedPoints++;
                
                submersion = Mathf.Min(submersion, maxBuoyancyDepth);
                
                Vector2 buoyancyVector = Vector2.up * buoyancyForce * submersion;
                rb.AddForceAtPosition(buoyancyVector, worldPos);
            }
        }
        
        if (submergedPoints == 0)
        {
            rb.gravityScale = originalGravityScale;
        }
        else
        {
            rb.gravityScale = 0.1f;
        }
    }
    
    void ApplyWaterResistance()
    {
        int submergedCount = 0;
        foreach (Transform point in floatPoints)
        {
            if (point != null)
            {
                Vector2 worldPos = point.position;
                float waterHeight = waterPhysics.GetWaterHeightAt(worldPos);
                if (waterHeight > worldPos.y) submergedCount++;
            }
        }
        
        if (submergedCount > 0)
        {
            rb.velocity *= waterDrag;
            rb.angularVelocity *= angularDrag;
        }
    }
    
    void ApplyStability()
    {
        float targetRotation = 0f;
        float rotationDifference = Mathf.DeltaAngle(transform.eulerAngles.z, targetRotation);
        
        if (Mathf.Abs(rotationDifference) > 5f)
        {
            rb.AddTorque(-rotationDifference * stabilityForce * Time.fixedDeltaTime);
        }
    }
    
    void OnDrawGizmosSelected()
    {
        if (floatPoints != null)
        {
            Gizmos.color = Color.blue;
            foreach (Transform point in floatPoints)
            {
                if (point != null)
                {
                    Gizmos.DrawWireSphere(point.position, 0.1f);
                    
                    if (waterPhysics != null)
                    {
                        float waterHeight = waterPhysics.GetWaterHeightAt(point.position);
                        Gizmos.color = Color.cyan;
                        Gizmos.DrawWireSphere(new Vector3(point.position.x, waterHeight, point.position.z), 0.05f);
                    }
                }
            }
        }
    }
}
