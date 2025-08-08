using TMPro;
using UnityEngine;

public class SimpleFPSCounter : BaseDisplay
{
    [Header("FPS Settings")]
    [SerializeField] private bool showFPS = true;
    [SerializeField] private KeyCode toggleKey = KeyCode.F1;
    [SerializeField] private float updateInterval = 0.5f; // Update FPS text every X seconds
    
    [Header("Display Options")]
    [SerializeField] private bool showDetailedInfo = false;
    [SerializeField] private bool showMinMaxFPS = true;
    [SerializeField] private bool showFrameTime = false;
    [SerializeField] private bool showMemoryUsage = false;
    
    [Header("Colors")]
    [SerializeField] private Color excellentColor = Color.green;    // >= 60 FPS
    [SerializeField] private Color goodColor = Color.yellow;        // >= 45 FPS  
    [SerializeField] private Color okayColor = new Color(1f, 0.5f, 0f); // >= 30 FPS
    [SerializeField] private Color badColor = Color.red;            // < 30 FPS
    
    [Header("Performance Thresholds")]
    [SerializeField] private float excellentThreshold = 60f;
    [SerializeField] private float goodThreshold = 45f;
    [SerializeField] private float okayThreshold = 30f;
    
    [Header("References")]
    [SerializeField] private Canvas fpsCanvas;
    
    // Performance variables
    private float deltaTime = 0.0f;
    private float lastUpdateTime = 0f;
    private float currentFPS = 0f;
    private float minFPS = float.MaxValue;
    private float maxFPS = 0f;
    private int frameCount = 0;
    private float fpsSum = 0f;
    
    // Memory tracking
    private long lastMemoryUsage = 0;
    
    // String caching to reduce GC allocation
    private string cachedFPSText = "";
    private readonly System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder(128);

    protected override void Start()
    {
        base.Start();
        
        // Initialize canvas if not assigned
        if (fpsCanvas == null)
        {
            fpsCanvas = GetComponentInParent<Canvas>();
        }
        
        // Set initial visibility
        SetVisible(showFPS);
        
        // Reset tracking variables
        ResetFPSTracking();
        
        GameLogger.LogVerbose("SimpleFPSCounter initialized");
    }

    protected override void Update()
    {
        // Handle toggle input
        HandleInput();
        
        if (!showFPS || !CanUpdateDisplay()) return;
        
        // Update delta time with smoothing
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        currentFPS = 1.0f / deltaTime;
        
        // Track min/max FPS
        UpdateFPSTracking();
        
        // Update display at intervals to reduce overhead
        if (Time.unscaledTime - lastUpdateTime >= updateInterval)
        {
            lastUpdateTime = Time.unscaledTime;
            base.Update(); // This calls UpdateDisplay() and HandleCameraFacing()
        }
    }

    protected override void UpdateDisplay()
    {
        if (!CanUpdateDisplay()) return;
        
        // Build display text efficiently
        BuildDisplayText();
        
        // Update text and color
        SetDisplayText(cachedFPSText);
        UpdateDisplayColor();
    }

