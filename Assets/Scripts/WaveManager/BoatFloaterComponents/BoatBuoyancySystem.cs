using System.Collections.Generic;
using UnityEngine;

public class BoatBuoyancySystem : MonoBehaviour
{
    [Header("Buoyancy Settings")]
    [SerializeField] private float buoyancyForce = 25f;
    [SerializeField] private float smoothBuoyancy = 0.5f;
    
    [Header("Dynamic Mass")]
    [SerializeField] private bool enableDynamicBuoyancy = true;
    [SerializeField] private float baseMass = 1f;
    [SerializeField] private bool debugMassChanges = true;
    
    private Rigidbody2D rb;
    private WaterPhysics waterPhysics;
    private Transform[] floatPoints;
    private BoatVisualSystem visualSystem;
    
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
        
        baseMass = rb.mass;
        effectiveBuoyancyForce = buoyancyForce;
        
        RefreshComponentCache();
        lastKnownMass = CalculateTotalMassOptimized();
        
        float requiredBuoyancy = rb.mass * Physics2D.gravity.magnitude * 1.5f;
        if (effectiveBuoyancyForce < requiredBuoyancy)
        {
            effectiveBuoyancyForce = requiredBuoyancy;
            if (debugMassChanges)
            {
                Debug.Log($"AUTO-ADJUSTED: Buoyancy increased to {effectiveBuoyancyForce:F1} to support mass {rb.mass}");
            }
        }
        
        if (debugMassChanges)
        {
            Debug.Log($"BoatBuoyancy: Initial mass setup - Base: {baseMass}, Total: {lastKnownMass}, Buoyancy: {effectiveBuoyancyForce}");
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
    }
    
    public void UpdateBuoyancy()
    {
        if (enableDynamicBuoyancy)
        {
            UpdateDynamicBuoyancyOptimized();
        }
        
        ApplyBuoyancy();
    }
    
    private void ApplyBuoyancy()
    {
        int submergedPoints = 0;
        Vector2 totalForce = Vector2.zero;
    
        foreach (Transform point in floatPoints)
        {
            if (point == null) continue;
        
            Vector2 worldPos = point.position;
        
            float waterHeight = waterPhysics.GetWaterHeightAt(worldPos, this.transform);
            float submersion = waterHeight - worldPos.y;
        
            if (submersion > 0)
            {
                submergedPoints++;
            
                float speedFactor = 1f;
                if (rb.velocity.y > 0)
                {
                    BoatPhysicsSystem physicsSystem = GetComponent<BoatPhysicsSystem>();
                    if (physicsSystem != null)
                    {
                        speedFactor = Mathf.Lerp(1f, smoothBuoyancy, rb.velocity.y / 3f);
                    }
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
                effectiveBuoyancyForce = Mathf.Max(buoyancyForce, requiredBuoyancy);
                
                if (debugMassChanges)
                {
                    Debug.Log($"MASS UPDATE: Total: {currentTotalMass:F2}, Buoyancy: {effectiveBuoyancyForce:F1}");
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
            Debug.Log($"CACHE REFRESH: Found {cachedChildRigidbodies?.Count ?? 0} rigidbodies, {cachedEnemies?.Length ?? 0} enemies");
        }
    }
    
    public void RecalculateBuoyancy()
    {
        componentsCached = false;
        RefreshComponentCache();
        lastKnownMass = 0f;
        UpdateDynamicBuoyancyOptimized();
        
        if (debugMassChanges)
        {
            Debug.Log("BoatBuoyancy: Manual buoyancy recalculation triggered with cache refresh");
        }
    }
}
