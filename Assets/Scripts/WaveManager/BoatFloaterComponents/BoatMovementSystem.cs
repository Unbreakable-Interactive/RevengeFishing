using UnityEngine;

public enum BoatMovementState
{
    AutoMove,
    Driven,
    Destroyed
}

public class BoatMovementSystem : MonoBehaviour
{
    private const float DRIVEN_SPEED = 2.5f;
    private const float AUTOMOVE_SPEED = 1;
    private const float DESTROYED_SPEED = 0;
    private const float MIN_ACTIVE_SPEED = 2f;
    
    [Header("Movement Settings")]
    [SerializeField] private float movementSpeed = 1f;
    [SerializeField] private float maxMovementForce = 2.5f;
    [SerializeField] private bool enableAutomaticMovement = true;
    [SerializeField] private bool debugMovement = true;
    
    [Header("Boundaries")]
    [SerializeField] private Transform leftBoundary;
    [SerializeField] private Transform rightBoundary;
    [SerializeField] private float boundaryBuffer = 1f;
    
    private Rigidbody2D rb;
    private BoatVisualSystem visualSystem;
    private BoatPhysicsSystem physicsSystem;
    
    private bool isRegisteredToPlatform = false;
    private bool movementActive = false;
    private float currentMovementDirection = 1f;
    private Platform assignedPlatform;
    
    [SerializeField] BoatMovementState movementState = BoatMovementState.AutoMove;
    
    private float lastBoundaryCheckTime = 0f;
    private Vector2 cachedPosition;

    public void Initialize(Rigidbody2D rigidbody, BoatVisualSystem visual)
    {
        rb = rigidbody;
        visualSystem = visual;
        physicsSystem = GetComponent<BoatPhysicsSystem>();
        movementState = BoatMovementState.AutoMove;
        SetupSpeedByState();

        GameLogger.LogError($"[MOVEMENT INIT] {gameObject.name} - Movement system initialized - INDEPENDENT FROM CREW");
        
        StartMovement();
    }

    private void SetupSpeedByState()
    {
        movementSpeed = movementState == BoatMovementState.AutoMove ? AUTOMOVE_SPEED : DRIVEN_SPEED;
        maxMovementForce = movementState == BoatMovementState.AutoMove ? AUTOMOVE_SPEED : DRIVEN_SPEED;
        
        GameLogger.LogError($"[MOVEMENT SETUP] {gameObject.name} - Speed set to: {movementSpeed} for state: {movementState}");
    }

    public void DestroyState()
    {
        movementState = BoatMovementState.Destroyed;
        movementSpeed = DESTROYED_SPEED;
        maxMovementForce = DESTROYED_SPEED;
        movementActive = false;

        if (physicsSystem != null)
        {
            physicsSystem.StopKinematicMovement();
        }
    }
    
    public void InitializeBoundaries(Transform left, Transform right)
    {
        leftBoundary = left;
        rightBoundary = right;
        GameLogger.LogError($"[MOVEMENT BOUNDARIES] {gameObject.name} - Boundaries set: Left={left?.name}, Right={right?.name}");
    }
    
    public void UpdateMovement()
    {
        if (debugMovement)
        {
            GameLogger.LogError($"[MOVEMENT UPDATE START] {gameObject.name} - AutoMovement: {enableAutomaticMovement}, State: {movementState}, Active: {movementActive}");
        }
        
        // EL BOTE SE MUEVE SOLO BASADO EN SU PROPIO ESTADO, NO EN LOS TRIPULANTES
        if (!enableAutomaticMovement || movementState == BoatMovementState.Destroyed) 
        {
            if (debugMovement)
            {
                GameLogger.LogError($"[MOVEMENT BLOCKED] {gameObject.name} - AutoMovement: {enableAutomaticMovement}, State: {movementState}");
            }
            return;
        }
        
        if (!movementActive)
        {
            if (debugMovement)
            {
                GameLogger.LogError($"[MOVEMENT INACTIVE] {gameObject.name} - Movement not active, forcing start");
            }
            StartMovement();
            return;
        }
        
        CheckBoundaries();
        ApplyMovementForce();
        
        if (debugMovement)
        {
            GameLogger.LogError($"[MOVEMENT UPDATE END] {gameObject.name} - Direction: {currentMovementDirection}, Speed: {movementSpeed}, INDEPENDENT MOVEMENT");
        }
    }
    
