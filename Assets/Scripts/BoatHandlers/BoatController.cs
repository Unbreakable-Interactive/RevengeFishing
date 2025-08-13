using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public enum BoatState
{
    Idle,
    AutoMove,
    Driven,
    Destroyed
}

public class BoatController : MonoBehaviour
{
    [Header("Boat Identity")]
    [SerializeField] private BoatID boatID = new BoatID();
    
    [Header("Required Components - AUTO ASSIGNED")]
    [SerializeField] private BoatCrewManager crewManager;
    [SerializeField] private BoatFloater boatFloater;
    [SerializeField] private BoatPlatform boatPlatform;
    [SerializeField] private SpriteRenderer boatSpriteRenderer;
    
    [Header("Movement System")]
    [SerializeField] private float autoMoveSpeed = 1f;
    [SerializeField] private float drivenSpeed = 2.5f;
    [SerializeField] private float maxMovementForce = 2.5f;
    [SerializeField] private bool enableAutomaticMovement = true;
    [SerializeField] private float boundaryBuffer = 1f;
    [SerializeField] private float currentMovementDirection = 1f;
    [SerializeField] private bool movementActive = false;
    
    [Header("Boat State System")]
    [SerializeField] private BoatState currentState = BoatState.Idle;
    [SerializeField] private bool canPlayLogic = true;
    
    [Header("Physics System")]
    [SerializeField] private float forceMultiplier = 5f;
    [SerializeField] private bool freezeBoatRotation = true;
    
    [Header("Patrol Boundaries")]
    [SerializeField] private Transform leftBoundaryPoint;
    [SerializeField] private Transform rightBoundaryPoint;
    
    [Header("Boundary References")]
    [SerializeField] private BoatBoundaryTrigger leftBoundary;
    [SerializeField] private BoatBoundaryTrigger rightBoundary;
    
    [Header("Boat Health System")]
    [SerializeField] private float currentIntegrity = 0f;
    [SerializeField] private float maxIntegrity = 200f;
    [SerializeField] private bool isDestroyed = false;
    
    [Header("Destruction System")]
    [SerializeField] private BoatPart[] boatParts;
    [SerializeField] private float destructionDelay = 2f;
    [SerializeField] private float resetDelay = 8f;
    
    [Header("Pool Management")]
    [SerializeField] private string poolName = "Boat";
    [SerializeField] private bool useObjectPool = true;
    
    private Rigidbody2D boatRigidbody;
    private float lastBoundaryCheckTime = 0f;
    private float stateTimer = 0f;
    private Vector2 cachedPosition;
    private static readonly List<Enemy> tempEnemyList = new List<Enemy>();
    private bool isInitialized = false;
    
    public static event Action<BoatController> OnBoatSunk;
    
    #region Unity Lifecycle
    
    private void Update()
    {
        if (!isInitialized || !canPlayLogic || isDestroyed) return;
        
        stateTimer += Time.deltaTime;
        UpdateCurrentState();
    }
    
    private void FixedUpdate()
    {
        if (!isInitialized || isDestroyed) return;
        
        ApplyStabilityForces();
        UpdateMovement();
    }
    
    #endregion
    
    #region Initialization
    
    public void Initialize(Transform _leftBoundary, Transform _rightBoundary)
    {
        if (isInitialized)
        {
            GameLogger.LogWarning($"[BOAT CONTROLLER] {gameObject.name} - Already initialized, skipping");
            return;
        }
        
        leftBoundaryPoint  = _leftBoundary;
        rightBoundaryPoint  = _rightBoundary;
        
        CacheComponents();
        SetupBoatPhysics();
        SetupBoatComponents();
        ConfigureAllBoatIDs();
        InitializeCrewManager();
        SetRandomInitialDirection();
        InitializeState(BoatState.Idle);
        
        isInitialized = true;
        
        GameLogger.LogError($"[BOAT CONTROLLER] {gameObject.name} - Boat fully initialized with ID: {boatID.UniqueID}");
    }
    
