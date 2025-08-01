using System;
using System.Collections.Generic;
using UnityEngine;

public class BoatFloater : MonoBehaviour
{
    [Header("Core Systems")]
    [SerializeField] private BoatPhysicsSystem physicsSystem;
    [SerializeField] private BoatMovementSystem movementSystem;
    [SerializeField] private BoatVisualSystem visualSystem;
    [SerializeField] private BoatBuoyancySystem buoyancySystem;
    
    [Header("Float Points")]
    public Transform[] floatPoints = new Transform[3];
    
    [Header("Control")]
    [SerializeField] private bool enableFloaterMovement = true;
    
    private Rigidbody2D rb;
    private WaterPhysics waterPhysics;
    
    public void Initialize()
    {
        rb = GetComponent<Rigidbody2D>();
        waterPhysics = WaterPhysics.Instance;
        
        if (ValidateFloatPoints())
        {
            physicsSystem.Initialize(rb, waterPhysics);
            buoyancySystem.Initialize(rb, waterPhysics, floatPoints);
            movementSystem.Initialize(rb, visualSystem);
            visualSystem.Initialize();
        }
    }
    
    private bool ValidateFloatPoints()
    {
        if (floatPoints[0] == null || floatPoints[1] == null || floatPoints[2] == null)
        {
            Debug.LogError("BoatFloater: Float Points not assigned in inspector.");
            return false;
        }
        return true;
    }

    #if UNITY_EDITOR
    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.Alpha1))
            movementSystem.SetMovementState_Driven();
        
        if(Input.GetKeyDown(KeyCode.Alpha2))
            movementSystem.SetMovementState_AutoMove();
    }
    #endif

    void FixedUpdate()
    {
        if (!enableFloaterMovement || waterPhysics == null) return;
        
        buoyancySystem.UpdateBuoyancy();
        movementSystem.UpdateMovement();
        physicsSystem.UpdatePhysics();
    }

    #region Public Methods

    public void InitializeCrew(List<Enemy> crewMembers)
    {
        buoyancySystem.InitializeCrew(crewMembers);
    }
    
    public void InitializeBoundaries(Transform leftBoundary, Transform rightBoundary)
    {
        movementSystem.InitializeBoundaries(leftBoundary, rightBoundary);
    }
    
    public void OnRegisteredToPlatform(Platform platform)
    {
        movementSystem.OnRegisteredToPlatform(platform);
    }
    
    public void RecalculateBuoyancy()
    {
        buoyancySystem.RecalculateBuoyancy();
    }

    #endregion
    
    public void SetAutomaticMovementEnabled(bool enabled) => movementSystem.SetAutomaticMovementEnabled(enabled);
    public void ForceStartMovement() => movementSystem.ForceStartMovement();
    public void StopMovement() => movementSystem.StopMovement();
    public bool IsMovementActive() => movementSystem.IsMovementActive();
    public float GetCurrentMovementDirection() => movementSystem.GetCurrentMovementDirection();
    
    public void SetHorizontalForce(float force) => physicsSystem.SetHorizontalForce(force);
    public void AddHorizontalForce(float additionalForce) => physicsSystem.AddHorizontalForce(additionalForce);
    
    public float GetCurrentDirectionMultiplier() => visualSystem.GetCurrentDirectionMultiplier();
    public void SetBoatSpriteRenderer(SpriteRenderer renderer) => visualSystem.SetBoatSpriteRenderer(renderer);
}
