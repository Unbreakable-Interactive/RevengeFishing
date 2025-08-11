using System.Collections.Generic;
using UnityEngine;

public class BoatPhysicsSystem : MonoBehaviour
{
    [Header("Kinematic Settings")]
    [SerializeField] private Vector3 kinematicVelocity;
    [SerializeField] private float maxKinematicSpeed = 25f;
    
    [Header("Resistance")]
    [SerializeField] private float kinematicDrag = 0.98f;
    
    [Header("Debug")]
    [SerializeField] private bool debugPhysics = true;
    
    private Rigidbody2D rb;
    private Vector3 smoothedKinematicVelocity;
    
    public void Initialize(Rigidbody2D rigidbody, WaterPhysics physics, Transform[] points)
    {
        rb = rigidbody;
        SetupKinematicMode();
        GameLogger.LogError($"[KINEMATIC PHYSICS] {gameObject.name} - Physics system initialized");
    }
    
    private void SetupKinematicMode()
    {
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.mass = 12f;
            rb.simulated = true;
            GameLogger.LogError($"[KINEMATIC PHYSICS] {gameObject.name} - Set to KINEMATIC mode");
        }
    }
    
    public void UpdatePhysics()
    {
        ApplyMovement();
    }
    
    private void ApplyMovement()
    {
        if (rb == null) return;
        
        kinematicVelocity.y *= kinematicDrag;
        
        smoothedKinematicVelocity = Vector3.Lerp(smoothedKinematicVelocity, kinematicVelocity, 8f * Time.fixedDeltaTime);
        Vector3 deltaPosition = smoothedKinematicVelocity * Time.fixedDeltaTime;
        
        if (deltaPosition.magnitude > 0.001f)
        {
            rb.MovePosition(rb.position + (Vector2)deltaPosition);
            
            if (debugPhysics)
            {
                GameLogger.LogError($"[PHYSICS MOVE] {gameObject.name} - MOVED by: {deltaPosition}");
            }
        }
        else if (debugPhysics && Mathf.Abs(kinematicVelocity.x) > 0.01f)
        {
            GameLogger.LogError($"[PHYSICS MOVE] {gameObject.name} - NO MOVEMENT: Delta too small {deltaPosition.magnitude}");
        }
    }

    public void SetTargetVelocity(Vector2 targetVelocity)
    {
        kinematicVelocity.x = Mathf.Clamp(targetVelocity.x, -maxKinematicSpeed, maxKinematicSpeed);
        kinematicVelocity.y = targetVelocity.y; // Mantener Y para buoyancy
        
        if (debugPhysics)
        {
            GameLogger.LogError($"[PHYSICS VELOCITY] {gameObject.name} - Target velocity set to: {kinematicVelocity}");
        }
    }
    
    public void SetHorizontalForce(float force)
    {
        kinematicVelocity.x = force;
        
        if (debugPhysics)
        {
            GameLogger.LogError($"[PHYSICS LEGACY] {gameObject.name} - Force as velocity: {force}");
        }
    }
    
    public void StopKinematicMovement()
    {
        kinematicVelocity = Vector3.zero;
        smoothedKinematicVelocity = Vector3.zero;
    }
    
    public Vector3 GetKinematicVelocity() => kinematicVelocity;
    public void SetKinematicVelocity(Vector3 velocity) => kinematicVelocity = velocity;
    public void AddHorizontalForce(float additionalForce) => kinematicVelocity.x += additionalForce;
    public void RegisterCrewMembers(List<Enemy> crewMembers) { }
    public void InitializeBoundaries(Transform left, Transform right) { }
    public void ConfigureCrewPhysicsIsolation(List<BoatLandEnemy> crewMembers) { }
}