    private void CheckBoundaries()
    {
        if (Time.time - lastBoundaryCheckTime < 0.1f) return;
        lastBoundaryCheckTime = Time.time;
        
        cachedPosition = transform.position;
        
        bool nearLeft = leftBoundary != null && cachedPosition.x <= leftBoundary.position.x + boundaryBuffer;
        bool nearRight = rightBoundary != null && cachedPosition.x >= rightBoundary.position.x - boundaryBuffer;
        
        if (nearLeft && currentMovementDirection < 0)
        {
            currentMovementDirection = 1f;
            visualSystem?.UpdateVisualDirection(currentMovementDirection);
            if (debugMovement)
            {
                GameLogger.LogError($"[MOVEMENT BOUNDARY] {gameObject.name} - Hit LEFT boundary, turning RIGHT");
            }
        }
        else if (nearRight && currentMovementDirection > 0)
        {
            currentMovementDirection = -1f;
            visualSystem?.UpdateVisualDirection(currentMovementDirection);
            if (debugMovement)
            {
                GameLogger.LogError($"[MOVEMENT BOUNDARY] {gameObject.name} - Hit RIGHT boundary, turning LEFT");
            }
        }
    }
    
    private void ApplyMovementForce()
    {
        if (physicsSystem == null) 
        {
            GameLogger.LogError($"[MOVEMENT ERROR] {gameObject.name} - Physics system is NULL!");
            return;
        }
        
        // GARANTIZAR QUE EL BOTE SIEMPRE SE MUEVA CUANDO ESTÉ ACTIVO
        float activeMovementSpeed = movementSpeed <= 0 ? MIN_ACTIVE_SPEED : movementSpeed;
        float targetForce = currentMovementDirection * activeMovementSpeed;
        
        GameLogger.LogError($"[MOVEMENT CALC] {gameObject.name} - Direction: {currentMovementDirection}, Speed: {movementSpeed}, Active Speed: {activeMovementSpeed}, Target Force: {targetForce}");
        
        physicsSystem.SetHorizontalForce(targetForce);
        
        if (debugMovement)
        {
            GameLogger.LogError($"[MOVEMENT FORCE] {gameObject.name} - Applied force: {targetForce} (Speed: {activeMovementSpeed}) BOAT FORCE");
        }
    }
    
    public void OnRegisteredToPlatform(Platform platform)
    {
        isRegisteredToPlatform = true;
        assignedPlatform = platform;
        
        GameLogger.LogError($"[MOVEMENT REGISTER] {gameObject.name} - Registered to platform {platform?.name}, starting INDEPENDENT movement!");
        
        StartMovement();
    }
    
    public void StartMovement()
    {
        movementActive = true;
        currentMovementDirection = Random.Range(0, 2) == 0 ? -1f : 1f;
        
        GameLogger.LogError($"[MOVEMENT START] {gameObject.name} - Movement ACTIVATED, direction: {currentMovementDirection} - BOAT MOVEMENT ONLY");
    }

    public void SetMovementState_Driven()
    {
        movementState = BoatMovementState.Driven;
        SetupSpeedByState();
        GameLogger.LogError($"[MOVEMENT STATE] {gameObject.name} - Set to DRIVEN, speed: {movementSpeed} - BOAT INDEPENDENT");
        
        if (!movementActive)
        {
            StartMovement();
        }
    }

    public void SetMovementState_AutoMove()
    {
        movementState = BoatMovementState.AutoMove;
        SetupSpeedByState();
        GameLogger.LogError($"[MOVEMENT STATE] {gameObject.name} - Set to AUTOMOVE, speed: {movementSpeed} - BOAT INDEPENDENT");
        
        if (!movementActive)
        {
            StartMovement();
        }
    }
    
    public void SetAutomaticMovementEnabled(bool enabled)
    {
        enableAutomaticMovement = enabled;
        GameLogger.LogError($"[MOVEMENT ENABLE] {gameObject.name} - Automatic movement: {enabled} - BOAT CONTROL ONLY");
        
        if (enabled && !movementActive)
        {
            StartMovement();
        }
        else if (!enabled)
        {
            movementActive = false;
            if (physicsSystem != null)
            {
                physicsSystem.SetHorizontalForce(0f);
            }
        }
    }
    
    public void ForceStartMovement() => StartMovement();
    
    public void StopMovement()
    {
        movementActive = false;
        GameLogger.LogError($"[MOVEMENT STOP] {gameObject.name} - Movement STOPPED by direct boat control");
        
        if (physicsSystem != null)
        {
            physicsSystem.SetHorizontalForce(0f);
        }
    }
    
    public bool IsMovementActive() => movementActive;
    public bool IsRegisteredToPlatform() => isRegisteredToPlatform;
    public float GetCurrentMovementDirection() => currentMovementDirection;
    public Platform GetAssignedPlatform() => assignedPlatform;
}
