using UnityEngine;
using UnityEngine.Profiling;
using System.Collections.Generic;
using System.Linq;

public class MemoryOptimizer : PerformanceComponentBase
{
    public override string ComponentName => "Memory Optimizer";
    
    [Header("Memory Monitoring")]
    public bool enableAutomaticCleanup = true;
    public float cleanupInterval = 30f; // seconds
    public long memoryWarningThreshold = 100; // MB
    public long memoryCriticalThreshold = 200; // MB
    
    [Header("Texture Memory")]
    public bool optimizeTextureMemory = true;
    public bool unloadUnusedTextures = true;
    public int maxTextureMemoryMB = 50;
    
    [Header("Object Pooling Detection")]
    public bool analyzeInstantiation = true;
    public bool detectMemoryLeaks = true;
    public int objectLeakThreshold = 100;
    
    [Header("Garbage Collection")]
    public bool enableSmartGC = true;
    public float gcInterval = 10f; // seconds
    public bool gcOnSceneLoad = true;
    
    // Memory tracking
    private long currentMemoryUsage = 0;
    private long peakMemoryUsage = 0;
    private long textureMemoryUsage = 0;
    private float lastCleanupTime = 0f;
    private float lastGCTime = 0f;
    
    // Cached object references to avoid repeated FindObjectsOfType calls
    private GameObject[] cachedGameObjects;
    private SpriteRenderer[] cachedSpriteRenderers;
    private float lastObjectCacheTime = 0f;
    private const float CACHE_REFRESH_INTERVAL = 2f; // Refresh cache every 2 seconds
    
    // Object tracking
    private Dictionary<string, int> objectCounts = new Dictionary<string, int>();
    private Dictionary<string, int> previousObjectCounts = new Dictionary<string, int>();
    private List<string> memoryLeakCandidates = new List<string>();
    
    // Performance metrics
    public struct MemoryMetrics
    {
        public long totalMemory;
        public long usedMemory;
        public long textureMemory;
        public int totalGameObjects;
        public int activeSpriteRenderers;
        public string memoryStatus;
        public List<string> recommendations;
    }
    
    private MemoryMetrics lastMetrics;
    
    public override void Initialize()
    {
        lastCleanupTime = Time.time;
        lastGCTime = Time.time;
        
        AnalyzeMemoryUsage();
        
        if (enableAutomaticCleanup)
        {
            InvokeRepeating(nameof(PerformMemoryCleanup), cleanupInterval, cleanupInterval);
        }
        
        Debug.Log($"{ComponentName}: Initialized - Current memory: {currentMemoryUsage}MB");
    }
    
    void Update()
    {
        if (!isEnabled) return;
        
        UpdateComponent();
    }
    
    public override void UpdateComponent()
    {
        // Update memory metrics
        currentMemoryUsage = Profiler.GetTotalAllocatedMemory() / (1024 * 1024);
        peakMemoryUsage = (long)Mathf.Max(peakMemoryUsage, currentMemoryUsage);
        
        // Check for memory warnings
        CheckMemoryWarnings();
        
        // Smart garbage collection
        if (enableSmartGC && Time.time - lastGCTime > gcInterval)
        {
            if (ShouldTriggerGC())
            {
                PerformSmartGarbageCollection();
                lastGCTime = Time.time;
            }
        }
        
        // Analyze object counts for memory leaks
        if (analyzeInstantiation && Time.time - lastCleanupTime > 5f)
        {
            AnalyzeObjectCounts();
        }
    }
    
    /// <summary>
    /// Perform comprehensive memory analysis
    /// </summary>
    [ContextMenu("Analyze Memory Usage")]
    public void AnalyzeMemoryUsage()
    {
        // Update current metrics
        currentMemoryUsage = Profiler.GetTotalAllocatedMemory() / (1024 * 1024);
        long totalMemory = Profiler.GetTotalReservedMemory() / (1024 * 1024);
        
        // Analyze texture memory
        textureMemoryUsage = AnalyzeTextureMemory();
        
        // Update cached objects periodically to avoid expensive calls every frame
        RefreshObjectCacheIfNeeded();
        
        // Count GameObjects using cached references
        int totalGameObjects = cachedGameObjects?.Length ?? 0;
        int activeSpriteRenderers = cachedSpriteRenderers?.Count(sr => sr != null && sr.enabled) ?? 0;
        
        // Generate status and recommendations
        string memoryStatus = GetMemoryStatus(currentMemoryUsage);
        List<string> recommendations = GenerateMemoryRecommendations();
        
        lastMetrics = new MemoryMetrics
        {
            totalMemory = totalMemory,
            usedMemory = currentMemoryUsage,
            textureMemory = textureMemoryUsage,
            totalGameObjects = totalGameObjects,
            activeSpriteRenderers = activeSpriteRenderers,
            memoryStatus = memoryStatus,
            recommendations = recommendations
        };
        
        LogMemoryAnalysis();
    }
    
