using System.Collections.Generic;
using UnityEngine;

public class BoatBuoyancySystem : MonoBehaviour
{
    [Header("Kinematic Buoyancy Settings")]
    [SerializeField] private float kinematicBuoyancyForce = 15f;
    [SerializeField] private float kinematicBuoyancySmooth = 2f;
    [SerializeField] private bool useKinematicBuoyancy = true;
    
    [Header("Kinematic Float Control")]
    [SerializeField] private float kinematicFloatHeight = 0.5f;
    [SerializeField] private float kinematicSinkSpeed = 1f;
    [SerializeField] private float kinematicRiseSpeed = 2f;
    
    [Header("Mass Calculation")]
    [SerializeField] private bool enableDynamicMass = true;
    [SerializeField] private float baseMass = 1f;
    [SerializeField] private bool debugMassChanges = true;
    
    private Rigidbody2D rb;
    private WaterPhysics waterPhysics;
    private Transform[] floatPoints;
    private BoatVisualSystem visualSystem;
    private BoatPhysicsSystem physicsSystem;
    
    private Vector3 kinematicBuoyancyOffset;
    private float averageWaterHeight;
    private float boatCenterY;
    
    private float lastKnownMass = 0f;
    private float effectiveBuoyancyForce = 6f;
    private float massCheckTimer = 0f;
    private const float MASS_CHECK_INTERVAL = 0.1f;
    
    private List<Rigidbody2D> cachedChildRigidbodies = new List<Rigidbody2D>();
    private Enemy[] cachedEnemies;
    private bool componentsCached = false;
    
    public void Initialize(Rigidbody2D rigidbody, WaterPhysics physics, Transform[] points)
    {
        rb = rigidbody;
        waterPhysics = physics;
        floatPoints = points;
        visualSystem = GetComponent<BoatVisualSystem>();
        physicsSystem = GetComponent<BoatPhysicsSystem>();
        
        if (rb != null && rb.bodyType == RigidbodyType2D.Kinematic)
        {
            useKinematicBuoyancy = true;
            GameLogger.LogError($"[KINEMATIC BUOYANCY] {gameObject.name} - Initialized kinematic buoyancy system");
        }
        else
        {
            baseMass = rb.mass;
            effectiveBuoyancyForce = kinematicBuoyancyForce;
            RefreshComponentCache();
            lastKnownMass = CalculateTotalMassOptimized();
        }
    }
    
    public void InitializeCrew(List<Enemy> crewMembers)
    {
        cachedEnemies = crewMembers.ToArray();
        cachedChildRigidbodies.Clear();
        
        foreach (var enemy in cachedEnemies)
        {
            if (enemy.Rigidbody2D != null)
            {
                cachedChildRigidbodies.Add(enemy.Rigidbody2D);
            }
        }
        
        componentsCached = true;
        
        if (useKinematicBuoyancy)
        {
            GameLogger.LogError($"[BUOYANCY CREW] {gameObject.name} - Registered {cachedEnemies.Length} crew members for mass calculation");
        }
    }
    
    public void UpdateBuoyancy()
    {
        if (useKinematicBuoyancy)
        {
            UpdateKinematicBuoyancy();
        }
        else
        {
            UpdateDynamicBuoyancy();
        }
    }
    
    private void UpdateKinematicBuoyancy()
    {
        if (floatPoints == null || waterPhysics == null || physicsSystem == null) return;
        
        CalculateKinematicBuoyancyValues();
        ApplyKinematicBuoyancyToPhysics();
    }
    
    private void CalculateKinematicBuoyancyValues()
    {
        float totalWaterHeight = 0f;
        int validPoints = 0;
        boatCenterY = transform.position.y;
        
        foreach (Transform point in floatPoints)
        {
            if (point == null) continue;
            
            Vector2 worldPos = point.position;
            float waterHeight = waterPhysics.GetWaterHeightAt(worldPos, transform);
            
            if (waterHeight > worldPos.y - 2f)
            {
                totalWaterHeight += waterHeight;
                validPoints++;
            }
        }
        
        if (validPoints > 0)
        {
            averageWaterHeight = totalWaterHeight / validPoints;
            
            float targetY = averageWaterHeight + kinematicFloatHeight;
            float currentY = boatCenterY;
            
            float buoyancyDirection = targetY - currentY;
            
            if (buoyancyDirection > 0)
            {
                kinematicBuoyancyOffset.y = buoyancyDirection * kinematicRiseSpeed;
            }
            else
            {
                kinematicBuoyancyOffset.y = buoyancyDirection * kinematicSinkSpeed;
            }
            
            kinematicBuoyancyOffset.y = Mathf.Clamp(kinematicBuoyancyOffset.y, -3f, 3f);
        }
        else
        {
            kinematicBuoyancyOffset.y = -kinematicSinkSpeed;
        }
    }
    
