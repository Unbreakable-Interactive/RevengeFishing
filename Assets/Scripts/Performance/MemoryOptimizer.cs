using UnityEngine;
using UnityEngine.Profiling;
using System.Collections.Generic;
using System.Linq;

public class MemoryOptimizer : PerformanceComponentBase
{
    public override string ComponentName => "Memory Optimizer";
    
    [Header("Memory Monitoring")]
    public bool enableAutomaticCleanup = true;
    public float cleanupInterval = 30f;
    public long memoryWarningThreshold = 100;
    public long memoryCriticalThreshold = 200;
    
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
    public float gcInterval = 10f;
    public bool gcOnSceneLoad = true;
    
    private long currentMemoryUsage = 0;
    private long peakMemoryUsage = 0;
    private long textureMemoryUsage = 0;
    private float lastCleanupTime = 0f;
    private float lastGCTime = 0f;
    
    private GameObject[] cachedGameObjects;
    private SpriteRenderer[] cachedSpriteRenderers;
    private float lastObjectCacheTime = 0f;
    private const float CACHE_REFRESH_INTERVAL = 2f;
    
    private Dictionary<string, int> objectCounts = new Dictionary<string, int>();
    private Dictionary<string, int> previousObjectCounts = new Dictionary<string, int>();
    private List<string> memoryLeakCandidates = new List<string>();
    
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
        
