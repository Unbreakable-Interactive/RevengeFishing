using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// Composite Performance Manager - Controls all performance monitoring and optimization components
/// </summary>
public class PerformanceManager : MonoBehaviour
{
    [Header("Performance Manager Settings")]
    [SerializeField] private bool enableInBuildMode = false;
    [SerializeField] private bool enableInDebugMode = true;
    [SerializeField] private bool lightweightMode = true; // NEW: Only FPS monitoring when true
    [SerializeField] private bool showAllDebugInfo = false;
    
    [Header("Key Bindings")]
    public KeyCode toggleAllPerformanceSystemsKey = KeyCode.Tab;
    
    [Header("Component References")]
    [SerializeField] private List<MonoBehaviour> componentReferences = new List<MonoBehaviour>();
    
    [Header("Runtime Info")]
    [SerializeField, TextArea(3, 5)] private string runtimeInfo = "";
    
    // Component management
    private List<IPerformanceComponent> performanceComponents = new List<IPerformanceComponent>();
    private Dictionary<System.Type, IPerformanceComponent> componentMap = new Dictionary<System.Type, IPerformanceComponent>();
    
    // State management
    private bool isInitialized = false;
    private bool debugMode = false;
    private float updateInterval = 0.5f;
    private float nextUpdateTime = 0f;
    
    #region Unity Lifecycle
    
    private void Awake()
    {
        // Determine if we should be active
        debugMode = Debug.isDebugBuild ? enableInDebugMode : enableInBuildMode;
        
        if (!debugMode)
        {
            gameObject.SetActive(false);
            return;
        }
        
        // Auto-discover performance components
        DiscoverComponents();
    }
    
    private void Start()
    {
        if (debugMode)
        {
            InitializeAllComponents();
        }
    }
    
    private void Update()
    {
        if (!debugMode || !isInitialized) return;
        
        HandleKeyInput();
        
        // Update components at interval
        if (Time.time >= nextUpdateTime)
        {
            UpdateAllComponents();
            UpdateRuntimeInfo();
            nextUpdateTime = Time.time + updateInterval;
        }
    }
    
    private void OnGUI()
    {
        if (!debugMode || !showAllDebugInfo) return;
        
        RenderCompositeDebugGUI();
    }
    
    #endregion
    
    #region Component Management
    
    /// <summary>
    /// Register a performance component with the manager
    /// </summary>
    public void RegisterComponent(IPerformanceComponent component)
    {
        if (component == null) return;
        
        if (!performanceComponents.Contains(component))
        {
            performanceComponents.Add(component);
            componentMap[component.GetType()] = component;
            
            // Add to reference list for inspector visibility
            if (component is MonoBehaviour mb && !componentReferences.Contains(mb))
            {
                componentReferences.Add(mb);
            }
            
            Debug.Log($"PerformanceManager: Registered component '{component.ComponentName}'");
        }
    }
    
    /// <summary>
    /// Unregister a performance component
    /// </summary>
    public void UnregisterComponent(IPerformanceComponent component)
    {
        if (component == null) return;
        
        performanceComponents.Remove(component);
        componentMap.Remove(component.GetType());
        
        if (component is MonoBehaviour mb)
        {
            componentReferences.Remove(mb);
        }
        
        Debug.Log($"PerformanceManager: Unregistered component '{component.ComponentName}'");
    }
    
    /// <summary>
    /// Get a specific performance component by type
    /// </summary>
    public T GetComponent<T>() where T : class, IPerformanceComponent
    {
        if (componentMap.TryGetValue(typeof(T), out IPerformanceComponent component))
        {
            return component as T;
        }
        return null;
    }
    
    /// <summary>
    /// Auto-discover performance components in the scene
    /// </summary>
    private void DiscoverComponents()
    {
        // Find all MonoBehaviours that implement IPerformanceComponent
        MonoBehaviour[] allComponents = FindObjectsOfType<MonoBehaviour>();
        
        foreach (MonoBehaviour mb in allComponents)
        {
            if (mb is IPerformanceComponent perfComponent)
            {
                RegisterComponent(perfComponent);
            }
        }
        
        Debug.Log($"PerformanceManager: Discovered {performanceComponents.Count} performance components");
    }
    
    #endregion
    
    #region Component Operations
    
