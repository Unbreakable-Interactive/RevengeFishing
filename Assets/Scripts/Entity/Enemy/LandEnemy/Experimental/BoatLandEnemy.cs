using UnityEngine;

public class BoatLandEnemy : LandEnemy, IBoatComponent
{
    [Header("Boat Crew Components - AUTO ASSIGNED")]
    [SerializeField] private BoatCrewPhysics crewPhysics;
    [SerializeField] private BoatCrewPlatformTracker platformTracker;
    [SerializeField] private BoatCrewAI crewAI;
    [SerializeField] private BoatCrewFisherman crewFisherman;

    public FishermanConfig fishermanConfig; 
    public bool trackingPlatformMovement => platformTracker?.IsTrackingMovement ?? false;
    
    private bool boatSystemsInitialized = false; // NUEVA: Prevenir doble inicialización
    
    public override string GetBoatID() => GetBoatIDFromParent();
    public override void SetBoatID(BoatID newBoatID) 
    { 
        if (boatID != null)
            boatID = newBoatID;
    }
    
    private string GetBoatIDFromParent()
    {
        if (assignedPlatform is BoatPlatform boatPlatform)
            return boatPlatform.GetBoatID();
            
        BoatCrewManager crewManager = GetComponentInParent<BoatCrewManager>();
        if (crewManager != null)
            return crewManager.GetBoatID();
            
        return "NO_BOAT_ID";
    }

    // ELIMINADO: No sobrescribir Start() para evitar doble inicialización
    // protected override void Start() 
    
    private void SetBoatKinematicMode()
    {
        if (rb != null && _state == EnemyState.Alive)
        {
            rb.isKinematic = true;
            rb.angularVelocity = 0f;
            GameLogger.LogError($"[BOAT CREW] {gameObject.name} - Set to KINEMATIC mode while alive");
        }
    }
    
    private void InitializeBoatCrewSystems()
    {
        // NUEVO: Prevenir múltiples inicializaciones
        if (boatSystemsInitialized)
        {
            GameLogger.LogError($"[BOAT CREW] {gameObject.name} - Systems already initialized, skipping");
            return;
        }
        
        if (crewPhysics == null) crewPhysics = GetComponent<BoatCrewPhysics>();
        if (platformTracker == null) platformTracker = GetComponent<BoatCrewPlatformTracker>();
        if (crewAI == null) crewAI = GetComponent<BoatCrewAI>();
        if (crewFisherman == null) crewFisherman = GetComponent<BoatCrewFisherman>();
        
        if (crewPhysics != null)
            crewPhysics.Initialize(rb, this, platformTracker);
            
        if (platformTracker != null)
            platformTracker.Initialize(this, crewPhysics);
            
        if (crewAI != null)
            crewAI.Initialize(this, platformTracker, crewPhysics);
            
        if (crewFisherman != null)
            crewFisherman.Initialize(this, hookSpawner, fishermanConfig);
        
        boatSystemsInitialized = true; // MARCAR COMO INICIALIZADO
        GameLogger.LogError($"[BOAT CREW] {gameObject.name} - All crew systems initialized ONCE");
    }

    protected override void Update()
    {
        base.Update();
        
        if (_state == EnemyState.Defeated)
        {
            if (crewFisherman != null)
            {
                crewFisherman.CleanupFishingTools();
            }
            return;
        }
        
        if (crewFisherman != null)
        {
            crewFisherman.HandleActiveHook();
        }
    }

    private void FixedUpdate()
    {
        if (rb == null) return;
        
        if (_state == EnemyState.Alive)
        {
            if (crewPhysics != null)
            {
                crewPhysics.UpdateGravityTransition();
                crewPhysics.CheckGroundStatusOptimized();
                crewPhysics.ApplyPhysicsMovement();
            }
            
            if (platformTracker != null)
            {
                platformTracker.UpdatePlatformTracking();
            }

            if (!platformBoundsCalculated && assignedPlatform != null)
            {
                CalculatePlatformBounds();
            }

            if (crewAI != null && crewAI.ShouldMakeAIDecisions() && crewAI.IsTimeForNextAction())
            {
                crewAI.MakeAIDecision();
            }
        }
    }

    protected override void MakeAIDecision()
    {
        if (_state != EnemyState.Alive) return;
        
        if (crewAI != null)
        {
            crewAI.MakeAIDecision();
        }
        else
        {
            base.MakeAIDecision();
        }
    }
    
    public override void OnHookPlayerInteraction(bool isBeingHeld)
    {
        if (_state != EnemyState.Alive) return;
        
        base.OnHookPlayerInteraction(isBeingHeld);
        GameLogger.LogVerbose($"BoatLandEnemy {gameObject.name}: Hook interacted with player!");
    }
    
    public void SynchronizeWithBoatMovement(Vector3 boatDelta, BoatPlatform platform)
    {
        if (_state != EnemyState.Alive) return;
        
        if (platformTracker != null)
        {
            platformTracker.SynchronizeWithBoatMovement(boatDelta, platform);
        }
    }
    
    public void OnPlatformAssigned(Platform platform)
    {
        if (platformTracker != null)
        {
            platformTracker.OnPlatformAssigned(platform);
        }
        
        if (crewPhysics != null)
        {
            crewPhysics.RefreshPlatformColliderCache();
        }
    }

