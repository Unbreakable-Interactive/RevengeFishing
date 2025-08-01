using UnityEngine;

public class BoatPlatformOccupant : MonoBehaviour
{
    [Header("Platform Detection")]
    [SerializeField] private bool debugMode = true;
    
    private BoatPlatform currentBoatPlatform;
    private bool isOnBoatPlatform = false;
    
    private void Start()
    {
        // Automáticamente detectar si está en un bote
        BoatPlatform platform = GetComponentInParent<BoatPlatform>();
        if (platform != null)
        {
            EnterBoatPlatform(platform);
        }
    }
    
    private void EnterBoatPlatform(BoatPlatform boatPlatform)
    {
        isOnBoatPlatform = true;
        currentBoatPlatform = boatPlatform;
        
        if (debugMode)
            Debug.Log($"{gameObject.name} entered boat platform: {boatPlatform.name}");
    }
    
    private void ExitBoatPlatform()
    {
        isOnBoatPlatform = false;
        currentBoatPlatform = null;
        
        if (debugMode)
            Debug.Log($"{gameObject.name} exited boat platform");
    }
    
    public bool IsOnBoatPlatform()
    {
        return isOnBoatPlatform;
    }
    
    public BoatPlatform GetCurrentBoatPlatform()
    {
        return currentBoatPlatform;
    }
}