    /// <summary>
    /// Initialize all registered components
    /// </summary>
    public void InitializeAllComponents()
    {
        foreach (var component in performanceComponents)
        {
            if (component.IsEnabled)
            {
                // In lightweight mode, only enable AdvancedPerformanceMonitor
                if (lightweightMode && !(component is AdvancedPerformanceMonitor))
                {
                    component.SetEnabled(false);
                    continue;
                }
                
                component.Initialize();
            }
        }
        
        isInitialized = true;
        Debug.Log($"PerformanceManager: Initialized {performanceComponents.Count} components (Lightweight: {lightweightMode})");
    }
    
    /// <summary>
    /// Update all enabled components
    /// </summary>
    private void UpdateAllComponents()
    {
        foreach (var component in performanceComponents)
        {
            if (component.IsEnabled)
            {
                // In lightweight mode, only update AdvancedPerformanceMonitor
                if (lightweightMode && !(component is AdvancedPerformanceMonitor))
                    continue;
                    
                component.UpdateComponent();
            }
        }
    }
    
    /// <summary>
    /// Enable all performance components
    /// </summary>
    public void EnableAllComponents()
    {
        foreach (var component in performanceComponents)
        {
            component.SetEnabled(true);
        }
        Debug.Log("PerformanceManager: Enabled all components");
    }
    
    /// <summary>
    /// Disable all performance components
    /// </summary>
    public void DisableAllComponents()
    {
        foreach (var component in performanceComponents)
        {
            component.SetEnabled(false);
        }
        Debug.Log("PerformanceManager: Disabled all components");
    }
    
    /// <summary>
    /// Reset all performance components to default state
    /// </summary>
    public void ResetAllComponents()
    {
        foreach (var component in performanceComponents)
        {
            component.Reset();
        }
        Debug.Log("PerformanceManager: Reset all components");
    }
    
    /// <summary>
    /// Toggle between lightweight (FPS only) and full debug mode
    /// </summary>
    public void ToggleLightweightMode()
    {
        lightweightMode = !lightweightMode;
        
        if (lightweightMode)
        {
            // Disable optimization components, keep only FPS monitoring
            foreach (var component in performanceComponents)
            {
                if (!(component is AdvancedPerformanceMonitor))
                {
                    component.SetEnabled(false);
                    component.ShowDebugInfo = false;
                }
            }
            Debug.Log("PerformanceManager: Switched to LIGHTWEIGHT mode (FPS only)");
        }
        else
        {
            // Re-enable all components for full debug mode
            foreach (var component in performanceComponents)
            {
                component.SetEnabled(true);
            }
            Debug.Log("PerformanceManager: Switched to FULL DEBUG mode");
        }
    }
    
    /// <summary>
    /// Toggle debug info for all components
    /// </summary>
    public void ToggleAllDebugInfo()
    {
        showAllDebugInfo = !showAllDebugInfo;
        
        foreach (var component in performanceComponents)
        {
            // In lightweight mode, only affect AdvancedPerformanceMonitor
            if (lightweightMode && !(component is AdvancedPerformanceMonitor))
                continue;
                
            component.ShowDebugInfo = showAllDebugInfo;
        }
        
        Debug.Log($"PerformanceManager: Debug info {(showAllDebugInfo ? "enabled" : "disabled")} (Lightweight: {lightweightMode})");
    }
    
    /// <summary>
    /// Set lightweight mode directly
    /// </summary>
    public void SetLightweightMode(bool enable)
    {
        if (lightweightMode != enable)
        {
            ToggleLightweightMode();
        }
    }
    
    #endregion
    
    #region Input Handling
    
    private void HandleKeyInput()
    {
        // Tab - Toggle ALL performance systems on/off
        if (Input.GetKeyDown(toggleAllPerformanceSystemsKey))
        {
            ToggleAllPerformanceSystems();
        }
    }
    
    /// <summary>
    /// Toggle all performance systems and their debug info with one key
    /// </summary>
    public void ToggleAllPerformanceSystems()
    {
        // Toggle show all debug info first
        showAllDebugInfo = !showAllDebugInfo;
        
        foreach (var component in performanceComponents)
        {
            // Enable/disable the component
            component.SetEnabled(showAllDebugInfo);
            
            // Enable/disable debug info
            component.ShowDebugInfo = showAllDebugInfo;
        }
        
        string status = showAllDebugInfo ? "ENABLED" : "DISABLED";
        Debug.Log($"PerformanceManager: ALL performance systems {status} (Tab key pressed)");
    }
    
