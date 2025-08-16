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
    [SerializeField] private BoatID boatID;
        
    [Header("Required Components - AUTO ASSIGNED")]
    [SerializeField] private BoatCrewManager crewManager;
    [SerializeField] private BoatFloater boatFloater;
    [SerializeField] private BoatPlatform boatPlatform;
    [SerializeField] private SpriteRenderer boatSpriteRenderer;
    [SerializeField] private Transform crewContainer;
    public Transform CrewContainer => crewContainer;
        
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
        
    private void Awake()
    {
        if (boatID == null)
        {
            boatID = new BoatID();
        }
        else
        {
            boatID.GenerateNewID();
        }
    }
        
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
                
        GameLogger.LogVerbose($"[BOAT CONTROLLER] {gameObject.name} - Boat fully initialized with ID: {boatID.UniqueID}");
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
    }

    private void InitializeCrewManager()
    {
        if (crewManager != null && boatPlatform != null && boatFloater != null)
        {
            crewManager.Initialize(this, boatPlatform, boatFloater);
            crewManager.SetupBoundaries(leftBoundary, rightBoundary);
            crewManager.StartCrewInitialization();
        }
    }
        
    #endregion

    #region Movement System

    private void UpdateMovement()
    {
        if (!movementActive || boatRigidbody == null) return;

        float targetSpeed = GetTargetSpeed();
        Vector2 targetVelocity = new Vector2(targetSpeed * currentMovementDirection, boatRigidbody.velocity.y);
                
        Vector2 force = (targetVelocity - boatRigidbody.velocity) * forceMultiplier;
        force.x = Mathf.Clamp(force.x, -maxMovementForce, maxMovementForce);
                
        boatRigidbody.AddForce(force, ForceMode2D.Force);
                
        if (freezeBoatRotation)
        {
            boatRigidbody.freezeRotation = true;
        }

        CheckBoundaries();
    }

    private float GetTargetSpeed()
    {
        return currentState switch
        {
            BoatState.AutoMove => autoMoveSpeed,
            BoatState.Driven => drivenSpeed,
            _ => 0f
        };
    }

    private void CheckBoundaries()
    {
        if (Time.time - lastBoundaryCheckTime < 0.5f) return;
        lastBoundaryCheckTime = Time.time;

        float currentX = transform.position.x;
                
        if (leftBoundaryPoint != null && rightBoundaryPoint != null)
        {
            float leftX = leftBoundaryPoint.position.x + boundaryBuffer;
            float rightX = rightBoundaryPoint.position.x - boundaryBuffer;
                        
            if (currentX <= leftX && currentMovementDirection < 0)
            {
                FlipDirection();
            }
            else if (currentX >= rightX && currentMovementDirection > 0)
            {
                FlipDirection();
            }
        }
    }

    private void FlipDirection()
    {
        currentMovementDirection *= -1f;
                
        if (boatSpriteRenderer != null)
        {
            Vector3 scale = boatSpriteRenderer.transform.localScale;
            scale.x = Mathf.Abs(scale.x) * currentMovementDirection;
            boatSpriteRenderer.transform.localScale = scale;
        }
    }

    private void SetRandomInitialDirection()
    {
        currentMovementDirection = Random.value > 0.5f ? 1f : -1f;
                
        if (boatSpriteRenderer != null)
        {
            Vector3 scale = boatSpriteRenderer.transform.localScale;
            scale.x = Mathf.Abs(scale.x) * currentMovementDirection;
            boatSpriteRenderer.transform.localScale = scale;
        }
    }

    public void SetMovementDirection(float direction)
    {
        currentMovementDirection = Mathf.Sign(direction);
    }

    public void SetMovementActive(bool active)
    {
        movementActive = active;
    }

    #endregion

    #region Stability Forces

    private void ApplyStabilityForces()
    {
        if (boatRigidbody == null) return;

        Vector2 stabilityForce = Vector2.zero;

        if (Mathf.Abs(boatRigidbody.angularVelocity) > 0.1f)
        {
            stabilityForce.y = -boatRigidbody.angularVelocity * 2f;
        }

        if (stabilityForce.magnitude > 0.01f)
        {
            boatRigidbody.AddForce(stabilityForce, ForceMode2D.Force);
        }
    }

    #endregion

    #region State System

    public void InitializeState(BoatState startState)
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
                
        GameLogger.LogVerbose($"[BOAT STATE] {gameObject.name} - Changed to {currentState} state");
    }
        
    private void EnterCurrentState()
    {
        switch (currentState)
        {
            case BoatState.Idle:
                SetMovementActive(false);
                break;
                            
            case BoatState.AutoMove:
                SetMovementActive(enableAutomaticMovement);
                if (crewManager != null)
                    crewManager.AssignNavigator();
                break;
                            
            case BoatState.Driven:
                SetMovementActive(true);
                break;
                            
            case BoatState.Destroyed:
                SetMovementActive(false);
                StartCoroutine(HandleDestruction());
                break;
        }
    }
        
    private void ExitCurrentState()
    {
        switch (currentState)
        {
            case BoatState.AutoMove:
                if (crewManager != null)
                    crewManager.ReleaseNavigator();
                break;
                            
            case BoatState.Driven:
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
                break;
        }
    }

    public void ChangeState_Driven()
    {
        ChangeState(BoatState.Driven);
    }

    public void ChangeState_AutoMove()
    {
        ChangeState(BoatState.AutoMove);
    }
        
    private void UpdateIdleState()
    {
        if (stateTimer > Random.Range(2f, 5f))
        {
            ChangeState(BoatState.AutoMove);
        }
    }
        
    private void UpdateAutoMoveState()
    {
        if (isDestroyed)
        {
            ChangeState(BoatState.Destroyed);
            return;
        }
                
        if (stateTimer > Random.Range(15f, 30f))
        {
            ChangeState(BoatState.Idle);
        }
    }
        
    private void UpdateDrivenState()
    {
        if (isDestroyed)
        {
            ChangeState(BoatState.Destroyed);
            return;
        }
    }

    #endregion

    #region Health System

    public void SetInitialIntegrity(float maxIntegrityValue, float currentIntegrityValue)
    {
        maxIntegrity = maxIntegrityValue;
        currentIntegrity = currentIntegrityValue;
    }

    public void OnCrewInitializationComplete()
    {
        if (crewManager != null && crewManager.HasActiveNavigator())
        {
            ChangeState_Driven();
        }
        else
        {
            ChangeState_AutoMove();
        }
    }

    public void RecalculateBoatIntegrity()
    {
        if (!isInitialized) return;

        currentIntegrity = 0f;
        tempEnemyList.Clear();

        if (boatPlatform != null)
        {
            boatPlatform.GetRegisteredEnemies(tempEnemyList);

            foreach (var enemy in tempEnemyList)
            {
                if (enemy != null && enemy.State == Enemy.EnemyState.Alive)
                {
                    currentIntegrity += enemy.PowerLevel;
                }
            }
        }

        tempEnemyList.Clear();

        if (currentIntegrity <= 0f && !isDestroyed)
        {
            TriggerDestruction();
        }
    }

    public void TriggerDestruction()
    {
        if (isDestroyed) return;

        isDestroyed = true;
        ChangeState(BoatState.Destroyed);
        OnBoatSunk?.Invoke(this);
    }

    private IEnumerator HandleDestruction()
    {
        yield return new WaitForSeconds(destructionDelay);

        DestroyBoatParts();

        yield return new WaitForSeconds(resetDelay);

        ResetBoat();
    }

    private void DestroyBoatParts()
    {
        if (boatParts != null)
        {
            foreach (var part in boatParts)
            {
                if (part != null)
                {
                    part.ApplyInitialForces();
                }
            }
        }
    }

    #endregion

    #region Reset System

    public void ResetBoat()
    {
        isDestroyed = false;
        currentIntegrity = 0f;

        if (crewManager != null)
        {
            crewManager.Reset();
        }

        if (boatParts != null)
        {
            foreach (var part in boatParts)
            {
                if (part != null)
                {
                    part.ResetToOriginalPosition();
                }
            }
        }

        SetRandomInitialDirection();
        ChangeState(BoatState.Idle);

        if (useObjectPool)
        {
            ReturnToPool();
        }
    }

    private void ReturnToPool()
    {
        if (SimpleObjectPool.Instance != null)
        {
            SimpleObjectPool.Instance.ReturnToPool(poolName, gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    #endregion

    #region Public Interface

    public BoatState GetCurrentState()
    {
        return currentState;
    }

    public float GetCurrentIntegrity()
    {
        return currentIntegrity;
    }

    public float GetMaxIntegrity()
    {
        return maxIntegrity;
    }

    public bool IsDestroyed()
    {
        return isDestroyed;
    }

    public string GetBoatID()
    {
        return boatID?.UniqueID ?? "NO_ID";
    }

    public int GetActiveCrewCount()
    {
        return crewManager?.GetActiveCrewCount() ?? 0;
    }

    #endregion

    #region Debug Methods

    private void OnDrawGizmosSelected()
    {
        if (leftBoundaryPoint != null && rightBoundaryPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(leftBoundaryPoint.position, rightBoundaryPoint.position);
                        
            Gizmos.color = Color.red;
            Vector3 leftBoundary = leftBoundaryPoint.position + Vector3.right * boundaryBuffer;
            Vector3 rightBoundary = rightBoundaryPoint.position - Vector3.right * boundaryBuffer;
                        
            Gizmos.DrawWireSphere(leftBoundary, 0.5f);
            Gizmos.DrawWireSphere(rightBoundary, 0.5f);
        }
                
        Gizmos.color = Color.blue;
        Vector3 directionIndicator = transform.position + Vector3.right * currentMovementDirection * 2f;
        Gizmos.DrawRay(transform.position, Vector3.right * currentMovementDirection * 2f);
    }

    #endregion
}