    private void HandleInput()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleFPS();
        }
        
        // Additional debug keys
        if (showFPS)
        {
            if (Input.GetKeyDown(KeyCode.F2))
            {
                ToggleDetailedInfo();
            }
            
            if (Input.GetKeyDown(KeyCode.F3))
            {
                ResetFPSTracking();
            }
        }
    }

    private void BuildDisplayText()
    {
        stringBuilder.Clear();
        
        // Basic FPS
        stringBuilder.AppendFormat("FPS: {0:F0}", currentFPS);
        
        if (showDetailedInfo)
        {
            // Frame time
            if (showFrameTime)
            {
                float frameTime = deltaTime * 1000f;
                stringBuilder.AppendFormat("\nFrame: {0:F1}ms", frameTime);
            }
            
            // Min/Max FPS
            if (showMinMaxFPS && frameCount > 10) // Only show after some frames
            {
                stringBuilder.AppendFormat("\nMin: {0:F0} | Max: {1:F0}", minFPS, maxFPS);
                
                float avgFPS = fpsSum / frameCount;
                stringBuilder.AppendFormat("\nAvg: {0:F0}", avgFPS);
            }
            
            // Memory usage
            if (showMemoryUsage)
            {
                long currentMemory = System.GC.GetTotalMemory(false);
                float memoryMB = currentMemory / 1024f / 1024f;
                stringBuilder.AppendFormat("\nMem: {0:F1}MB", memoryMB);
                lastMemoryUsage = currentMemory;
            }
        }
        
        cachedFPSText = stringBuilder.ToString();
    }

    private void UpdateDisplayColor()
    {
        Color targetColor;
        
        if (currentFPS >= excellentThreshold)
            targetColor = excellentColor;
        else if (currentFPS >= goodThreshold)
            targetColor = goodColor;
        else if (currentFPS >= okayThreshold)
            targetColor = okayColor;
        else
            targetColor = badColor;
        
        if (displayText != null)
            displayText.color = targetColor;
    }

    private void UpdateFPSTracking()
    {
        frameCount++;
        fpsSum += currentFPS;
        
        if (currentFPS < minFPS)
            minFPS = currentFPS;
        
        if (currentFPS > maxFPS)
            maxFPS = currentFPS;
    }

    #region Public Methods

    public void ToggleFPS()
    {
        showFPS = !showFPS;
        SetVisible(showFPS);
        GameLogger.Log($"FPS Counter {(showFPS ? "enabled" : "disabled")}");
    }

    public void ToggleDetailedInfo()
    {
        showDetailedInfo = !showDetailedInfo;
        GameLogger.Log($"FPS Detailed Info {(showDetailedInfo ? "enabled" : "disabled")}");
    }

    public void SetVisible(bool visible)
    {
        showFPS = visible;
        
        if (fpsCanvas != null)
        {
            fpsCanvas.gameObject.SetActive(visible);
        }
        else if (gameObject != null)
        {
            gameObject.SetActive(visible);
        }
    }

    public void ResetFPSTracking()
    {
        minFPS = float.MaxValue;
        maxFPS = 0f;
        frameCount = 0;
        fpsSum = 0f;
        GameLogger.LogVerbose("FPS tracking reset");
    }

    public void SetUpdateInterval(float interval)
    {
        updateInterval = Mathf.Max(0.1f, interval);
    }

    #endregion

    #region Context Menu Methods

    [ContextMenu("Toggle FPS Display")]
    private void ContextToggleFPS()
    {
        ToggleFPS();
    }

    [ContextMenu("Toggle Detailed Info")]
    private void ContextToggleDetailedInfo()
    {
        ToggleDetailedInfo();
    }

    [ContextMenu("Reset FPS Tracking")]
    private void ContextResetTracking()
    {
        ResetFPSTracking();
    }

    [ContextMenu("Log Current Performance")]
    private void LogCurrentPerformance()
    {
        float avgFPS = frameCount > 0 ? fpsSum / frameCount : 0f;
        
        string performanceReport = $"=== PERFORMANCE REPORT ===\n" +
                                 $"Current FPS: {currentFPS:F1}\n" +
                                 $"Average FPS: {avgFPS:F1}\n" +
                                 $"Min FPS: {minFPS:F1}\n" +
                                 $"Max FPS: {maxFPS:F1}\n" +
                                 $"Frame Time: {deltaTime * 1000f:F2}ms\n" +
                                 $"Frames Tracked: {frameCount}";
        
        if (showMemoryUsage)
        {
            long currentMemory = System.GC.GetTotalMemory(false);
            performanceReport += $"\nMemory: {currentMemory / 1024f / 1024f:F1}MB";
        }
        
        Debug.Log(performanceReport);
    }

    #endregion

    #region Performance Monitoring

    /// <summary>
    /// Get current FPS value
    /// </summary>
    public float GetCurrentFPS() => currentFPS;

    /// <summary>
    /// Get average FPS since last reset
    /// </summary>
    public float GetAverageFPS() => frameCount > 0 ? fpsSum / frameCount : 0f;

    /// <summary>
    /// Get minimum FPS recorded
    /// </summary>
    public float GetMinFPS() => minFPS == float.MaxValue ? 0f : minFPS;

    /// <summary>
    /// Get maximum FPS recorded
    /// </summary>
    public float GetMaxFPS() => maxFPS;

    /// <summary>
    /// Check if performance is below threshold
    /// </summary>
    public bool IsPerformancePoor() => currentFPS < okayThreshold;

    /// <summary>
    /// Get performance quality rating (0-3: Bad, Okay, Good, Excellent)
    /// </summary>
    public int GetPerformanceRating()
    {
        if (currentFPS >= excellentThreshold) return 3;
        if (currentFPS >= goodThreshold) return 2;
        if (currentFPS >= okayThreshold) return 1;
        return 0;
    }

    #endregion

    private void OnValidate()
    {
        // Ensure thresholds are logical
        excellentThreshold = Mathf.Max(1f, excellentThreshold);
        goodThreshold = Mathf.Min(excellentThreshold - 1f, goodThreshold);
        okayThreshold = Mathf.Min(goodThreshold - 1f, okayThreshold);
        
        updateInterval = Mathf.Max(0.1f, updateInterval);
    }
}