    private void ToggleComponentDebug<T>() where T : class, IPerformanceComponent
    {
        var component = GetComponent<T>();
        if (component != null)
        {
            component.ShowDebugInfo = !component.ShowDebugInfo;
            Debug.Log($"PerformanceManager: Toggled debug for {component.ComponentName}: {component.ShowDebugInfo}");
        }
    }
    
    #endregion
    
    #region Debug GUI
    
    private void RenderCompositeDebugGUI()
    {
        // Create unified style guide
        GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = 20;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.normal.textColor = Color.cyan;
        
        GUIStyle sectionStyle = new GUIStyle(GUI.skin.label);
        sectionStyle.fontSize = 16;
        sectionStyle.fontStyle = FontStyle.Bold;
        sectionStyle.normal.textColor = Color.yellow;
        
        GUIStyle normalStyle = new GUIStyle(GUI.skin.label);
        normalStyle.fontSize = 14;
        normalStyle.normal.textColor = Color.white;
        
        GUIStyle helpStyle = new GUIStyle(GUI.skin.label);
        helpStyle.fontSize = 12;
        helpStyle.normal.textColor = Color.gray;
        
        // Layout settings
        float x = 10f;
        float y = 10f;
        float lineHeight = 20f;
        float sectionSpacing = lineHeight * 0.7f;
        float width = 450f;
        
        // === HEADER SECTION ===
        GUI.Label(new Rect(x, y, width, lineHeight + 4), "🚀 PERFORMANCE DASHBOARD", titleStyle);
        y += lineHeight * 1.8f;
        
        // Status overview
        int activeCount = performanceComponents.Count(c => c.IsEnabled);
        int debugCount = performanceComponents.Count(c => c.ShowDebugInfo);
        
        GUI.Label(new Rect(x, y, width, lineHeight), $"📊 Status: {activeCount}/{performanceComponents.Count} Active | {debugCount} Showing Debug", normalStyle);
        y += lineHeight + sectionSpacing;
        
        // === KEY BINDINGS SECTION ===
        GUI.Label(new Rect(x, y, width, lineHeight), "⌨️ CONTROLS", sectionStyle);
        y += lineHeight;
        
        GUI.Label(new Rect(x + 20, y, width, lineHeight), $"[{toggleAllPerformanceSystemsKey}] Toggle ALL Performance Systems", helpStyle);
        y += lineHeight + sectionSpacing;
        
        // === PERFORMANCE SECTIONS ===
        // Render each component in organized sections
        var perfMonitor = GetComponent<AdvancedPerformanceMonitor>();
        var culling = GetComponent<GameObjectCulling>();
        var particles = GetComponent<ParticleOptimizer>();
        var drawCallOpt = GetComponent<DrawCallOptimizer>();
        var memoryOpt = GetComponent<MemoryOptimizer>();
        
        // 1. PERFORMANCE MONITOR SECTION
        if (perfMonitor != null && perfMonitor.IsEnabled && perfMonitor.ShowDebugInfo)
        {
            GUI.Label(new Rect(x, y, width, lineHeight), "📈 PERFORMANCE MONITOR", sectionStyle);
            y += lineHeight;
            y = perfMonitor.RenderDebugGUI(y);
            y += sectionSpacing;
        }
        
        // 2. OBJECT CULLING SECTION  
        if (culling != null && culling.IsEnabled && culling.ShowDebugInfo)
        {
            GUI.Label(new Rect(x, y, width, lineHeight), "🎯 OBJECT CULLING", sectionStyle);
            y += lineHeight;
            y = culling.RenderDebugGUI(y);
            y += sectionSpacing;
        }
        
        // 3. PARTICLE OPTIMIZATION SECTION
        if (particles != null && particles.IsEnabled && particles.ShowDebugInfo)
        {
            GUI.Label(new Rect(x, y, width, lineHeight), "🎨 PARTICLE OPTIMIZER", sectionStyle);
            y += lineHeight;
            y = particles.RenderDebugGUI(y);
            y += sectionSpacing;
        }
        
        // 4. DRAW CALL OPTIMIZATION SECTION
        if (drawCallOpt != null && drawCallOpt.IsEnabled && drawCallOpt.ShowDebugInfo)
        {
            GUI.Label(new Rect(x, y, width, lineHeight), "🎯 DRAW CALL OPTIMIZER", sectionStyle);
            y += lineHeight;
            y = drawCallOpt.RenderDebugGUI(y);
            y += sectionSpacing;
        }
        
        // 5. MEMORY OPTIMIZATION SECTION
        if (memoryOpt != null && memoryOpt.IsEnabled && memoryOpt.ShowDebugInfo)
        {
            GUI.Label(new Rect(x, y, width, lineHeight), "💾 MEMORY OPTIMIZER", sectionStyle);
            y += lineHeight;
            y = memoryOpt.RenderDebugGUI(y);
            y += sectionSpacing;
        }
        
        // === SUMMARY SECTION ===
        if (activeCount > 0)
        {
            GUI.Label(new Rect(x, y, width, lineHeight), "📋 PERFORMANCE SUMMARY", sectionStyle);
            y += lineHeight;
            
            string summary = GetPerformanceSummary();
            if (!string.IsNullOrEmpty(summary))
            {
                // Split long summary into multiple lines for better readability
                string[] summaryParts = summary.Split('|');
                foreach (string part in summaryParts)
                {
                    GUI.Label(new Rect(x + 20, y, width, lineHeight), part.Trim(), normalStyle);
                    y += lineHeight;
                }
            }
        }
    }
    
