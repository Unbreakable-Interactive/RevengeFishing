using UnityEngine;

public class ParticleOptimizer : PerformanceComponentBase
{
    public override string ComponentName => "Particle Optimizer";
    
    [Header("Particle Performance Settings")]
    public ParticleSystem[] particleSystems;
    public float maxDistance = 30f;
    public int maxParticles = 50;
    public int minParticles = 10;
    
    [Header("Distance-Based Optimization")]
    public bool enableDistanceOptimization = true;
    public Camera referenceCamera;
    
    [Header("Performance Limits")]
    [Range(0.1f, 1f)]
    public float qualityMultiplier = 0.5f;
    public bool disableWhenOffscreen = true;
    
    [Header("Debug")]
    public KeyCode toggleKey = KeyCode.F3;
    
    private ParticleSystemSettings[] originalSettings;
    
    [System.Serializable]
    public class ParticleSystemSettings
    {
        public ParticleSystem system;
        public int originalMaxParticles;
        public float originalEmissionRate;
        public bool originalEnabled;
        
        public ParticleSystemSettings(ParticleSystem ps)
        {
            system = ps;
            originalMaxParticles = ps.main.maxParticles;
            originalEmissionRate = ps.emission.rateOverTime.constant;
            originalEnabled = ps.emission.enabled;
        }
        
        public void RestoreOriginal()
        {
            if (system == null) return;
            
            var main = system.main;
            main.maxParticles = originalMaxParticles;
            
            var emission = system.emission;
            emission.rateOverTime = originalEmissionRate;
            emission.enabled = originalEnabled;
        }
        
        public void ApplyOptimized(float distanceRatio, float qualityMult)
        {
            if (system == null) return;
            
            var main = system.main;
            var emission = system.emission;
            
            int targetMaxParticles = Mathf.RoundToInt(originalMaxParticles * distanceRatio * qualityMult);
            targetMaxParticles = Mathf.Clamp(targetMaxParticles, 5, originalMaxParticles);
            main.maxParticles = targetMaxParticles;
            
            float targetEmissionRate = originalEmissionRate * distanceRatio * qualityMult;
            emission.rateOverTime = Mathf.Max(targetEmissionRate, 1f);
        }
        
        public void SetEnabled(bool enabled)
        {
            if (system == null) return;
            
            var emission = system.emission;
            emission.enabled = enabled;
        }
    }

    public override void Initialize()
    {
        if (referenceCamera == null)
            referenceCamera = Camera.main;
            
        InitializeParticleSystems();
    }

    void Start()
    {
        // Handled by base class
    }
    
    void InitializeParticleSystems()
    {
        if (particleSystems == null || particleSystems.Length == 0)
        {
            particleSystems = GetComponentsInChildren<ParticleSystem>();
        }
        
        originalSettings = new ParticleSystemSettings[particleSystems.Length];
        
        for (int i = 0; i < particleSystems.Length; i++)
        {
            if (particleSystems[i] != null)
            {
                originalSettings[i] = new ParticleSystemSettings(particleSystems[i]);
            }
        }
        
        GameLogger.LogVerbose($"ParticleOptimizer initialized with {particleSystems.Length} particle systems");
    }

    void Update()
    {
        if (!isEnabled) return;
        
        if (Input.GetKeyDown(toggleKey))
            ShowDebugInfo = !ShowDebugInfo;
            
        UpdateComponent();
    }
    
    public override void UpdateComponent()
    {
        if (enableDistanceOptimization)
        {
            OptimizeParticlesByDistance();
        }
    }
    
    void OptimizeParticlesByDistance()
    {
        if (referenceCamera == null) return;
        
        Vector3 cameraPos = referenceCamera.transform.position;
        float distance = Vector3.Distance(transform.position, cameraPos);
        
        float distanceRatio = Mathf.Clamp01(1f - (distance / maxDistance));
        
        bool isVisible = true;
        if (disableWhenOffscreen)
        {
            Plane[] planes = GeometryUtility.CalculateFrustumPlanes(referenceCamera);
            Bounds bounds = new Bounds(transform.position, Vector3.one * 5f);
            isVisible = GeometryUtility.TestPlanesAABB(planes, bounds);
        }
        
        for (int i = 0; i < originalSettings.Length; i++)
        {
            if (originalSettings[i]?.system == null) continue;
            
            if (!isVisible || distance > maxDistance)
            {
                originalSettings[i].SetEnabled(false);
            }
            else
            {
                originalSettings[i].SetEnabled(true);
                originalSettings[i].ApplyOptimized(distanceRatio, qualityMultiplier);
            }
        }
    }
    
    // OnGUI removed - handled by PerformanceManager composite GUI
    
    public override float RenderDebugGUI(float startY)
    {
        if (!ShowDebugInfo) return startY;
        
        GUIStyle normalStyle = GetDebugGUIStyle(14, Color.white);
        
        Vector3 cameraPos = referenceCamera ? referenceCamera.transform.position : Vector3.zero;
        float distance = Vector3.Distance(transform.position, cameraPos);
        
        float x = 30f; // Indent for section content
        float y = startY;
        float lineHeight = 20f;
        
        GUI.Label(new Rect(x, y, 300, lineHeight), $"Particle Systems: {particleSystems.Length}", normalStyle);
        y += lineHeight;
        GUI.Label(new Rect(x, y, 300, lineHeight), $"Distance to Camera: {distance:F1}m", normalStyle);
        y += lineHeight;
        GUI.Label(new Rect(x, y, 300, lineHeight), $"Quality Multiplier: {qualityMultiplier:F2}", normalStyle);
        y += lineHeight;
        
        int activeCount = 0;
        for (int i = 0; i < particleSystems.Length; i++)
        {
            if (particleSystems[i] != null && particleSystems[i].emission.enabled)
                activeCount++;
        }
        GUI.Label(new Rect(x, y, 300, lineHeight), $"Active Particles: {activeCount}/{particleSystems.Length}", normalStyle);
        y += lineHeight;
        
        return y;
    }
    
    public override string GetPerformanceMetrics()
    {
        int activeCount = 0;
        if (particleSystems != null && originalSettings != null)
        {
            for (int i = 0; i < particleSystems.Length; i++)
            {
                if (particleSystems[i] != null && particleSystems[i].emission.enabled)
                    activeCount++;
            }
        }
        
        Vector3 cameraPos = referenceCamera ? referenceCamera.transform.position : Vector3.zero;
        float distance = Vector3.Distance(transform.position, cameraPos);
        
        return $"Active: {activeCount}/{(particleSystems?.Length ?? 0)} | Dist: {distance:F1}m | Quality: {qualityMultiplier:F1}";
    }
    
    public void SetQualityMultiplier(float quality)
    {
        qualityMultiplier = Mathf.Clamp01(quality);
    }
    
    public void SetMaxDistance(float distance)
    {
        maxDistance = distance;
    }
    
    public void EnableAllParticles()
    {
        if (originalSettings == null) return;
        foreach (var setting in originalSettings)
        {
            setting?.SetEnabled(true);
        }
    }
    
    public void DisableAllParticles()
    {
        if (originalSettings == null) return;
        foreach (var setting in originalSettings)
        {
            setting?.SetEnabled(false);
        }
    }
    
    [ContextMenu("Restore Original Settings")]
    public void RestoreOriginalSettings()
    {
        if (originalSettings == null) return;
        foreach (var setting in originalSettings)
        {
            setting?.RestoreOriginal();
        }
    }
}