    private void CacheComponents()
    {
        if (crewManager == null) crewManager = GetComponent<BoatCrewManager>();
        if (boatFloater == null) boatFloater = GetComponent<BoatFloater>();
        if (boatPlatform == null) boatPlatform = GetComponentInChildren<BoatPlatform>();
        if (boatSpriteRenderer == null) boatSpriteRenderer = GetComponentInChildren<SpriteRenderer>();
        
        boatRigidbody = GetComponent<Rigidbody2D>();
        
        if (boatRigidbody == null)
        {
            GameLogger.LogError($"[BOAT CONTROLLER] {gameObject.name} - Missing Rigidbody2D component!");
        }
    }
    
    private void SetupBoatPhysics()
    {
        if (boatRigidbody != null)
        {
            boatRigidbody.bodyType = RigidbodyType2D.Dynamic;
            boatRigidbody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            boatRigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;
        }
    }

    private void SetupBoatComponents()
    {
        if (boatPlatform == null)
            boatPlatform = GetComponentInChildren<BoatPlatform>();
        
        if (boatFloater == null)
            boatFloater = GetComponentInChildren<BoatFloater>();
        
        if (crewManager == null)
            crewManager = GetComponentInChildren<BoatCrewManager>();
        
        BoatBoundaryTrigger[] boundaries = GetComponentsInChildren<BoatBoundaryTrigger>();
        foreach (var boundary in boundaries)
        {
            if (boundary.IsLeftBoundary)
                leftBoundary = boundary;
            else
                rightBoundary = boundary;
        }
        
        // if (leftBoundaryPoint != null) leftBoundaryX = leftBoundaryPoint.position.x;
        // if (rightBoundaryPoint != null) rightBoundaryX = rightBoundaryPoint.position.x;
    }

    private void ConfigureAllBoatIDs()
    {
        if (boatPlatform != null)
            boatPlatform.SetBoatID(boatID);
    
        if (crewManager != null)
            crewManager.SetBoatID(boatID);
    
        if (leftBoundary != null)
            leftBoundary.SetBoatID(boatID);
    
        if (rightBoundary != null)
            rightBoundary.SetBoatID(boatID);
        
        leftBoundary.SetBoatID(boatID);
        rightBoundary.SetBoatID(boatID);
    
        GameLogger.LogVerbose($"[BOAT CONTROLLER] All components configured with BoatID: {boatID.UniqueID}");
    }

    private void InitializeCrewManager()
    {
        if (crewManager != null)
        {
            crewManager.Initialize(this, boatPlatform, boatFloater);
            crewManager.SetupBoundaries(leftBoundary, rightBoundary);
            crewManager.StartCrewInitialization();
        }
    }
    
    private void SetRandomInitialDirection()
    {
        currentMovementDirection = Random.Range(0, 2) == 0 ? -1f : 1f;
        
        // float currentX = transform.position.x;
        // if (currentX <= leftBoundaryX + 1f)
        // {
        //     currentMovementDirection = 1f;
        // }
        // else if (currentX >= rightBoundaryX - 1f)
        // {
        //     currentMovementDirection = -1f;
        // }
    }
    
    #endregion
    
    #region Movement System
    
    private void UpdateMovement()
    {
        if (!enableAutomaticMovement || currentState == BoatState.Destroyed) return;
        
        if (!movementActive && (currentState == BoatState.AutoMove || currentState == BoatState.Driven))
        {
            StartMovement();
            return;
        }
        
        if (movementActive)
        {
            CheckBoundaries();
            ApplyMovementForce();
        }
    }
    