    public Vector2 GetTargetHorizontalVelocity()
    {
        if (_state != EnemyState.Alive) return Vector2.zero;
        
        if (crewAI != null)
            return crewAI.GetTargetHorizontalVelocity();
            
        switch (_landMovementState)
        {
            case LandMovementState.WalkLeft: return Vector2.left * walkingSpeed;
            case LandMovementState.WalkRight: return Vector2.right * walkingSpeed;
            case LandMovementState.RunLeft: return Vector2.left * runningSpeed;
            case LandMovementState.RunRight: return Vector2.right * runningSpeed;
            default: return Vector2.zero;
        }
    }
    
    public bool IsGrounded => crewPhysics?.IsGrounded ?? false;
    public bool IsFallingToWater => crewPhysics?.IsFallingToWater ?? false;
    public Vector2 SimulatedVelocity => crewPhysics?.SimulatedVelocity ?? Vector2.zero;
    
    public Platform GetAssignedPlatform() => assignedPlatform;

    public override void SetAssignedPlatform(Platform platform)
    {        
        base.SetAssignedPlatform(platform);
        
        if (platformTracker != null)
        {
            platformTracker.SetAssignedPlatform(platform);
        }
        
        if (crewPhysics != null)
        {
            crewPhysics.RefreshPlatformColliderCache();
        }
    }
    
    protected override void CheckPlatformBounds()
    {
        if (platformTracker != null && platformTracker.IsTrackingMovement && assignedPlatform is BoatPlatform)
        {
            platformTracker.CheckBoatPlatformBounds();
            return;
        }
        
        base.CheckPlatformBounds();
    }

    public override void SetMovementMode(bool aboveWater)
    {
        if (_state == EnemyState.Defeated)
        {
            GameLogger.LogError($"[BOAT CREW] {gameObject.name} DEFEATED - PROCESSING water mode change to {aboveWater}");
            base.SetMovementMode(aboveWater);
            return;
        }
        
        if (_state == EnemyState.Alive && isOnBoat && assignedPlatform is BoatPlatform)
        {
            GameLogger.LogError($"[BOAT CREW] {gameObject.name} ON BOAT - IGNORING water mode change to {aboveWater}");
            
            isAboveWater = aboveWater;
            
            if (crewPhysics != null)
            {
                crewPhysics.HandleMovementModeChange(aboveWater);
            }
            
            return;
        }
        
        GameLogger.LogError($"[BOAT CREW] {gameObject.name} PROCESSING water mode change to {aboveWater}, state: {_state}");
        base.SetMovementMode(aboveWater);
        
        if (crewPhysics != null)
        {
            crewPhysics.HandleMovementModeChange(aboveWater);
        }
    }

    public override void ChangeState_Alive()
    {
        base.ChangeState_Alive();
        
        SetBoatKinematicMode();
        
        if (crewPhysics != null)
        {
            crewPhysics.HandleAlive();
        }
        
        if (platformTracker != null)
        {
            platformTracker.Reset();
        }
    }
    
    protected override void TriggerDefeat()
    {
        GameLogger.LogError($"[BOAT CREW] {gameObject.name} DEFEATED - Switching to DYNAMIC physics");
        
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.simulated = true;
            rb.gravityScale = 1f;
            rb.freezeRotation = true;
            
            if (crewPhysics != null)
            {
                rb.velocity = crewPhysics.SimulatedVelocity;
            }
            
            GameLogger.LogError($"[BOAT CREW] {gameObject.name} - Now DYNAMIC for floating behavior");
        }
        
        if (crewFisherman != null)
        {
            crewFisherman.CleanupFishingTools();
        }
        
        if (crewPhysics != null)
        {
            crewPhysics.HandleDefeat();
        }
        
        if (platformTracker != null)
        {
            platformTracker.ForceStopSynchronization();
        }
        
        base.TriggerDefeat();
    }
    
    protected override void StartDefeatBehaviors()
    {
        if (crewFisherman != null)
        {
            crewFisherman.CleanupFishingTools();
        }
        
        base.StartDefeatBehaviors();
        
        if (platformTracker != null)
        {
            platformTracker.ForceStopSynchronization();
        }
    }

    protected override void CleanupFishingTools()
    {
        if (crewFisherman != null)
        {
            crewFisherman.CleanupFishingTools();
        }
        
        base.CleanupFishingTools();
    }

    public override void Initialize()
    {
        base.Initialize();
        
        // SOLO INICIALIZAR UNA VEZ
        if (!boatSystemsInitialized)
        {
            InitializeBoatCrewSystems();
            SetBoatKinematicMode();
            
            if (crewPhysics != null)
            {
                crewPhysics.ResetToOriginalState();
            }
            
            if (platformTracker != null)
            {
                platformTracker.Reset();
            }
        }
        
        GameLogger.LogError($"[BOAT CREW INIT] {gameObject.name} - Systems initialized safely");
    }

    public override void ReturnToPool()
    {
        if (isReturningToPool) return;
        isReturningToPool = true;
        
        // RESET flag para permitir reinicialización en próximo uso
        boatSystemsInitialized = false;
        
        BoatCrewManager crewManager = GetComponentInParent<BoatCrewManager>();
        if (crewManager != null && crewManager.BoatEnemyBelongToBoat(this))
        {
            crewManager.HandleCrewMemberDeath(this);
        }
        else
        {
            GameLogger.LogError($"BoatLandEnemy {gameObject.name} couldn't find its BoatCrewManager!");
            
            if (ParentContainer != null)
            {
                ParentContainer.SetActive(false);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }

    public void SetIndependentMovement(bool independent)
    {
        if (crewAI != null)
        {
            crewAI.SetIndependentMovement(independent);
        }
    }

    public override void WaterMovement()
    {
        if (_state == EnemyState.Alive)
        {
            return;
        }
        
        base.WaterMovement();
    }
}