    #endregion
    
    #region Runtime Info
    
    private void UpdateRuntimeInfo()
    {
        var activeComponents = performanceComponents.Where(c => c.IsEnabled).ToList();
        var metrics = activeComponents.Select(c => $"{c.ComponentName}: {c.GetPerformanceMetrics()}").ToArray();
        
        runtimeInfo = $"Performance Manager Status:\n" +
                     $"Debug Mode: {debugMode}\n" +
                     $"Active Components: {activeComponents.Count}/{performanceComponents.Count}\n" +
                     $"Show Debug Info: {showAllDebugInfo}\n\n" +
                     $"Component Metrics:\n{string.Join("\n", metrics)}";
    }
    
    #endregion
    
    #region Public API
    
    /// <summary>
    /// Get performance summary for all components
    /// </summary>
    public string GetPerformanceSummary()
    {
        var summary = performanceComponents
            .Where(c => c.IsEnabled)
            .Select(c => $"{c.ComponentName}: {c.GetPerformanceMetrics()}")
            .ToArray();
            
        return string.Join(" | ", summary);
    }
    
    /// <summary>
    /// Set update interval for component updates
    /// </summary>
    public void SetUpdateInterval(float interval)
    {
        updateInterval = Mathf.Max(0.1f, interval);
    }
    
    /// <summary>
    /// Force immediate update of all components
    /// </summary>
    public void ForceUpdateAll()
    {
        UpdateAllComponents();
        UpdateRuntimeInfo();
    }
    
    #endregion
    
    #region Context Menu Actions
    
    [ContextMenu("Toggle All Performance Systems")]
    public void ToggleAllPerformanceSystemsMenu() => ToggleAllPerformanceSystems();
    
    [ContextMenu("Toggle Lightweight Mode")]
    public void ToggleLightweightModeMenu() => ToggleLightweightMode();
    
    [ContextMenu("Enable Lightweight Mode (FPS Only)")]
    public void EnableLightweightModeMenu() => SetLightweightMode(true);
    
    [ContextMenu("Enable Full Debug Mode")]
    public void EnableFullDebugModeMenu() => SetLightweightMode(false);
    
    [ContextMenu("Enable All Components")]
    public void EnableAllComponentsMenu() => EnableAllComponents();
    
    [ContextMenu("Disable All Components")]
    public void DisableAllComponentsMenu() => DisableAllComponents();
    
    [ContextMenu("Reset All Components")]
    public void ResetAllComponentsMenu() => ResetAllComponents();
    
    [ContextMenu("Force Update All")]
    public void ForceUpdateAllMenu() => ForceUpdateAll();
    
    [ContextMenu("Log Performance Summary")]
    public void LogPerformanceSummary()
    {
        Debug.Log($"PERFORMANCE SUMMARY: {GetPerformanceSummary()}");
    }
    
    #endregion
}