using System.Collections;
using UnityEngine;

public enum BoatMovementState
{
    AutoMove,
    Driven,
    Destroyed
}

public class BoatMovementSystem : MonoBehaviour
{
    private const float DRIVEN_SPEED = 60;
    private const float AUTOMOVE_SPEED = 2;
    private const float DESTROYED_SPEED = 0;
    
    [Header("Movement Settings")]
    [SerializeField] private float movementSpeed = 2f;
    [SerializeField] private float maxMovementForce = 6f;
    [SerializeField] private bool enableAutomaticMovement = true;
    [SerializeField] private bool debugMovement = false;
    
    [Header("Performance Optimization")]
    [SerializeField] private float waterCheckInterval = 0.25f;
    [SerializeField] private float boundaryCheckInterval = 0.1f;
    
    [Header("Boundaries")]
    [SerializeField] private Transform leftBoundary;
    [SerializeField] private Transform rightBoundary;
    [SerializeField] private float boundaryBuffer = 1f;
    
    private Rigidbody2D rb;
    private BoatVisualSystem visualSystem;
    
    private bool isRegisteredToPlatform = false;
    private bool movementActive = false;
    private float currentMovementDirection = 1f;
    private Platform assignedPlatform;
    
    [SerializeField] BoatMovementState movementState = BoatMovementState.AutoMove;
    
    private BoatFloater cachedFloater;
    private WaterPhysics cachedWaterPhysics;
    private Transform[] cachedFloatPoints;
    
    private float lastWaterCheckTime = 0f;
    private float lastBoundaryCheckTime = 0f;
    private bool isInWaterCached = false;
    
    private Vector2 cachedPosition;
    private Vector2 cachedMoveForce;

    public void Initialize(Rigidbody2D rigidbody, BoatVisualSystem visual)
    {
        rb = rigidbody;
        visualSystem = visual;
        movementState = BoatMovementState.AutoMove;

        CacheComponents();
        SetupSpeedByState();
    }
    
    private void CacheComponents()
    {
        cachedFloater = GetComponent<BoatFloater>();
        cachedWaterPhysics = WaterPhysics.Instance;
        
        if (cachedFloater?.floatPoints != null)
        {
            cachedFloatPoints = cachedFloater.floatPoints;
        }
    }

    private void SetupSpeedByState()
    {
        movementSpeed = movementState == BoatMovementState.AutoMove ? AUTOMOVE_SPEED : DRIVEN_SPEED;
        maxMovementForce = movementState == BoatMovementState.AutoMove ? AUTOMOVE_SPEED : DRIVEN_SPEED;
    }

    public void DestroyState()
    {
        movementState = BoatMovementState.Destroyed;
        movementSpeed = DESTROYED_SPEED;
        maxMovementForce = DESTROYED_SPEED;
    
        movementActive = false;
    
        if (rb != null)
        {
            Vector2 sinkingForce = Vector2.down * 2f;
            rb.AddForce(sinkingForce, ForceMode2D.Impulse);
        
            rb.drag = 0.1f;
            rb.angularDrag = 0.1f;
        }
    
        GameLogger.LogVerbose("BoatMovement: Boat destroyed - movement stopped, sinking force applied");
    }
    
    public void InitializeBoundaries(Transform left, Transform right)
    {
        leftBoundary = left;
        rightBoundary = right;
    }
    
    public void UpdateMovement()
    {
        if (!enableAutomaticMovement || !movementActive || movementState == BoatMovementState.Destroyed) return;
        
        float currentTime = Time.time;
        
        if (currentTime - lastWaterCheckTime >= waterCheckInterval)
        {
            isInWaterCached = IsInWaterOptimized();
            lastWaterCheckTime = currentTime;
        }
        
        if (!isInWaterCached) return;
        
        if (currentTime - lastBoundaryCheckTime >= boundaryCheckInterval)
        {
            CheckBoundariesOptimized();
            lastBoundaryCheckTime = currentTime;
        }
        
        ApplyMovementForceOptimized();
    }
    
