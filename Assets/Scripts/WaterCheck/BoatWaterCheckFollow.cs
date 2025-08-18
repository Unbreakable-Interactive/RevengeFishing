using UnityEngine;

public class BoatWaterCheckFollow : MonoBehaviour
{
    [Header("Base WaterCheckFollow Settings")]
    public float waterSurfaceY = 5.92f;
    public GameObject target;
    
    [Header("BOAT SAFETY EXTENSIONS")]
    [SerializeField] private float minDistanceFromBoat = 1.0f;
    [SerializeField] private bool enableBoatSafetyCheck = true;
    
    private Transform boatTransform;
    private BoatFloater associatedBoat;

    void Start()
    {
        GetComponent<Collider2D>().isTrigger = true;
        FindAssociatedBoat();
        
        GameLogger.LogVerbose($"BoatWaterCheckFollow: Initialized with boat safety for {gameObject.name}");
    }

    void Update()
    {
        if (target == null) return;
        
        if (enableBoatSafetyCheck && ShouldApplyBoatSafety())
        {
            UpdateWithBoatSafety();
        }
        else
        {
            transform.position = new Vector3(target.transform.position.x, waterSurfaceY, transform.position.z);
        }
    }
    
    private void FindAssociatedBoat()
    {
        Transform current = transform.parent;
        while (current != null)
        {
            BoatFloater boat = current.GetComponent<BoatFloater>();
            if (boat != null)
            {
                associatedBoat = boat;
                boatTransform = boat.transform;
                break;
            }
            current = current.parent;
        }
        
        if (associatedBoat == null && transform.parent != null)
        {
            BoatFloater boat = transform.parent.GetComponentInChildren<BoatFloater>();
            if (boat != null)
            {
                associatedBoat = boat;
                boatTransform = boat.transform;
            }
        }
        
        if (associatedBoat == null)
        {
            BoatFloater[] allBoats = FindObjectsOfType<BoatFloater>();
            foreach (BoatFloater boat in allBoats)
            {
                float distance = Vector3.Distance(transform.position, boat.transform.position);
                if (distance < 20f)
                {
                    associatedBoat = boat;
                    boatTransform = boat.transform;
                    break;
                }
            }
        }
        
        string status = associatedBoat != null ? $"Found: {associatedBoat.name}" : "Not found";
        GameLogger.LogVerbose($"Associated boat: {status}");
    }
    
    private bool ShouldApplyBoatSafety()
    {
        return associatedBoat != null && boatTransform != null;
    }
    
    private void UpdateWithBoatSafety()
    {
        if (target == null) return;
        
        float boatY = boatTransform.position.y;
        float safeWaterlineY = boatY - minDistanceFromBoat;
        float targetY = Mathf.Min(waterSurfaceY, safeWaterlineY);
        
        transform.position = new Vector3(target.transform.position.x, targetY, transform.position.z);
        
        GameLogger.LogVerbose($"🚤 Boat Safety: Boat Y={boatY:F2}, Safe Y={safeWaterlineY:F2}, Using Y={targetY:F2}");
    }
    
    public void SetMinimumDistanceFromBoat(float distance)
    {
        minDistanceFromBoat = distance;
    }
    
    public void SetBoatSafetyEnabled(bool enabled)
    {
        enableBoatSafetyCheck = enabled;
    }
    
    public bool IsPositionSafe()
    {
        if (!ShouldApplyBoatSafety()) return true;
        
        float currentDistance = boatTransform.position.y - transform.position.y;
        return currentDistance >= minDistanceFromBoat;
    }
    
    #if UNITY_EDITOR
    [ContextMenu("TEST: Check Current Safety")]
    private void TestCurrentSafety()
    {
        bool isSafe = IsPositionSafe();
        GameLogger.LogVerbose($"Current Safety Status: {(isSafe ? "SAFE" : "UNSAFE")}");
        
        if (ShouldApplyBoatSafety())
        {
            float distance = boatTransform.position.y - transform.position.y;
            GameLogger.LogVerbose($"Current distance from boat: {distance:F2} (minimum: {minDistanceFromBoat:F2})");
        }
    }
    
    [ContextMenu("TEST: Force Find Boat")]
    private void TestFindBoat()
    {
        FindAssociatedBoat();
        TestCurrentSafety();
    }
    #endif
}