    private void CheckBoundaries()
    {
        if (Time.time - lastBoundaryCheckTime < 0.1f) return;
        lastBoundaryCheckTime = Time.time;
        
        cachedPosition = transform.position;
        
        bool hitLeft = cachedPosition.x <= leftBoundaryPoint.position.x && currentMovementDirection < 0;
        bool hitRight = cachedPosition.x >= rightBoundaryPoint.position.x && currentMovementDirection > 0;
        
        if (hitLeft || hitRight)
        {
            currentMovementDirection *= -1f;
            UpdateVisualDirection();
            
            GameLogger.LogVerbose($"[BOAT BOUNDARY] {gameObject.name} - Hit boundary at {cachedPosition.x:F1}, direction now: {(currentMovementDirection > 0 ? "RIGHT" : "LEFT")}");
        }
    }
    
    private void ApplyMovementForce()
    {
        if (boatRigidbody == null || !movementActive) return;
        
        float currentSpeed = GetCurrentMovementSpeed();
        float targetForce = currentMovementDirection * currentSpeed * forceMultiplier;
        
        Vector2 force = Vector2.right * targetForce;
        boatRigidbody.AddForce(force);
    }
    
    private float GetCurrentMovementSpeed()
    {
        switch (currentState)
        {
            case BoatState.AutoMove:
                return autoMoveSpeed;
            case BoatState.Driven:
                return drivenSpeed;
            case BoatState.Destroyed:
                return 0f;
            default:
                return autoMoveSpeed;
        }
    }
    
    private void StartMovement()
    {
        movementActive = true;
        GameLogger.LogVerbose($"[BOAT MOVEMENT] {gameObject.name} - Movement started, direction: {(currentMovementDirection > 0 ? "RIGHT" : "LEFT")}");
    }
    
    private void StopMovement()
    {
        movementActive = false;
        if (boatRigidbody != null)
        {
            boatRigidbody.velocity = Vector2.zero;
        }
        GameLogger.LogVerbose($"[BOAT MOVEMENT] {gameObject.name} - Movement stopped");
    }
    
    #endregion
    
    #region Visual System
    
    private void UpdateVisualDirection()
    {
        if (boatSpriteRenderer != null)
        {
            boatSpriteRenderer.flipX = currentMovementDirection < 0;
        }
    }
    
    private void ActivateBoatPartsDestruction()
    {
        if (boatSpriteRenderer != null)
        {
            boatSpriteRenderer.gameObject.SetActive(false);
        }
        
        StartCoroutine(BatchExplosionSequence());
    }
    
    private IEnumerator BatchExplosionSequence()
    {
        List<int> remainingParts = new List<int>();
        
        for (int i = 0; i < boatParts.Length; i++)
        {
            if (boatParts[i] != null)
            {
                boatParts[i].gameObject.SetActive(true);
                remainingParts.Add(i);
            }
        }
        
        int maxSimultaneousExplosions = 3;
        float explosionDelayRange = 0.3f;
        
        while (remainingParts.Count > 0)
        {
            int batchSize = Mathf.Min(maxSimultaneousExplosions, remainingParts.Count);
            
            for (int i = 0; i < batchSize; i++)
            {
                int randomIndex = Random.Range(0, remainingParts.Count);
                int partIndex = remainingParts[randomIndex];
                
                if (boatParts[partIndex] != null)
                {
                    boatParts[partIndex].ApplyInitialForces();
                }
                
                remainingParts.RemoveAt(randomIndex);
            }
            
            if (remainingParts.Count > 0)
            {
                yield return new WaitForSeconds(Random.Range(0.1f, explosionDelayRange));
            }
        }
    }
    
    private void ResetBoatPartsVisual()
    {
        for (int i = 0; i < boatParts.Length; i++)
        {
            if (boatParts[i] != null)
            {
                boatParts[i].gameObject.SetActive(false);
                boatParts[i].ResetToOriginalPosition();
            }
        }
        
        if (boatSpriteRenderer != null)
        {
            boatSpriteRenderer.gameObject.SetActive(true);
        }
    }
    
    #endregion
    
    #region State Machine
    
    private void InitializeState(BoatState startState)
    {
        currentState = startState;
        stateTimer = 0f;
        EnterCurrentState();
    }
    
