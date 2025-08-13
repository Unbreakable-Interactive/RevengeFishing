using System.Collections;
using UnityEngine;

public class BoatLandEnemy : LandEnemy, IBoatComponent
{
    [Header("Boat Identity")]
    [SerializeField] private bool debugBoatCrew = false;
    
    [Header("Boat Crew Systems")]
    [SerializeField] private BoatCrewPhysics crewPhysics;
    [SerializeField] public FishermanConfig fishermanConfig;
    
    [Header("Boat Specific Settings")]
    [SerializeField] private float boundaryBuffer = 0.3f;
    [SerializeField] private bool enableHookThrowing = true;
    
    [Header("Crew Behavior")]
    [SerializeField] private CrewRole crewRole = CrewRole.Sailor;
    [SerializeField] private bool isNavigating = false;
    
    private bool boatSystemsInitialized = false;
    private BoatPlatform boatPlatform;
    private Vector3 lastBoatPosition;
    private float lastHookAttemptTime = 0f;
    private bool hasFallenFromBoat = false;
    
    public enum CrewRole
    {
        Sailor,
        Navigator,
        Lookout
    }
    
    public override string GetBoatID() => boatID?.UniqueID ?? "NO_ID";
    public override void SetBoatID(BoatID newBoatID) => boatID = newBoatID;
    
    public CrewRole GetCrewRole() => crewRole;
    public void SetCrewRole(CrewRole role) => crewRole = role;
    
    public bool IsNavigating() => isNavigating;
    
    public void AssignToWheel()
    {
        isNavigating = true;
        crewRole = CrewRole.Navigator;
        _landMovementState = LandMovementState.Idle;
        fishingToolEquipped = false;
    }
    
    public void ReleaseFromWheel()
    {
        isNavigating = false;
        if (crewRole == CrewRole.Navigator)
        {
            crewRole = CrewRole.Sailor;
        }
    }

    public void SynchronizeWithBoatMovement(Vector3 deltaMovement, BoatPlatform platform)
    {
        if (rb == null || !isOnBoat || _state != EnemyState.Alive) return;
        
        if (deltaMovement.magnitude > 0.001f && rb.bodyType == RigidbodyType2D.Kinematic)
        {
            Vector3 newPosition = transform.position + deltaMovement;
            rb.MovePosition(newPosition);
            
            if (debugBoatCrew)
                GameLogger.LogVerbose($"[BOAT SYNC] {gameObject.name} - Synced with delta: {deltaMovement}");
        }
    }
    
    private void InitializeBoatCrewSystems()
    {
        if (boatSystemsInitialized) return;
        
        if (crewPhysics == null) crewPhysics = GetComponent<BoatCrewPhysics>();
        
        if (crewPhysics != null)
            crewPhysics.Initialize(rb, this);
            
        boatSystemsInitialized = true;
    }

    protected override void Update()
    {
        base.Update();
        
        if (_state == EnemyState.Alive)
        {
            if (enableHookThrowing)
            {
                AttemptHookThrowing();
            }
        }
        else if (_state == EnemyState.Defeated && isOnBoat && !hasFallenFromBoat)
        {
            HandleFallFromBoat();
            hasFallenFromBoat = true;
        }
    }

    private void HandleFallFromBoat()
    {
        GameLogger.LogError($"[BOAT CREW DEFEAT] {gameObject.name} - Falling from boat");
        
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 1f;
            rb.drag = 0.5f;
            rb.freezeRotation = false;
        }
        
        isOnBoat = false;
        
        if (assignedPlatform != null)
        {
            assignedPlatform.UnregisterEnemy(this);
            assignedPlatform = null;
        }
        
        if (isNavigating)
        {
            ReleaseFromWheel();
        }
        
        if (crewPhysics != null)
        {
            crewPhysics.ResetPhysics();
        }
        
        if (boatPlatform != null)
        {
            BoatController boatController = boatPlatform.GetComponentInParent<BoatController>();
            if (boatController != null)
            {
                boatController.RecalculateBoatIntegrity();
            }
        }
        