    long AnalyzeTextureMemory()
    {
        long textureMemory = 0;
        
        // Find all textures in memory
        Texture2D[] allTextures = Resources.FindObjectsOfTypeAll<Texture2D>();
        
        foreach (var texture in allTextures)
        {
            if (texture != null)
            {
                // Estimate texture memory usage
                int pixelCount = texture.width * texture.height;
                int bytesPerPixel = GetBytesPerPixel(texture.format);
                textureMemory += (pixelCount * bytesPerPixel) / (1024 * 1024); // Convert to MB
            }
        }
        
        return textureMemory;
    }
    
    int GetBytesPerPixel(TextureFormat format)
    {
        switch (format)
        {
            case TextureFormat.RGBA32:
            case TextureFormat.ARGB32:
                return 4;
            case TextureFormat.RGB24:
                return 3;
            case TextureFormat.RGBA4444:
            case TextureFormat.RGB565:
                return 2;
            case TextureFormat.Alpha8:
                return 1;
            case TextureFormat.DXT1:
                return 1; // Compressed
            case TextureFormat.DXT5:
                return 1; // Compressed
            default:
                return 4; // Default assumption
        }
    }
    
    void AnalyzeObjectCounts()
    {
        // Store previous counts
        previousObjectCounts = new Dictionary<string, int>(objectCounts);
        objectCounts.Clear();
        
        // Count current objects by type
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        
        foreach (var obj in allObjects)
        {
            string typeName = obj.name;
            
            // Group similar objects (remove numbers/instances)
            if (typeName.Contains("(") && typeName.Contains(")"))
            {
                typeName = typeName.Substring(0, typeName.IndexOf("(")).Trim();
            }
            
            if (objectCounts.ContainsKey(typeName))
                objectCounts[typeName]++;
            else
                objectCounts[typeName] = 1;
        }
        
        // Detect potential memory leaks
        DetectMemoryLeaks();
    }
    
    void DetectMemoryLeaks()
    {
        memoryLeakCandidates.Clear();
        
        foreach (var current in objectCounts)
        {
            if (previousObjectCounts.ContainsKey(current.Key))
            {
                int previousCount = previousObjectCounts[current.Key];
                int growth = current.Value - previousCount;
                
                // If object count grew significantly and is above threshold
                if (growth > 10 && current.Value > objectLeakThreshold)
                {
                    memoryLeakCandidates.Add($"{current.Key}: {previousCount} → {current.Value} (+{growth})");
                }
            }
        }
        
        if (memoryLeakCandidates.Count > 0)
        {
            Debug.LogWarning($"MemoryOptimizer: Potential memory leaks detected:\n{string.Join("\n", memoryLeakCandidates)}");
        }
    }
    
    void CheckMemoryWarnings()
    {
        if (currentMemoryUsage > memoryCriticalThreshold)
        {
            Debug.LogError($"CRITICAL MEMORY USAGE: {currentMemoryUsage}MB (Threshold: {memoryCriticalThreshold}MB)");
            
            // Emergency cleanup
            PerformEmergencyCleanup();
        }
        else if (currentMemoryUsage > memoryWarningThreshold)
        {
            Debug.LogWarning($"HIGH MEMORY USAGE: {currentMemoryUsage}MB (Threshold: {memoryWarningThreshold}MB)");
        }
    }
    
    bool ShouldTriggerGC()
    {
        // Trigger GC if memory usage is high or growing rapidly
        return currentMemoryUsage > memoryWarningThreshold || 
               (currentMemoryUsage > peakMemoryUsage * 0.8f);
    }
    