    private void ChangeState(BoatState newState)
    {
        if (currentState == newState) return;
        
        ExitCurrentState();
        currentState = newState;
        stateTimer = 0f;
        EnterCurrentState();
        
        GameLogger.LogError($"[BOAT STATE] {gameObject.name} - Changed to {currentState} state");
    }
    
    private void EnterCurrentState()
    {
        switch (currentState)
        {
            case BoatState.Idle:
                EnterIdleState();
                break;
            case BoatState.AutoMove:
                EnterAutoMoveState();
                break;
            case BoatState.Driven:
                EnterDrivenState();
                break;
            case BoatState.Destroyed:
                EnterDestroyedState();
                break;
        }
    }
    
    private void UpdateCurrentState()
    {
        switch (currentState)
        {
            case BoatState.Idle:
                UpdateIdleState();
                break;
            case BoatState.AutoMove:
                UpdateAutoMoveState();
                break;
            case BoatState.Driven:
                UpdateDrivenState();
                break;
            case BoatState.Destroyed:
                UpdateDestroyedState();
                break;
        }
    }
    
    private void ExitCurrentState()
    {
        switch (currentState)
        {
            case BoatState.Idle:
                ExitIdleState();
                break;
            case BoatState.AutoMove:
                ExitAutoMoveState();
                break;
            case BoatState.Driven:
                ExitDrivenState();
                break;
        }
    }
    
    #endregion
    
    #region State Implementations
    
    private void EnterIdleState()
    {
        StopMovement();
        if (crewManager != null) crewManager.ReleaseNavigator();
    }
    
    private void UpdateIdleState()
    {
        if (stateTimer >= 2f)
        {
            ChangeState(BoatState.AutoMove);
        }
    }
    
    private void ExitIdleState() { }
    
    private void EnterAutoMoveState()
    {
        if (crewManager != null) crewManager.ReleaseNavigator();
        StartMovement();
    }
    
    private void UpdateAutoMoveState() { }
    
    private void ExitAutoMoveState() { }
    
    private void EnterDrivenState()
    {
        if (crewManager != null) crewManager.AssignNavigator();
        StartMovement();
    }
    
    private void UpdateDrivenState() { }
    
    private void ExitDrivenState() { }
    
    private void EnterDestroyedState()
    {
        StopMovement();
        canPlayLogic = false;
        isDestroyed = true;
        
        ActivateBoatPartsDestruction();
        OnBoatSunk?.Invoke(this);
        
        StartCoroutine(ResetBoatAfterDelay());
    }
    
    private void UpdateDestroyedState() { }
    
    #endregion
    
    #region Physics & Stability
    
    private void ApplyStabilityForces()
    {
        if (boatRigidbody != null)
        {
            if (freezeBoatRotation)
            {
                boatRigidbody.angularVelocity = 0f;
                transform.rotation = Quaternion.identity;
            }
            
            float maxSpeed = GetCurrentMovementSpeed() * 2f;
            if (boatRigidbody.velocity.magnitude > maxSpeed)
            {
                boatRigidbody.velocity = boatRigidbody.velocity.normalized * maxSpeed;
            }
        }
    }
    
    #endregion
    
    #region Crew Management & Integrity
    
    public void StartCrewInitialization()
    {
        if (crewManager != null)
        {
            crewManager.StartCrewInitialization();
        }
    }
    
    public void RecalculateBoatIntegrity()
    {
        if (crewManager == null) return;
        
        currentIntegrity = 0f;
        var allCrew = crewManager.GetAllCrewMembers();
        
        foreach (var crew in allCrew)
        {
            if (crew != null && 
                crew.ParentContainer != null && 
                crew.ParentContainer.activeInHierarchy && 
                crew.State == Enemy.EnemyState.Alive)
            {
                currentIntegrity += crew.PowerLevel;
            }
        }
        
        maxIntegrity = currentIntegrity;
        
        int activeCrewCount = crewManager.GetActiveCrewCount();
        
        if (activeCrewCount <= 0 && !isDestroyed && allCrew.Count > 0)
        {
            SinkBoat();
        }
        else if (activeCrewCount > 0 && isDestroyed && currentState != BoatState.Destroyed)
        {
            isDestroyed = false;
            canPlayLogic = true;
            ChangeState(BoatState.Idle);
        }
    }
    