        OnEnemyDied?.Invoke(this);
    }
    
    private void FixedUpdate()
    {
        if (_state != EnemyState.Alive) return;

        if (isOnBoat && assignedPlatform != null)
            SyncWithBoatPlatform();
        
        if(crewPhysics != null)
            crewPhysics.UpdatePhysics();
    }
    
    private void SyncWithBoatPlatform()
    {
        Vector3 currentBoatPosition = assignedPlatform.transform.position;
        Vector3 deltaMovement = currentBoatPosition - lastBoatPosition;
        
        if (deltaMovement.magnitude > 0.001f && rb.bodyType == RigidbodyType2D.Kinematic)
        {
            Vector3 newPosition = transform.position + deltaMovement;
            rb.MovePosition(newPosition);
        }
        
        lastBoatPosition = currentBoatPosition;
    }
    
    private void AttemptHookThrowing()
    {
        if (!enableHookThrowing || hookSpawner == null || _state != EnemyState.Alive || isNavigating) return;
        
        if (Time.time - lastHookAttemptTime < 2f) return;
        
        if (!fishingToolEquipped && Time.time > nextActionTime)
        {
            if (Random.value < 0.7f)
            {
                if (TryEquipFishingTool())
                {
                    StartCoroutine(ThrowHookAfterDelay());
                    lastHookAttemptTime = Time.time;
                }
            }
        }
        else if (fishingToolEquipped && hookSpawner.CanThrowHook())
        {
            if (Random.value < 0.5f)
            {
                hookSpawner.ThrowHook();
                lastHookAttemptTime = Time.time;
                ScheduleNextAction();
            }
        }
    }
    
    private IEnumerator ThrowHookAfterDelay()
    {
        yield return new WaitForSeconds(Random.Range(0.5f, 1.5f));
        
        if (fishingToolEquipped && _state == EnemyState.Alive && hookSpawner != null && hookSpawner.CanThrowHook())
        {
            hookSpawner.ThrowHook();
            ScheduleNextAction();
        }
    }

    public override void SetMovementMode(bool aboveWater)
    {
        if (_state == EnemyState.Alive && isOnBoat)
        {
            isAboveWater = aboveWater;
            
            if (rb != null)
            {
                if (aboveWater)
                {
                    rb.bodyType = RigidbodyType2D.Kinematic;
                    rb.gravityScale = 0f;
                    rb.drag = 0f;
                    rb.freezeRotation = true;
                }
                else
                {
                    rb.bodyType = RigidbodyType2D.Dynamic;
                    rb.gravityScale = 0.5f;
                    rb.drag = 3f;
                }
            }
            
            if (debugBoatCrew)
                GameLogger.LogVerbose($"[BOAT CREW] {gameObject.name} - Boat movement mode: {(aboveWater ? "Above Water" : "In Water")}");
        }
        else
        {
            base.SetMovementMode(aboveWater);
        }
    }
    
    public override void LandMovement()
    {
        if (isOnBoat)
        {
            if (isNavigating)
            {
                _landMovementState = LandMovementState.Idle;
                return;
            }
            
            BoatLandMovement();
        }
        else
        {
            base.LandMovement();
        }
    }
    
    private void BoatLandMovement()
    {
        if (Time.time >= nextActionTime)
        {
            MakeBoatAIDecision();
        }
        
        CalculateBoatPlatformBounds();
        ExecuteBoatLandMovementBehaviour();
        
        if (platformBoundsCalculated)
        {
            CheckBoatPlatformBounds();
        }
    }
    
    protected virtual void CalculateBoatPlatformBounds()
    {
        if (assignedPlatform == null) 
        {
            if (debugBoatCrew)
                GameLogger.LogWarning($"[BOAT BOUNDS] {gameObject.name} - No assigned platform!");
            return;
        }

        Collider2D platformCol = assignedPlatform.GetComponent<Collider2D>();

        if (platformCol != null)
        {
            Bounds bounds = platformCol.bounds;
            platformLeftEdge = bounds.min.x + boundaryBuffer;
            platformRightEdge = bounds.max.x - boundaryBuffer;
            platformBoundsCalculated = true;
            
            if (debugBoatCrew && Time.frameCount % 300 == 0)
            {
                GameLogger.LogVerbose($"[BOAT BOUNDS] {gameObject.name} - Left:{platformLeftEdge:F2}, Right:{platformRightEdge:F2}, Center:{bounds.center.x:F2}");
            }
        }
        else
        {
            GameLogger.LogError($"[BOAT BOUNDS ERROR] {gameObject.name} - Platform has no collider!");
        }
    }
    
    protected virtual void CheckBoatPlatformBounds()
    {
        if (!platformBoundsCalculated) return;
        
        float currentX = transform.position.x;
        
        if (currentX <= platformLeftEdge && (_landMovementState == LandMovementState.WalkLeft || _landMovementState == LandMovementState.RunLeft))
        {
            rb.velocity = new Vector2(0, rb.velocity.y);
            _landMovementState = LandMovementState.Idle;
            ChooseRandomActionExcluding(LandMovementState.WalkLeft, LandMovementState.RunLeft);
            ScheduleNextAction();
            
            if (debugBoatCrew)
                GameLogger.LogError($"[BOAT BOUNDS] {gameObject.name} - Hit LEFT boundary at {currentX:F2} (limit: {platformLeftEdge:F2})");
        }
        else if (currentX >= platformRightEdge && (_landMovementState == LandMovementState.WalkRight || _landMovementState == LandMovementState.RunRight))
        {
            rb.velocity = new Vector2(0, rb.velocity.y);
            _landMovementState = LandMovementState.Idle;
            ChooseRandomActionExcluding(LandMovementState.WalkRight, LandMovementState.RunRight);
            ScheduleNextAction();
            
            if (debugBoatCrew)
                GameLogger.LogError($"[BOAT BOUNDS] {gameObject.name} - Hit RIGHT boundary at {currentX:F2} (limit: {platformRightEdge:F2})");
        }
    }
    
    protected virtual void MakeBoatAIDecision()
    {
        ChooseRandomLandAction();
        ScheduleNextAction();
    }
    
    protected virtual void ExecuteBoatLandMovementBehaviour()
    {
        if (fishingToolEquipped || isNavigating) return;
        
        Vector2 movement = Vector2.zero;
        float currentSpeed = isOnBoat ? walkingSpeed * 0.3f : walkingSpeed;
        
        switch (_landMovementState)
        {
            case LandMovementState.Idle:
                EnemyAnimator?.SetBool("isWalking", false);
                EnemyAnimator?.SetBool("isRunning", false);
                EnemyAnimator?.SetBool("isIdle", true);
                break;
            case LandMovementState.WalkLeft:
                EnemyAnimator?.SetBool("isWalking", true);
                EnemyAnimator?.SetBool("isRunning", false);
                EnemyAnimator?.SetBool("isIdle", false);
                transform.localScale = new Vector3(-1f, 1f, 1f);
                movement = Vector2.left * currentSpeed;
                break;
            case LandMovementState.WalkRight:
                EnemyAnimator?.SetBool("isWalking", true);
                EnemyAnimator?.SetBool("isRunning", false);
                EnemyAnimator?.SetBool("isIdle", false);
                transform.localScale = new Vector3(1f, 1f, 1f);
                movement = Vector2.right * currentSpeed;
                break;
            case LandMovementState.RunLeft:
                EnemyAnimator?.SetBool("isWalking", false);
                EnemyAnimator?.SetBool("isRunning", true);
                EnemyAnimator?.SetBool("isIdle", false);
                transform.localScale = new Vector3(-1f, 1f, 1f);
                movement = Vector2.left * (currentSpeed * 1.2f);
                break;
            case LandMovementState.RunRight:
                EnemyAnimator?.SetBool("isWalking", false);
                EnemyAnimator?.SetBool("isRunning", true);
                EnemyAnimator?.SetBool("isIdle", false);
                transform.localScale = new Vector3(1f, 1f, 1f);
                movement = Vector2.right * (currentSpeed * 1.2f);
                break;
        }
        
        if (isOnBoat && rb.bodyType == RigidbodyType2D.Kinematic)
        {
            if (movement != Vector2.zero)
            {
                Vector3 newPosition = transform.position + new Vector3(movement.x * Time.fixedDeltaTime, 0, 0);
                rb.MovePosition(newPosition);
            }
        }
        else
        {
            rb.velocity = new Vector2(movement.x, rb.velocity.y);
        }
    }
    
    public override void OnPlatformAssigned(Platform platform)
    {
        base.OnPlatformAssigned(platform);
        
        boatPlatform = platform as BoatPlatform;
        
        if (boatPlatform != null)
        {
            lastBoatPosition = boatPlatform.transform.position;
            platformBoundsCalculated = false;
            
            GameLogger.LogError($"[BOAT CREW] {gameObject.name} - Assigned to boat platform: {boatPlatform.name}");
        }
        else
        {
            GameLogger.LogError($"[BOAT CREW ERROR] {gameObject.name} - Platform is not BoatPlatform! Type: {platform?.GetType().Name}");
        }
        
        // if (crewPhysics != null)
        // {
        //     crewPhysics.RefreshPlatformColliderCache();
        // }
    }

    public override void Initialize()
    {
        base.Initialize();
        InitializeBoatCrewSystems();
    }

    public override void ReturnToPool()
    {
        if (isReturningToPool) return;
        
        isReturningToPool = true;
        
        ReleaseFromWheel();
        
        if (assignedPlatform != null)
        {
            assignedPlatform.UnregisterEnemy(this);
            assignedPlatform = null;
        }
        
        CleanupBoatSystems();
        
        if (parentContainer != null && parentContainer.activeInHierarchy)
        {
            gameObject.SetActive(false);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void CleanupBoatSystems()
    {
        StopAllCoroutines();
        
        if (crewPhysics != null)
            crewPhysics.ResetPhysics();
    }

    protected override void OnTriggerEnter2D(Collider2D other)
    {
        base.OnTriggerEnter2D(other);
        
        if (other.gameObject.layer == LayerMask.NameToLayer("WaterLine"))
        {
            GameLogger.LogError($"[BOAT CREW WATER] {gameObject.name} - Hit water line");
            
            SetMovementMode(false);
            _landMovementState = LandMovementState.Idle;
            ChangeState_Alive();
        }
    }

    protected void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("WaterLine"))
        {
            GameLogger.LogError($"[BOAT CREW WATER] {gameObject.name} - Left water line");
            
            SetMovementMode(true);
            
            if (isOnBoat && rb != null)
            {
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.gravityScale = 0f;
                rb.drag = 0f;
                rb.freezeRotation = true;
            }
        }
    }

    public override bool TryEquipFishingTool()
    {
        if (isNavigating) return false;
        return base.TryEquipFishingTool();
    }

    public override bool TryUnequipFishingTool()
    {
        bool result = base.TryUnequipFishingTool();
        if (result && isOnBoat)
        {
            _landMovementState = LandMovementState.Idle;
        }
        return result;
    }

    protected override void ChooseRandomLandAction()
    {
        if (fishingToolEquipped || isNavigating) return;
        
        if (isOnBoat)
        {
            float randomValue = Random.value;
            
            if (randomValue < 0.6f)
            {
                _landMovementState = LandMovementState.Idle;
            }
            else if (randomValue < 0.9f)
            {
                _landMovementState = (Random.value < 0.5f) ? LandMovementState.WalkLeft : LandMovementState.WalkRight;
            }
            else
            {
                if (!fishingToolEquipped && hookSpawner != null)
                {
                    TryEquipFishingTool();
                }
            }
        }
        else
        {
            base.ChooseRandomLandAction();
        }
    }
    
    public override void TriggerAlive()
    {
        base.TriggerAlive();
        
        hasFallenFromBoat = false;
        
        if (isOnBoat && rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
            rb.drag = 0f;
            rb.freezeRotation = true;
        }
    }

    protected override void TriggerDefeat()
    {
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 1f;
            rb.drag = 0.5f;
            rb.freezeRotation = false;
        }
        
        base.TriggerDefeat();
        
        GameLogger.LogError($"[BOAT CREW DEFEAT] {gameObject.name} - Base defeat behavior applied");
    }
}