    private void ApplyKinematicBuoyancyToPhysics()
    {
        Vector3 currentKinematicVelocity = physicsSystem.GetKinematicVelocity();
        
        currentKinematicVelocity.y = Mathf.Lerp(
            currentKinematicVelocity.y,
            kinematicBuoyancyOffset.y,
            kinematicBuoyancySmooth * Time.fixedDeltaTime
        );
        
        physicsSystem.SetKinematicVelocity(currentKinematicVelocity);
    }
    
    private void UpdateDynamicBuoyancy()
    {
        if (enableDynamicMass)
        {
            UpdateDynamicBuoyancyOptimized();
        }
        
        ApplyDynamicBuoyancy();
    }
    
    private void ApplyDynamicBuoyancy()
    {
        int submergedPoints = 0;
        Vector2 totalForce = Vector2.zero;
    
        foreach (Transform point in floatPoints)
        {
            if (point == null) continue;
        
            Vector2 worldPos = point.position;
            float waterHeight = waterPhysics.GetWaterHeightAt(worldPos, transform);
            float submersion = waterHeight - worldPos.y;
        
            if (submersion > 0)
            {
                submergedPoints++;
            
                float speedFactor = 1f;
                if (rb.velocity.y > 0)
                {
                    speedFactor = Mathf.Lerp(1f, 0.5f, rb.velocity.y / 3f);
                }
            
                float force = submersion * effectiveBuoyancyForce * speedFactor;
            
                if (visualSystem != null && visualSystem.GetCurrentDirectionMultiplier() < 0)
                {
                    force *= 0.95f;
                }
            
                totalForce += Vector2.up * force;
            }
        }
    
        if (submergedPoints > 0)
        {
            rb.AddForce(totalForce);
        }
    }
    
    private void UpdateDynamicBuoyancyOptimized()
    {
        massCheckTimer += Time.fixedDeltaTime;
        
        if (massCheckTimer >= MASS_CHECK_INTERVAL)
        {
            massCheckTimer = 0f;
            
            float currentTotalMass = CalculateTotalMassOptimized();
            
            if (Mathf.Abs(currentTotalMass - lastKnownMass) > 0.5f)
            {
                lastKnownMass = currentTotalMass;
                
                float requiredBuoyancy = currentTotalMass * Physics2D.gravity.magnitude * 2.0f;
                effectiveBuoyancyForce = Mathf.Max(kinematicBuoyancyForce, requiredBuoyancy);
                
                if (debugMassChanges)
                {
                    GameLogger.LogVerbose($"MASS UPDATE: Total: {currentTotalMass:F2}, Buoyancy: {effectiveBuoyancyForce:F1}");
                }
            }
        }
    }
    
    private float CalculateTotalMassOptimized()
    {
        if (!componentsCached)
        {
            RefreshComponentCache();
        }
        
        float totalMass = rb.mass;
        
        if (cachedChildRigidbodies != null)
        {
            foreach (Rigidbody2D childRb in cachedChildRigidbodies)
            {
                if (childRb != null && childRb != rb)
                {
                    totalMass += childRb.mass;
                }
            }
        }
        
        if (cachedEnemies != null)
        {
            totalMass += cachedEnemies.Length * 0.5f;
        }
        
        return totalMass;
    }
    
    private void RefreshComponentCache()
    {
        componentsCached = true;
        
        if (debugMassChanges)
        {
            GameLogger.LogVerbose($"CACHE REFRESH: Found {cachedChildRigidbodies?.Count ?? 0} rigidbodies, {cachedEnemies?.Length ?? 0} enemies");
        }
    }
    
    public void RecalculateBuoyancy()
    {
        if (useKinematicBuoyancy)
        {
            kinematicBuoyancyOffset = Vector3.zero;
            GameLogger.LogError($"[BUOYANCY RECALC] {gameObject.name} - Kinematic buoyancy recalculated");
        }
        else
        {
            componentsCached = false;
            RefreshComponentCache();
            lastKnownMass = 0f;
            UpdateDynamicBuoyancyOptimized();
        }
    }
    
    #region Public Methods
    
    public void SetKinematicBuoyancyForce(float force)
    {
        kinematicBuoyancyForce = force;
    }
    
    public void SetKinematicFloatHeight(float height)
    {
        kinematicFloatHeight = height;
    }
    
    public float GetAverageWaterHeight()
    {
        return averageWaterHeight;
    }
    
    public Vector3 GetKinematicBuoyancyOffset()
    {
        return kinematicBuoyancyOffset;
    }
    
    #endregion
}
