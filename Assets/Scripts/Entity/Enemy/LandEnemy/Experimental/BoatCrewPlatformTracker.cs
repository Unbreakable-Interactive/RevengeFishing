using UnityEngine;

public class BoatCrewPlatformTracker : MonoBehaviour
{
    [Header("Platform Tracking")]
    [SerializeField] private float platformStickDistance = 0.2f;
    [SerializeField] private float platformCheckInterval = 0.08f;
    [SerializeField] private bool enableIndependentMovement = true;
    
    [Header("Synchronization")]
    [SerializeField] private float syncSmoothness = 15f; // NUEVA: Suavizado de sincronización
    [SerializeField] private bool prioritizeBoatMovement = true; // NUEVA: Priorizar movimiento del bote
    
    [Header("Platform Bounds Safety")]
    [SerializeField] private float boundsBuffer = 0.3f;
    [SerializeField] private float boundsStopThreshold = 0.1f;
    
    private BoatLandEnemy boatEnemy;
    private BoatCrewPhysics crewPhysics;
    
    private Vector3 lastPlatformPosition;
    private Vector3 lastValidPlatformPosition;
    private Vector3 targetPosition; // NUEVA: Posición objetivo para suavizado
    private float localXPositionOnPlatform;
    private float relativeYOffsetToPlatform = 0f;
    private bool trackingPlatformMovement = false;
    
    private Collider2D cachedPlatformCollider;
    private float lastPlatformCheckTime = 0f;
    private float platformStickDistanceSqr;
    
    public bool IsTrackingMovement => trackingPlatformMovement;
    
    public void Initialize(BoatLandEnemy enemy, BoatCrewPhysics physics)
    {
        boatEnemy = enemy;
        crewPhysics = physics;
        
        platformStickDistanceSqr = platformStickDistance * platformStickDistance;
        
        GameLogger.LogError($"[CREW PLATFORM TRACKER] {gameObject.name} - Platform tracking initialized");
    }
    
    public void UpdatePlatformTracking()
    {
        float currentTime = Time.fixedTime;
        
        if (currentTime - lastPlatformCheckTime >= platformCheckInterval)
        {
            CheckPlatformStickingOptimized();
            lastPlatformCheckTime = currentTime;
        }
        
        if (trackingPlatformMovement)
        {
            UpdatePlatformMovementSmooth(); // CAMBIADO: Usar versión suavizada
        }
    }
    
    private void CheckPlatformStickingOptimized()
    {
        if (boatEnemy?.GetAssignedPlatform() == null) 
        {
            if (trackingPlatformMovement)
            {
                StopTrackingPlatformMovement();
            }
            return;
        }

        if (cachedPlatformCollider == null)
        {
            RefreshPlatformColliderCache();
            if (cachedPlatformCollider == null) return;
        }

        float distance = transform.position.y - cachedPlatformCollider.bounds.max.y;

        if (trackingPlatformMovement)
        {
            if (distance > platformStickDistance * 4f)
            {
                StopTrackingPlatformMovement();
                if (crewPhysics != null)
                {
                    crewPhysics.SetStuckToPlatform(false);
                }
            }
        }
        else
        {
            if (distance <= platformStickDistance && distance >= -platformStickDistance)
            {
                StartTrackingPlatformMovement();
            }
        }
    }
    
    // NUEVA: Versión suavizada para mejor sincronización visual
    private void UpdatePlatformMovementSmooth()
    {
        if (boatEnemy?.GetAssignedPlatform() == null || cachedPlatformCollider == null) return;

        Vector3 currentPlatformPosition = cachedPlatformCollider.bounds.max;
        
        if (lastPlatformPosition != Vector3.zero)
        {
            Vector3 platformMovement = currentPlatformPosition - lastPlatformPosition;
            
            // CALCULAR POSICIÓN OBJETIVO
            Vector3 newTargetPosition = transform.position;
            
            if (prioritizeBoatMovement)
            {
                // PRIORIZAR MOVIMIENTO DEL BOTE - aplicar primero
                newTargetPosition += platformMovement;
                
                // LUEGO aplicar movimiento local del tripulante (reducido)
                Vector3 localMovement = GetLocalMovementOnPlatform() * 0.3f; // Reducido para evitar conflictos
                newTargetPosition.x += localMovement.x;
            }
            else
            {
                // MOVIMIENTO COMBINADO
                Vector3 localMovement = GetLocalMovementOnPlatform();
                newTargetPosition += platformMovement + localMovement;
            }
            
            // MANTENER SIEMPRE EN LA SUPERFICIE DE LA PLATAFORMA
            newTargetPosition.y = currentPlatformPosition.y + relativeYOffsetToPlatform;
            
            targetPosition = newTargetPosition;
            
            // APLICAR CON SUAVIZADO PARA EVITAR JITTERING
            Vector3 smoothedPosition = Vector3.Lerp(transform.position, targetPosition, syncSmoothness * Time.fixedDeltaTime);
            transform.position = smoothedPosition;
            
            lastValidPlatformPosition = smoothedPosition;
        }
        
        lastPlatformPosition = currentPlatformPosition;
    }
    
