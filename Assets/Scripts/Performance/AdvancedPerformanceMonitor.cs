using UnityEngine;
using UnityEngine.Profiling;
using System.Collections.Generic;

public class AdvancedPerformanceMonitor : PerformanceComponentBase
{
    public override string ComponentName => "Performance Monitor";
    
    [Header("Display Settings")]
    public bool showPerformanceStats = true;
    public int fontSize = 14;
    
    [Header("Performance Tracking")]
    public float updateInterval = 0.5f;
    public int frameHistorySize = 60;
    
    [Header("Warning Thresholds")]
    public float lowFPSWarning = 30f;
    public float highMemoryWarning = 100f; // MB
    public int highDrawCallWarning = 100;
    
    private float frameTime = 0f;
    private float fps = 0f;
    private Queue<float> frameHistory = new Queue<float>();
    private float nextUpdateTime = 0f;
    
    // Performance metrics
    private long usedMemory = 0;
    private long totalMemory = 0;
    private int drawCalls = 0;
    private int triangles = 0;
    private int vertices = 0;
    
    // Bottleneck detection
    private float cpuTime = 0f;
    private float gpuTime = 0f;
    private bool isBottlenecked = false;
    private string bottleneckType = "";

    // IPerformanceComponent implementation
    public override void Initialize()
    {
        frameHistory.Clear();
        nextUpdateTime = Time.time;
        showPerformanceStats = ShowDebugInfo;
        GameLogger.LogVerbose($"{ComponentName}: Initialized");
    }

    void Update()
    {
        if (!isEnabled) return;
        UpdateComponent();
    }
    
    public override void UpdateComponent()
    {
        UpdateFrameTracking();
        
        if (Time.time >= nextUpdateTime)
        {
            UpdatePerformanceMetrics();
            DetectBottlenecks();
            nextUpdateTime = Time.time + updateInterval;
        }
    }
    
    void UpdateFrameTracking()
    {
        frameTime += (Time.unscaledDeltaTime - frameTime) * 0.1f;
        fps = 1.0f / frameTime;
        
        // Track frame history
        frameHistory.Enqueue(fps);
        if (frameHistory.Count > frameHistorySize)
            frameHistory.Dequeue();
    }
    
    void UpdatePerformanceMetrics()
    {
        // Memory usage (Unity 2022.3 compatible)
        usedMemory = Profiler.GetTotalAllocatedMemory() / (1024 * 1024); // Convert to MB
        totalMemory = Profiler.GetTotalReservedMemory() / (1024 * 1024);
        
        // Rendering stats (approximated)
        drawCalls = GetApproximateDrawCalls();
        
        // GPU timing (approximated)
        UpdateGPUTiming();
    }
    
    int GetApproximateDrawCalls()
    {
        // Estimate based on active renderers
        Renderer[] renderers = FindObjectsOfType<Renderer>();
        int activeBatches = 0;
        
        foreach (Renderer r in renderers)
        {
            if (r.enabled && r.gameObject.activeInHierarchy)
                activeBatches++;
        }
        
        return activeBatches;
    }
    
    void UpdateGPUTiming()
    {
        cpuTime = Time.unscaledDeltaTime * 1000f; // Convert to ms
        
        // Approximate GPU time based on frame time and CPU time
        gpuTime = Mathf.Max(0f, (frameTime * 1000f) - cpuTime);
    }
    
    void DetectBottlenecks()
    {
        isBottlenecked = false;
        bottleneckType = "";
        
        if (fps < lowFPSWarning)
        {
            isBottlenecked = true;
            
            if (cpuTime > gpuTime * 1.5f)
                bottleneckType = "CPU BOUND";
            else if (gpuTime > cpuTime * 1.5f)
                bottleneckType = "GPU BOUND";
            else
                bottleneckType = "MIXED";
        }
        
        if (usedMemory > highMemoryWarning)
        {
            if (isBottlenecked)
                bottleneckType += " + MEMORY";
            else
            {
                isBottlenecked = true;
                bottleneckType = "MEMORY";
            }
        }
        
        if (drawCalls > highDrawCallWarning)
        {
            if (isBottlenecked)
                bottleneckType += " + DRAW CALLS";
            else
            {
                isBottlenecked = true;
                bottleneckType = "DRAW CALLS";
            }
        }
    }
    