    /// <summary>
    /// Perform memory cleanup
    /// </summary>
    [ContextMenu("Perform Memory Cleanup")]
    public void PerformMemoryCleanup()
    {
        long memoryBefore = currentMemoryUsage;
        
        // Unload unused assets
        if (unloadUnusedTextures)
        {
            Resources.UnloadUnusedAssets();
        }
        
        // Force garbage collection
        PerformSmartGarbageCollection();
        
        // Update memory usage
        currentMemoryUsage = Profiler.GetTotalAllocatedMemory() / (1024 * 1024);
        
        long memorySaved = memoryBefore - currentMemoryUsage;
        Debug.Log($"MemoryOptimizer: Cleanup completed. Memory freed: {memorySaved}MB");
        
        lastCleanupTime = Time.time;
    }
    
    void PerformSmartGarbageCollection()
    {
        // Only perform GC if it's likely to be beneficial
        if (currentMemoryUsage > 20) // Only if using more than 20MB
        {
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
            System.GC.Collect();
        }
    }
    
    void PerformEmergencyCleanup()
    {
        Debug.Log("MemoryOptimizer: Performing emergency cleanup!");
        
        // Aggressive cleanup
        Resources.UnloadUnusedAssets();
        System.GC.Collect();
        
        // Disable non-essential components temporarily
        DisableNonEssentialComponents();
        
        // Clear any large caches if they exist
        ClearObjectCaches();
    }
    