    private Vector3 GetLocalMovementOnPlatform()
    {
        if (boatEnemy == null) return Vector3.zero;
        
        Vector2 targetHorizontalVelocity = boatEnemy.GetTargetHorizontalVelocity();
        return (Vector3)targetHorizontalVelocity * Time.fixedDeltaTime;
    }
    
    public void StartTrackingPlatformMovement()
    {
        if (boatEnemy?.GetAssignedPlatform() == null) return;

        RefreshPlatformColliderCache();
        if (cachedPlatformCollider == null) return;

        if (crewPhysics != null)
        {
            crewPhysics.SetStuckToPlatform(true);
        }
        
        trackingPlatformMovement = true;

        Vector3 platformSurface = cachedPlatformCollider.bounds.max;
        
        if (boatEnemy.BodyCollider != null)
        {
            float enemyHeight = boatEnemy.BodyCollider.bounds.size.y;
            relativeYOffsetToPlatform = -(enemyHeight / 2f) + 0.25f;
        }
        else
        {
            relativeYOffsetToPlatform = -0.15f;
        }

        localXPositionOnPlatform = 0f;
        lastPlatformPosition = platformSurface;

        Vector3 adjustedPosition = transform.position;
        adjustedPosition.y = platformSurface.y + relativeYOffsetToPlatform;
        transform.position = adjustedPosition;
        targetPosition = adjustedPosition; // NUEVA: Inicializar target position
        lastValidPlatformPosition = adjustedPosition;

        GameLogger.LogError($"[CREW SYNC] {gameObject.name} - Started SMOOTH platform tracking with priority: {prioritizeBoatMovement}");
    }

    public void StopTrackingPlatformMovement()
    {
        if (crewPhysics != null)
        {
            crewPhysics.SetStuckToPlatform(false);
        }
        
        trackingPlatformMovement = false;
        lastPlatformPosition = Vector3.zero;
        localXPositionOnPlatform = 0f;
        relativeYOffsetToPlatform = 0f;
        
        GameLogger.LogVerbose($"BoatCrewPlatformTracker {gameObject.name}: Stopped tracking platform movement");
    }
    
    private void RefreshPlatformColliderCache()
    {
        if (boatEnemy?.GetAssignedPlatform() != null)
        {
            cachedPlatformCollider = boatEnemy.GetAssignedPlatform().PlatformCollider;
        }
    }
    
    public void CheckBoatPlatformBounds()
    {
        if (boatEnemy?.GetAssignedPlatform() == null || cachedPlatformCollider == null) return;
        if (boatEnemy.State != Enemy.EnemyState.Alive) return;

        Bounds platformBounds = cachedPlatformCollider.bounds;
        Vector3 currentPos = transform.position;

        float leftEdge = platformBounds.min.x + boundsBuffer;
        float rightEdge = platformBounds.max.x - boundsBuffer;

        bool atLeftEdge = currentPos.x <= leftEdge;
        bool atRightEdge = currentPos.x >= rightEdge;
        
        if (atLeftEdge || atRightEdge)
        {
            float clampedX = Mathf.Clamp(currentPos.x, leftEdge, rightEdge);
            Vector3 clampedPos = new Vector3(clampedX, currentPos.y, currentPos.z);
            transform.position = clampedPos;
            targetPosition = clampedPos; // NUEVA: Actualizar target position
            
            if (boatEnemy != null)
            {
                boatEnemy.MovementStateLand = LandEnemy.LandMovementState.Idle;
            }
        }
    }
    
    // MEJORADA: Sincronización directa con el bote
    public void SynchronizeWithBoatMovement(Vector3 boatDelta, BoatPlatform platform)
    {
        if (!trackingPlatformMovement || platform != boatEnemy?.GetAssignedPlatform()) return;
        
        // APLICAR MOVIMIENTO DIRECTO DEL BOTE SIN SUAVIZADO
        Vector3 currentPos = transform.position;
        Vector3 newPos = currentPos + boatDelta;
        
        if (cachedPlatformCollider != null)
        {
            newPos.y = cachedPlatformCollider.bounds.max.y + relativeYOffsetToPlatform;
        }
        
        transform.position = newPos;
        targetPosition = newPos; // Actualizar target para evitar conflictos
        
        GameLogger.LogVerbose($"[CREW SYNC] {gameObject.name} - Direct boat sync: {boatDelta}");
    }
    
    public void OnPlatformAssigned(Platform platform)
    {
        RefreshPlatformColliderCache();
        
        if (platform != null)
        {
            Invoke(nameof(DelayedPlatformCheck), 0.1f);
        }
        else
        {
            StopTrackingPlatformMovement();
        }
    }
    
    private void DelayedPlatformCheck()
    {
        CheckPlatformStickingOptimized();
    }
    
    public void SetAssignedPlatform(Platform platform)
    {
        RefreshPlatformColliderCache();
    }
    
    public void ForceStopSynchronization()
    {
        StopTrackingPlatformMovement();
    }
    
    public void Reset()
    {
        StopTrackingPlatformMovement();
        lastValidPlatformPosition = Vector3.zero;
        localXPositionOnPlatform = 0f;
        targetPosition = Vector3.zero; // NUEVA: Reset target position
    }
}