        GameLogger.LogVerbose($"{ComponentName}: Initialized - Current memory: {currentMemoryUsage}MB");
    }
    
    void Update()
    {
        if (!isEnabled) return;
        UpdateComponent();
    }
    
    public override void UpdateComponent()
    {
        currentMemoryUsage = Profiler.GetTotalAllocatedMemory() / (1024 * 1024);
        peakMemoryUsage = (long)Mathf.Max(peakMemoryUsage, currentMemoryUsage);
        
        CheckMemoryWarnings();
        
        if (enableSmartGC && Time.time - lastGCTime > gcInterval)
        {
            if (ShouldTriggerGC())
            {
                PerformSmartGarbageCollection();
                lastGCTime = Time.time;
            }
        }
        
        if (analyzeInstantiation && Time.time - lastCleanupTime > 5f)
        {
            AnalyzeObjectCounts();
        }
    }
    
    [ContextMenu("Analyze Memory Usage")]
    public void AnalyzeMemoryUsage()
    {
        currentMemoryUsage = Profiler.GetTotalAllocatedMemory() / (1024 * 1024);
        long totalMemory = Profiler.GetTotalReservedMemory() / (1024 * 1024);
        
        textureMemoryUsage = AnalyzeTextureMemory();
        RefreshObjectCacheIfNeeded();
        
        int totalGameObjects = cachedGameObjects?.Length ?? 0;
        int activeSpriteRenderers = cachedSpriteRenderers?.Count(sr => sr != null && sr.enabled) ?? 0;
        
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
        Texture2D[] allTextures = Resources.FindObjectsOfTypeAll<Texture2D>();
        
        foreach (var texture in allTextures)
        {
            if (texture != null)
            {
                int pixelCount = texture.width * texture.height;
                int bytesPerPixel = GetBytesPerPixel(texture.format);
                textureMemory += (pixelCount * bytesPerPixel) / (1024 * 1024);
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
            case TextureFormat.DXT5:
                return 1;
            default:
                return 4;
        }
    }
    
    void AnalyzeObjectCounts()
    {
        previousObjectCounts = new Dictionary<string, int>(objectCounts);
        objectCounts.Clear();
        
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        
        foreach (var obj in allObjects)
        {
            string typeName = obj.name;
            
            if (typeName.Contains("(") && typeName.Contains(")"))
            {
                typeName = typeName.Substring(0, typeName.IndexOf("(")).Trim();
            }
            
            if (objectCounts.ContainsKey(typeName))
                objectCounts[typeName]++;
            else
                objectCounts[typeName] = 1;
        }
        
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
                
                if (growth > 10 && current.Value > objectLeakThreshold)
                {
                    memoryLeakCandidates.Add($"{current.Key}: {previousCount} → {current.Value} (+{growth})");
                }
            }
        }
        
        if (memoryLeakCandidates.Count > 0)
        {
            GameLogger.LogWarning($"MemoryOptimizer: Potential memory leaks detected:\n{string.Join("\n", memoryLeakCandidates)}");
        }
    }
    
    void CheckMemoryWarnings()
    {
        if (currentMemoryUsage > memoryCriticalThreshold)
        {
            GameLogger.LogError($"CRITICAL MEMORY USAGE: {currentMemoryUsage}MB (Threshold: {memoryCriticalThreshold}MB)");
            PerformEmergencyCleanup();
        }
        else if (currentMemoryUsage > memoryWarningThreshold)
        {
            GameLogger.LogWarning($"HIGH MEMORY USAGE: {currentMemoryUsage}MB (Threshold: {memoryWarningThreshold}MB)");
        }
    }
    
    bool ShouldTriggerGC()
    {
        return currentMemoryUsage > memoryWarningThreshold || 
               (currentMemoryUsage > peakMemoryUsage * 0.8f);
    }
    
    [ContextMenu("Perform Memory Cleanup")]
    public void PerformMemoryCleanup()
    {
        long memoryBefore = currentMemoryUsage;
        
        if (unloadUnusedTextures)
        {
            Resources.UnloadUnusedAssets();
        }
        
        PerformSmartGarbageCollection();
        
        currentMemoryUsage = Profiler.GetTotalAllocatedMemory() / (1024 * 1024);
        
        long memorySaved = memoryBefore - currentMemoryUsage;
        GameLogger.LogVerbose($"MemoryOptimizer: Cleanup completed. Memory freed: {memorySaved}MB");
        
        lastCleanupTime = Time.time;
    }
    
    void PerformSmartGarbageCollection()
    {
        if (currentMemoryUsage > 20)
        {
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
            System.GC.Collect();
        }
    }
    
    void PerformEmergencyCleanup()
    {
        GameLogger.LogWarning("MemoryOptimizer: Performing emergency cleanup!");
        
        Resources.UnloadUnusedAssets();
        System.GC.Collect();
        
        DisableNonEssentialComponents();
        ClearObjectCaches();
    }
    
    void DisableNonEssentialComponents()
    {
        ParticleSystem[] particles = FindObjectsOfType<ParticleSystem>();
        foreach (var ps in particles)
        {
            if (ps.isPlaying)
            {
                ps.Stop();
            }
        }
        
        SpriteRenderer[] sprites = FindObjectsOfType<SpriteRenderer>();
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            foreach (var sr in sprites)
            {
                float distance = Vector3.Distance(sr.transform.position, mainCam.transform.position);
                if (distance > 50f)
                {
                    sr.enabled = false;
                }
            }
        }
    }
    
    void ClearObjectCaches()
    {
        var poolers = FindObjectsOfType<MonoBehaviour>().Where(mb => mb.GetType().Name.Contains("Pool"));
        
        foreach (var pooler in poolers)
        {
            var clearMethod = pooler.GetType().GetMethod("Clear");
            if (clearMethod != null)
            {
                clearMethod.Invoke(pooler, null);
                GameLogger.LogVerbose($"Cleared object pool: {pooler.GetType().Name}");
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
        GameLogger.LogVerbose("=== MEMORY ANALYSIS RESULTS ===");
        GameLogger.LogVerbose($"Total Memory: {lastMetrics.totalMemory}MB");
        GameLogger.LogVerbose($"Used Memory: {lastMetrics.usedMemory}MB ({lastMetrics.memoryStatus})");
        GameLogger.LogVerbose($"Texture Memory: {lastMetrics.textureMemory}MB");
        GameLogger.LogVerbose($"Peak Memory: {peakMemoryUsage}MB");
        GameLogger.LogVerbose($"Total GameObjects: {lastMetrics.totalGameObjects}");
        GameLogger.LogVerbose($"Active Sprite Renderers: {lastMetrics.activeSpriteRenderers}");
        
        if (lastMetrics.recommendations.Count > 0)
        {
            GameLogger.LogVerbose("Recommendations:");
            foreach (var rec in lastMetrics.recommendations)
            {
                GameLogger.LogVerbose($"  • {rec}");
            }
        }
        
        if (memoryLeakCandidates.Count > 0)
        {
            GameLogger.LogVerbose("Potential Memory Leaks:");
            foreach (var leak in memoryLeakCandidates)
            {
                GameLogger.LogVerbose($"  • {leak}");
            }
        }
    }
    
    void RefreshObjectCacheIfNeeded()
    {
        if (Time.time - lastObjectCacheTime >= CACHE_REFRESH_INTERVAL)
        {
            cachedGameObjects = FindObjectsOfType<GameObject>();
            cachedSpriteRenderers = FindObjectsOfType<SpriteRenderer>();
            lastObjectCacheTime = Time.time;
            
            if (enableAutomaticCleanup)
            {
                GameLogger.LogVerbose($"MemoryOptimizer: Object cache refreshed - {cachedGameObjects.Length} GameObjects, {cachedSpriteRenderers.Length} SpriteRenderers");
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
        
        Color memoryColor = GetMemoryStatusColor(currentMemoryUsage);
        normalStyle.normal.textColor = memoryColor;
        GUI.Label(new Rect(x, y, 400, lineHeight), $"Memory: {currentMemoryUsage}MB ({lastMetrics.memoryStatus})", normalStyle);
        y += lineHeight;
        
        normalStyle.normal.textColor = Color.white;
        GUI.Label(new Rect(x, y, 400, lineHeight), $"Peak: {peakMemoryUsage}MB | Texture: {textureMemoryUsage}MB", normalStyle);
        y += lineHeight;
        
        GUI.Label(new Rect(x, y, 400, lineHeight), $"GameObjects: {lastMetrics.totalGameObjects} | Sprites: {lastMetrics.activeSpriteRenderers}", normalStyle);
        y += lineHeight;
        
        if (memoryLeakCandidates.Count > 0)
        {
            warningStyle.normal.textColor = Color.red;
            GUI.Label(new Rect(x, y, 400, lineHeight), $"⚠ {memoryLeakCandidates.Count} potential memory leaks detected", warningStyle);
            y += lineHeight;
        }
        
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
        return Color.magenta;
    }
    
    public override string GetPerformanceMetrics()
    {
        return $"Memory: {currentMemoryUsage}MB ({lastMetrics.memoryStatus}) | Objects: {lastMetrics.totalGameObjects} | Leaks: {memoryLeakCandidates.Count}";
    }
    
    public long GetCurrentMemoryUsage() => currentMemoryUsage;
    public long GetPeakMemoryUsage() => peakMemoryUsage;
    public long GetTextureMemoryUsage() => textureMemoryUsage;
    public List<string> GetMemoryLeakCandidates() => new List<string>(memoryLeakCandidates);
    public MemoryMetrics GetLastMetrics() => lastMetrics;
    
    [ContextMenu("Force Emergency Cleanup")]
    public void ForceEmergencyCleanup() => PerformEmergencyCleanup();
    
    void OnDestroy()
    {
        if (enableAutomaticCleanup)
        {
            CancelInvoke(nameof(PerformMemoryCleanup));
        }
    }
}
