using System.Collections.Generic;
using UnityEngine;

public class BoatFloater : MonoBehaviour
{
    [Header("Buoyancy Settings")]
    [SerializeField] private float depthBeforeSubmerged = 1f;
    [SerializeField] private float displacementAmount = 3f;
    [SerializeField] private float waterDrag = 0.99f;
    [SerializeField] private float waterAngularDrag = 0.5f;
    
    [Header("Float Points")]
    [SerializeField] private List<Transform> floatPoints = new List<Transform>();
    
    [Header("Movement")]
    [SerializeField] private bool enableAutomaticMovement = false;
    [SerializeField] private float movementForce = 5f;
    [SerializeField] private float currentDirection = 1f;
    
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private List<Enemy> crewMembers = new List<Enemy>();
    
  
    void FixedUpdate()
    {
        if (GameStates.instance.IsGameplayRunning())
        {
            ApplyBuoyancy();
            
            if (enableAutomaticMovement)
            {
                ApplyMovement();
            }
        }
    }
    
    private void ApplyBuoyancy()
    {
        if (WaterPhysics.Instance == null || floatPoints.Count == 0) return;
        
        foreach (Transform floatPoint in floatPoints)
        {
            rb.AddForceAtPosition(Physics2D.gravity / floatPoints.Count, floatPoint.position, ForceMode2D.Force);
            
            float waveHeight = WaterPhysics.Instance.GetWaterHeightAt(floatPoint.position, transform);
            
            if (floatPoint.position.y < waveHeight)
            {
                float displacementMultiplier = Mathf.Clamp01((waveHeight - floatPoint.position.y) / depthBeforeSubmerged) * displacementAmount;
                
                rb.AddForceAtPosition(
                    new Vector2(0f, Mathf.Abs(Physics2D.gravity.y) * displacementMultiplier),
                    floatPoint.position,
                    ForceMode2D.Force
                );
                
                rb.velocity += -rb.velocity * (displacementMultiplier * waterDrag * Time.fixedDeltaTime);
                rb.angularVelocity += displacementMultiplier * -rb.angularVelocity * waterAngularDrag * Time.fixedDeltaTime;
            }
        }
    }
    
    private void ApplyMovement()
    {
        rb.AddForce(Vector2.right * (currentDirection * movementForce), ForceMode2D.Force);
    }
    
    public void SetAutomaticMovementEnabled(bool enabled)
    {
        enableAutomaticMovement = enabled;
    }
    
    public void SetMovementDirection(float direction)
    {
        currentDirection = Mathf.Clamp(direction, -1f, 1f);
    }
    
    public void ForceStartMovement()
    {
        enableAutomaticMovement = true;
    }
    
    public void StopMovement()
    {
        enableAutomaticMovement = false;
    }
    
    public bool IsMovementActive()
    {
        return enableAutomaticMovement;
    }
    
    public float GetCurrentMovementDirection()
    {
        return currentDirection;
    }
    
    public void InitializeCrew(List<Enemy> crew)
    {
        crewMembers = crew;
    }
    
    public void InitializeBoundaries(Transform leftBoundary, Transform rightBoundary)
    {
    }
    
    public void OnRegisteredToPlatform(Platform platform)
    {
    }
    
    public void RecalculateBuoyancy()
    {
        displacementAmount = 15f + (crewMembers.Count * 2f);
    }
}