    public void SinkBoat()
    {
        if (!isDestroyed)
        {
            ChangeState(BoatState.Destroyed);
        }
    }
    
    #endregion
    
    #region Destruction & Reset
    
    private IEnumerator ResetBoatAfterDelay()
    {
        yield return new WaitForSeconds(resetDelay);
        
        ResetBoatPartsVisual();
        ResetBoat();
    }
    
    public void ResetBoat()
    {
        currentIntegrity = 0f;
        isDestroyed = false;
        canPlayLogic = true;
        
        if (crewManager != null)
        {
            crewManager.Reset();
        }
        
        SetRandomInitialDirection();
        ChangeState(BoatState.Idle);
        
        GameLogger.LogError($"[BOAT RESET] {gameObject.name} - Boat reset completed");
    }
    
    #endregion
    
    #region Public Interface
    
    public void SetMovementState_AutoMove()
    {
        if (!isInitialized) return;
        ChangeState(BoatState.AutoMove);
    }
    
    public void SetMovementState_Driven()
    {
        if (!isInitialized) return;
        ChangeState(BoatState.Driven);
    }
    
    public void SetAutomaticMovementEnabled(bool enabled)
    {
        enableAutomaticMovement = enabled;
        if (!enabled) StopMovement();
    }
    
    public void DestroyBoat()
    {
        if (!isInitialized) return;
        ChangeState(BoatState.Destroyed);
    }
    
    public void ReturnToPool()
    {
        isInitialized = false;
        canPlayLogic = false;
        isDestroyed = false;
        StopMovement();
        
        if (useObjectPool && SimpleObjectPool.Instance != null)
        {
            SimpleObjectPool.Instance.ReturnToPool(poolName, gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
    
    // Getters
    public float GetCurrentIntegrity() => currentIntegrity;
    public float GetMaxIntegrity() => maxIntegrity;
    public bool IsDestroyed() => isDestroyed;
    public bool IsMovementActive() => movementActive;
    public float GetCurrentDirection() => currentMovementDirection;
    public BoatState GetCurrentState() => currentState;
    public bool IsInitialized() => isInitialized;
    public BoatFloater BoatFloater => boatFloater;
    public BoatPlatform BoatPlatform => boatPlatform;
    public BoatCrewManager CrewManager => crewManager;
    
    public List<Enemy> GetAllCrewMembers()
    {
        tempEnemyList.Clear();
        
        if (crewManager != null)
        {
            var boatCrew = crewManager.GetAllCrewMembers();
            for (int i = 0; i < boatCrew.Count; i++)
            {
                tempEnemyList.Add(boatCrew[i]);
            }
        }
        
        return tempEnemyList;
    }
    
    #endregion
    
    #region Debug
    
    private void OnDrawGizmosSelected()
    {
        if (leftBoundaryPoint != null && rightBoundaryPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(
                new Vector3(leftBoundaryPoint.position.x, transform.position.y - 2f, 0),
                new Vector3(leftBoundaryPoint.position.x, transform.position.y + 2f, 0)
            );
            Gizmos.DrawLine(
                new Vector3(rightBoundaryPoint.position.x, transform.position.y - 2f, 0),
                new Vector3(rightBoundaryPoint.position.x, transform.position.y + 2f, 0)
            );
            
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(
                new Vector3(leftBoundaryPoint.position.x, transform.position.y, 0),
                new Vector3(rightBoundaryPoint.position.x, transform.position.y, 0)
            );
            
            Gizmos.color = Color.green;
            float arrowX = transform.position.x + currentMovementDirection * 2f;
            Gizmos.DrawLine(transform.position, new Vector3(arrowX, transform.position.y, 0));
        }
    }
    
    #endregion
}