    public override float RenderDebugGUI(float startY)
    {
        if (!ShowDebugInfo) return startY;
        
        GUIStyle normalStyle = GetDebugGUIStyle(fontSize, Color.white);
        GUIStyle warningStyle = GetDebugGUIStyle(fontSize, Color.red);
        warningStyle.fontStyle = FontStyle.Bold;
        
        float x = 30f; // Indent for section content
        float y = startY;
        float lineHeight = fontSize + 4f;
        
        // FPS with color coding
        Color fpsColor = GetFPSColor(fps);
        normalStyle.normal.textColor = fpsColor;
        GUI.Label(new Rect(x, y, 300, lineHeight), $"FPS: {fps:F1} ({frameTime * 1000f:F1}ms)", normalStyle);
        y += lineHeight;
        
        // FPS Stats
        normalStyle.normal.textColor = Color.white;
        if (frameHistory.Count > 0)
        {
            float minFPS = float.MaxValue;
            float maxFPS = float.MinValue;
            float avgFPS = 0f;
            
            foreach (float f in frameHistory)
            {
                minFPS = Mathf.Min(minFPS, f);
                maxFPS = Mathf.Max(maxFPS, f);
                avgFPS += f;
            }
            avgFPS /= frameHistory.Count;
            
            GUI.Label(new Rect(x, y, 300, lineHeight), $"FPS Range: {minFPS:F0}-{maxFPS:F0} (avg: {avgFPS:F0})", normalStyle);
            y += lineHeight;
        }
        
        // Memory
        Color memoryColor = usedMemory > highMemoryWarning ? Color.red : Color.white;
        normalStyle.normal.textColor = memoryColor;
        GUI.Label(new Rect(x, y, 300, lineHeight), $"Memory: {usedMemory}MB / {totalMemory}MB", normalStyle);
        y += lineHeight;
        
        // Draw Calls
        Color drawCallColor = drawCalls > highDrawCallWarning ? Color.red : Color.white;
        normalStyle.normal.textColor = drawCallColor;
        GUI.Label(new Rect(x, y, 300, lineHeight), $"Draw Calls: {drawCalls}", normalStyle);
        y += lineHeight;
        
        // Timing breakdown
        normalStyle.normal.textColor = Color.white;
        GUI.Label(new Rect(x, y, 300, lineHeight), $"CPU: {cpuTime:F1}ms | GPU: {gpuTime:F1}ms", normalStyle);
        y += lineHeight;
        
        // Bottleneck warning
        if (isBottlenecked)
        {
            y += lineHeight * 0.5f;
            GUI.Label(new Rect(x, y, 300, lineHeight), $"⚠️ BOTTLENECK: {bottleneckType}", warningStyle);
            y += lineHeight;
        }
        
        return y;
    }
    
    public override string GetPerformanceMetrics()
    {
        return $"FPS: {fps:F1} | Memory: {usedMemory}MB | Draw Calls: {drawCalls}" + 
               (isBottlenecked ? $" | BOTTLENECK: {bottleneckType}" : "");
    }
    
    Color GetFPSColor(float currentFPS)
    {
        if (currentFPS >= 60f) return Color.green;
        if (currentFPS >= 30f) return Color.yellow;
        return Color.red;
    }
    
    // Public methods for external monitoring
    public float GetCurrentFPS() => fps;
    public float GetAverageFPS()
    {
        if (frameHistory.Count == 0) return 0f;
        
        float sum = 0f;
        foreach (float f in frameHistory)
            sum += f;
        return sum / frameHistory.Count;
    }
    
    public long GetUsedMemoryMB() => usedMemory;
    public int GetDrawCalls() => drawCalls;
    public bool IsBottlenecked() => isBottlenecked;
    public string GetBottleneckType() => bottleneckType;
    
    // Performance recommendations
    public string GetPerformanceRecommendation()
    {
        if (!isBottlenecked) return "Performance is good!";
        
        if (bottleneckType.Contains("CPU"))
            return "Reduce Update() calls, optimize scripts, use object pooling";
        if (bottleneckType.Contains("GPU"))
            return "Reduce draw calls, optimize shaders, lower texture resolution";
        if (bottleneckType.Contains("MEMORY"))
            return "Reduce texture memory, implement object pooling, call GC.Collect()";
        if (bottleneckType.Contains("DRAW CALLS"))
            return "Enable SRP Batcher, use sprite atlases, reduce active renderers";
            
        return "Multiple bottlenecks detected - check all systems";
    }
    
    [ContextMenu("Force Garbage Collection")]
    public void ForceGarbageCollection()
    {
        System.GC.Collect();
        Resources.UnloadUnusedAssets();
        GameLogger.LogVerbose("Forced garbage collection");
    }
    
    [ContextMenu("Log Performance Report")]
    public void LogPerformanceReport()
    {
        GameLogger.LogVerbose($"=== PERFORMANCE REPORT ===");
        GameLogger.LogVerbose($"FPS: {fps:F1} (avg: {GetAverageFPS():F1})");
        GameLogger.LogVerbose($"Memory: {usedMemory}MB / {totalMemory}MB");
        GameLogger.LogVerbose($"Draw Calls: {drawCalls}");
        GameLogger.LogVerbose($"CPU Time: {cpuTime:F1}ms | GPU Time: {gpuTime:F1}ms");
        GameLogger.LogVerbose($"Bottleneck: {(isBottlenecked ? bottleneckType : "None")}");
        GameLogger.LogVerbose($"Recommendation: {GetPerformanceRecommendation()}");
    }
}