    void DisableNonEssentialComponents()
    {
        // Disable particle systems temporarily
        ParticleSystem[] particles = FindObjectsOfType<ParticleSystem>();
        foreach (var ps in particles)
        {
            if (ps.isPlaying)
            {
                ps.Stop();
            }
        }
        
        // Disable some sprite renderers that are far from camera
        SpriteRenderer[] sprites = FindObjectsOfType<SpriteRenderer>();
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            foreach (var sr in sprites)
            {
                float distance = Vector3.Distance(sr.transform.position, mainCam.transform.position);
                if (distance > 50f) // Disable distant sprites
                {
                    sr.enabled = false;
                }
            }
        }
    }
    
    void ClearObjectCaches()
    {
        // Clear any object pools if they exist
        var poolers = FindObjectsOfType<MonoBehaviour>().Where(mb => mb.GetType().Name.Contains("Pool"));
        
        foreach (var pooler in poolers)
        {
            // Try to call Clear method if it exists
            var clearMethod = pooler.GetType().GetMethod("Clear");
            if (clearMethod != null)
            {
                clearMethod.Invoke(pooler, null);
                Debug.Log($"Cleared object pool: {pooler.GetType().Name}");
            }
        }
    }
    
    string GetMemoryStatus(long memoryMB)
    {
        if (memoryMB < 50) return "GOOD";
        if (memoryMB < 100) return "MODERATE";
        if (memoryMB < 200) return "HIGH";
        return "CRITICAL";
    }
    
    List<string> GenerateMemoryRecommendations()
    {
        List<string> recommendations = new List<string>();
        
        if (textureMemoryUsage > maxTextureMemoryMB)
        {
            recommendations.Add($"Texture memory too high ({textureMemoryUsage}MB). Compress textures or use atlases");
        }
        
        if (lastMetrics.activeSpriteRenderers > 500)
        {
            recommendations.Add($"Too many active sprite renderers ({lastMetrics.activeSpriteRenderers}). Enable culling");
        }
        
        if (memoryLeakCandidates.Count > 0)
        {
            recommendations.Add($"Potential memory leaks detected in {memoryLeakCandidates.Count} object types");
        }
        
        if (currentMemoryUsage > memoryWarningThreshold)
        {
            recommendations.Add("Consider implementing object pooling for frequently created objects");
        }
        
        return recommendations;
    }
    
    void LogMemoryAnalysis()
    {
        Debug.Log("=== MEMORY ANALYSIS RESULTS ===");
        Debug.Log($"Total Memory: {lastMetrics.totalMemory}MB");
        Debug.Log($"Used Memory: {lastMetrics.usedMemory}MB ({lastMetrics.memoryStatus})");
        Debug.Log($"Texture Memory: {lastMetrics.textureMemory}MB");
        Debug.Log($"Peak Memory: {peakMemoryUsage}MB");
        Debug.Log($"Total GameObjects: {lastMetrics.totalGameObjects}");
        Debug.Log($"Active Sprite Renderers: {lastMetrics.activeSpriteRenderers}");
        
        if (lastMetrics.recommendations.Count > 0)
        {
            Debug.Log("Recommendations:");
            foreach (var rec in lastMetrics.recommendations)
            {
                Debug.Log($"  • {rec}");
            }
        }
        
        if (memoryLeakCandidates.Count > 0)
        {
            Debug.Log("Potential Memory Leaks:");
            foreach (var leak in memoryLeakCandidates)
            {
                Debug.Log($"  • {leak}");
            }
        }
    }
    
    /// <summary>
    /// Refresh cached object references if enough time has passed to avoid expensive searches every frame
    /// </summary>
    void RefreshObjectCacheIfNeeded()
    {
        if (Time.time - lastObjectCacheTime >= CACHE_REFRESH_INTERVAL)
        {
            cachedGameObjects = FindObjectsOfType<GameObject>();
            cachedSpriteRenderers = FindObjectsOfType<SpriteRenderer>();
            lastObjectCacheTime = Time.time;
            
            if (enableAutomaticCleanup)
            {
                Debug.Log($"MemoryOptimizer: Object cache refreshed - {cachedGameObjects.Length} GameObjects, {cachedSpriteRenderers.Length} SpriteRenderers");
            }
        }
    }
    
    public override float RenderDebugGUI(float startY)
    {
        if (!ShowDebugInfo) return startY;
        
        GUIStyle normalStyle = GetDebugGUIStyle(14, Color.white);
        GUIStyle warningStyle = GetDebugGUIStyle(14, Color.red);
        GUIStyle goodStyle = GetDebugGUIStyle(14, Color.green);
        
        float x = 30f;
        float y = startY;
        float lineHeight = 20f;
        
        // Memory status
        Color memoryColor = GetMemoryStatusColor(currentMemoryUsage);
        normalStyle.normal.textColor = memoryColor;
        GUI.Label(new Rect(x, y, 400, lineHeight), $"Memory: {currentMemoryUsage}MB ({lastMetrics.memoryStatus})", normalStyle);
        y += lineHeight;
        
        // Peak memory
        normalStyle.normal.textColor = Color.white;
        GUI.Label(new Rect(x, y, 400, lineHeight), $"Peak: {peakMemoryUsage}MB | Texture: {textureMemoryUsage}MB", normalStyle);
        y += lineHeight;
        
        // Object counts
        GUI.Label(new Rect(x, y, 400, lineHeight), $"GameObjects: {lastMetrics.totalGameObjects} | Sprites: {lastMetrics.activeSpriteRenderers}", normalStyle);
        y += lineHeight;
        
        // Memory leaks warning
        if (memoryLeakCandidates.Count > 0)
        {
            warningStyle.normal.textColor = Color.red;
            GUI.Label(new Rect(x, y, 400, lineHeight), $"⚠ {memoryLeakCandidates.Count} potential memory leaks detected", warningStyle);
            y += lineHeight;
        }
        
        // Auto-cleanup status
        if (enableAutomaticCleanup)
        {
            goodStyle.normal.textColor = Color.green;
            GUI.Label(new Rect(x, y, 400, lineHeight), $"✓ Auto-cleanup: {cleanupInterval}s intervals", goodStyle);
        }
        else
        {
            warningStyle.normal.textColor = Color.yellow;
            GUI.Label(new Rect(x, y, 400, lineHeight), "⚠ Auto-cleanup disabled", warningStyle);
        }
        y += lineHeight;
        
        return y;
    }
    
    Color GetMemoryStatusColor(long memoryMB)
    {
        if (memoryMB < 50) return Color.green;
        if (memoryMB < 100) return Color.yellow;
        if (memoryMB < 200) return Color.red;
        return Color.magenta; // Critical
    }
    
    public override string GetPerformanceMetrics()
    {
        return $"Memory: {currentMemoryUsage}MB ({lastMetrics.memoryStatus}) | Objects: {lastMetrics.totalGameObjects} | Leaks: {memoryLeakCandidates.Count}";
    }
    
    // Public API
    public long GetCurrentMemoryUsage() => currentMemoryUsage;
    public long GetPeakMemoryUsage() => peakMemoryUsage;
    public long GetTextureMemoryUsage() => textureMemoryUsage;
    public List<string> GetMemoryLeakCandidates() => new List<string>(memoryLeakCandidates);
    public MemoryMetrics GetLastMetrics() => lastMetrics;
    
    [ContextMenu("Force Emergency Cleanup")]
    public void ForceEmergencyCleanup() => PerformEmergencyCleanup();
    
    void OnDestroy()
    {
        // Clean up on destroy
        if (enableAutomaticCleanup)
        {
            CancelInvoke(nameof(PerformMemoryCleanup));
        }
    }
}