    private void CheckBoundariesOptimized()
    {
        cachedPosition = transform.position;
        
        bool nearLeftBoundary = leftBoundary != null && 
                               cachedPosition.x <= leftBoundary.position.x + boundaryBuffer;
        bool nearRightBoundary = rightBoundary != null && 
                                cachedPosition.x >= rightBoundary.position.x - boundaryBuffer;
        
        if (nearLeftBoundary && currentMovementDirection < 0)
        {
            currentMovementDirection = 1f;
            visualSystem?.UpdateVisualDirection(currentMovementDirection);
        }
        else if (nearRightBoundary && currentMovementDirection > 0)
        {
            currentMovementDirection = -1f;
            visualSystem?.UpdateVisualDirection(currentMovementDirection);
        }
    }
    
    private void ApplyMovementForceOptimized()
    {
        cachedMoveForce.x = currentMovementDirection * movementSpeed;
        cachedMoveForce.y = 0f;
        
        if (cachedMoveForce.sqrMagnitude > maxMovementForce * maxMovementForce)
        {
            cachedMoveForce = cachedMoveForce.normalized * maxMovementForce;
        }
        
        rb.AddForce(cachedMoveForce);
    }
    
    private bool IsInWaterOptimized()
    {
        if (cachedFloater == null || cachedFloatPoints == null || cachedWaterPhysics == null) return false;
        
        for (int i = 0; i < cachedFloatPoints.Length; i++)
        {
            if (cachedFloatPoints[i] != null)
            {
                Vector2 worldPos = cachedFloatPoints[i].position;
                float waterHeight = cachedWaterPhysics.GetWaterHeightAt(worldPos);
                if (waterHeight > worldPos.y) return true;
            }
        }
        return false;
    }
    
    public void OnRegisteredToPlatform(Platform platform)
    {
        if (isRegisteredToPlatform) return;
        
        isRegisteredToPlatform = true;
        assignedPlatform = platform;
        
        if (debugMovement)
        {
            GameLogger.LogVerbose($"BoatMovement: Registered to platform {platform.name}, starting movement!");
        }
        
        StartMovement();
    }
    
    public void StartMovement()
    {
        if (movementActive) return;
        
        movementActive = true;
        ChooseNewMovementDirection();
        
        lastWaterCheckTime = 0f;
        lastBoundaryCheckTime = 0f;
        
        if (debugMovement)
        {
            GameLogger.LogVerbose("BoatMovement: Movement started!");
        }
    }
    
    private void ChooseNewMovementDirection()
    {
        currentMovementDirection = Random.Range(0, 2) == 0 ? -1f : 1f;
        visualSystem?.UpdateVisualDirection(currentMovementDirection);
        
        if (debugMovement)
        {
            string direction = currentMovementDirection > 0 ? "RIGHT" : "LEFT";
            GameLogger.LogVerbose($"BoatMovement: New movement direction: {direction}");
        }
    }

    public void SetMovementState_Driven()
    {
        movementState = BoatMovementState.Driven;
        SetupSpeedByState();
    }

    public void SetMovementState_AutoMove()
    {
        movementState = BoatMovementState.AutoMove;
        SetupSpeedByState();
    }
    
    public void SetAutomaticMovementEnabled(bool enabled)
    {
        enableAutomaticMovement = enabled;
        
        if (enabled && !movementActive && isRegisteredToPlatform)
        {
            StartMovement();
        }
        else if (!enabled)
        {
            movementActive = false;
        }
    }
    
    public void ForceStartMovement()
    {
        StartMovement();
    }
    
    public void StopMovement()
    {
        movementActive = false;
        if (debugMovement)
        {
            GameLogger.LogVerbose("BoatMovement: Movement stopped manually");
        }
    }
    
    public bool IsMovementActive() => movementActive;
    public bool IsRegisteredToPlatform() => isRegisteredToPlatform;
    public float GetCurrentMovementDirection() => currentMovementDirection;
    public Platform GetAssignedPlatform() => assignedPlatform;
}
