using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Specialized component for optimizing draw calls in 2D games
/// Focuses on sprite batching, culling, and texture optimization
/// </summary>
public class DrawCallOptimizer : PerformanceComponentBase
{
    public override string ComponentName => "Draw Call Optimizer";
    
    [Header("Draw Call Analysis")]
    public Camera targetCamera;
    public bool enableAutomaticOptimization = true;
    public bool logOptimizationResults = true;
    
    [Header("Sprite Batching")]
    public bool enableSpriteBatching = true;
    public bool forceSpriteAtlas = false;
    public int maxSpritesPerBatch = 50;
    
    [Header("Culling Optimization")]
    public bool enableFrustumCulling = true;
    public bool enableDistanceCulling = true;
    public float maxRenderDistance = 100f;
    public LayerMask cullableLayers = -1;
    
    [Header("Texture Optimization")]
    public bool optimizeTextureSettings = false;
    public int maxTextureSize = 1024;
    public bool forceTextureCompression = false;
    
    // Analysis data
    private List<SpriteRenderer> allSpriteRenderers = new List<SpriteRenderer>();
    private Dictionary<Texture2D, List<SpriteRenderer>> textureGroups = new Dictionary<Texture2D, List<SpriteRenderer>>();
    private Dictionary<Material, List<SpriteRenderer>> materialGroups = new Dictionary<Material, List<SpriteRenderer>>();
    
    // Performance metrics
    private int originalDrawCalls = 0;
    private int optimizedDrawCalls = 0;
    private int culledSprites = 0;
    private float lastAnalysisTime = 0f;
    private float analysisInterval = 2f;
    
    // Optimization results
    public struct OptimizationResult
    {
        public int beforeDrawCalls;
        public int afterDrawCalls;
        public int spritesAnalyzed;
        public int spritesCulled;
        public int textureGroups;
        public int materialGroups;
        public string recommendations;
    }
    
    private OptimizationResult lastResult;
    
    public override void Initialize()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;
            
        AnalyzeScene();
        
        if (enableAutomaticOptimization)
        {
            ApplyOptimizations();
        }
        
        GameLogger.LogVerbose($"{ComponentName}: Initialized - Found {allSpriteRenderers.Count} sprite renderers");
    }
    
    void Update()
    {
        if (!isEnabled) return;
        
        UpdateComponent();
    }
    
    public override void UpdateComponent()
    {
        // Periodic analysis
        if (Time.time - lastAnalysisTime > analysisInterval)
        {
            if (enableFrustumCulling || enableDistanceCulling)
            {
                UpdateCulling();
            }
            
            lastAnalysisTime = Time.time;
        }
    }
    
    /// <summary>
    /// Analyze the scene for draw call optimization opportunities
    /// </summary>
    [ContextMenu("Analyze Scene")]
    public void AnalyzeScene()
    {
        allSpriteRenderers.Clear();
        textureGroups.Clear();
        materialGroups.Clear();
        
        // Find all sprite renderers
        allSpriteRenderers = FindObjectsOfType<SpriteRenderer>().ToList();
        originalDrawCalls = allSpriteRenderers.Count(sr => sr.enabled && sr.gameObject.activeInHierarchy);
        
        // Group by texture
        foreach (var sr in allSpriteRenderers)
        {
            if (sr.sprite != null && sr.sprite.texture != null)
            {
                var texture = sr.sprite.texture;
                if (!textureGroups.ContainsKey(texture))
                    textureGroups[texture] = new List<SpriteRenderer>();
                textureGroups[texture].Add(sr);
            }
            
            // Group by material
            var material = sr.sharedMaterial;
            if (material != null)
            {
                if (!materialGroups.ContainsKey(material))
                    materialGroups[material] = new List<SpriteRenderer>();
                materialGroups[material].Add(sr);
            }
        }
        
        // Generate recommendations
        GenerateOptimizationRecommendations();
        
        if (logOptimizationResults)
        {
            LogAnalysisResults();
        }
    }
    
    /// <summary>
    /// Apply automatic optimizations
    /// </summary>
    [ContextMenu("Apply Optimizations")]
    public void ApplyOptimizations()
    {
        if (enableSpriteBatching)
        {
            OptimizeSpriteBatching();
        }
        
        if (optimizeTextureSettings)
        {
            OptimizeTextureSettings();
        }
        
        // Update draw call count
        optimizedDrawCalls = allSpriteRenderers.Count(sr => sr.enabled && sr.gameObject.activeInHierarchy);
        
        GameLogger.LogVerbose($"DrawCallOptimizer: Optimization complete. Draw calls: {originalDrawCalls} → {optimizedDrawCalls} (Saved: {originalDrawCalls - optimizedDrawCalls})");
    }
    
    void OptimizeSpriteBatching()
    {
        // Group sprites by same texture and material for batching
        var batchGroups = materialGroups.Where(kvp => kvp.Value.Count > 1).ToList();
        
        foreach (var group in batchGroups)
        {
            var sprites = group.Value;
            if (sprites.Count <= maxSpritesPerBatch)
            {
                // These sprites can potentially be batched
                // Ensure they use the same sorting layer and have similar z-positions
                OptimizeBatchGroup(sprites);
            }
        }
    }
    
    void OptimizeBatchGroup(List<SpriteRenderer> sprites)
    {
        // Sort by z-position to maintain proper layering
        sprites.Sort((a, b) => a.transform.position.z.CompareTo(b.transform.position.z));
        
        // Set consistent sorting properties for batching
        string commonSortingLayer = sprites[0].sortingLayerName;
        
        for (int i = 0; i < sprites.Count; i++)
        {
            sprites[i].sortingLayerName = commonSortingLayer;
            sprites[i].sortingOrder = i; // Sequential ordering
        }
    }
    
    void OptimizeTextureSettings()
    {
        foreach (var textureGroup in textureGroups)
        {
            var texture = textureGroup.Key;
            var spriteCount = textureGroup.Value.Count;
            
            // Skip if texture is null or already optimized
            if (texture == null) continue;
            
            #if UNITY_EDITOR
            string texturePath = UnityEditor.AssetDatabase.GetAssetPath(texture);
            if (!string.IsNullOrEmpty(texturePath))
            {
                var importer = UnityEditor.AssetImporter.GetAtPath(texturePath) as UnityEditor.TextureImporter;
                if (importer != null)
                {
                    bool changed = false;
                    
                    // Optimize texture size based on usage
                    if (texture.width > maxTextureSize || texture.height > maxTextureSize)
                    {
                        importer.maxTextureSize = maxTextureSize;
                        changed = true;
                    }
                    
                    // Enable compression if not already enabled
                    if (forceTextureCompression && importer.textureCompression == UnityEditor.TextureImporterCompression.Uncompressed)
                    {
                        importer.textureCompression = UnityEditor.TextureImporterCompression.Compressed;
                        changed = true;
                    }
                    
                    if (changed)
                    {
                        UnityEditor.AssetDatabase.ImportAsset(texturePath);
                        GameLogger.LogVerbose($"Optimized texture: {texture.name} (used by {spriteCount} sprites)");
                    }
                }
            }
            #endif
        }
    }
    
    void UpdateCulling()
    {
        if (targetCamera == null) return;
        
        culledSprites = 0;
        Vector3 cameraPos = targetCamera.transform.position;
        Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(targetCamera);
        
        foreach (var sr in allSpriteRenderers)
        {
            if (sr == null || !sr.gameObject.activeInHierarchy) continue;
            
            bool shouldRender = true;
            
            // Distance culling
            if (enableDistanceCulling)
            {
                float distance = Vector3.Distance(sr.transform.position, cameraPos);
                if (distance > maxRenderDistance)
                {
                    shouldRender = false;
                }
            }
            
            // Frustum culling
            if (shouldRender && enableFrustumCulling)
            {
                Bounds bounds = sr.bounds;
                if (!GeometryUtility.TestPlanesAABB(frustumPlanes, bounds))
                {
                    shouldRender = false;
                }
            }
            
            // Apply culling result
            if (sr.enabled != shouldRender)
            {
                sr.enabled = shouldRender;
                if (!shouldRender) culledSprites++;
            }
        }
    }
    
    void GenerateOptimizationRecommendations()
    {
        List<string> recommendations = new List<string>();
        
        // Analyze texture usage
        var largeTextureGroups = textureGroups.Where(kvp => kvp.Value.Count > 10).ToList();
        if (largeTextureGroups.Count > 0)
        {
            recommendations.Add($"Consider using Sprite Atlases for {largeTextureGroups.Count} frequently used textures");
        }
        
        // Analyze draw calls
        if (originalDrawCalls > 100)
        {
            recommendations.Add($"High draw calls detected ({originalDrawCalls}). Enable sprite batching and culling");
        }
        
        // Analyze layering
        var layerGroups = allSpriteRenderers.GroupBy(sr => sr.sortingLayerName).ToList();
        if (layerGroups.Count > 10)
        {
            recommendations.Add($"Too many sorting layers ({layerGroups.Count}). Consolidate similar objects");
        }
        
        // Analyze material usage
        var materialCount = materialGroups.Count;
        if (materialCount > 20)
        {
            recommendations.Add($"Too many materials ({materialCount}). Share materials between similar sprites");
        }
        
        lastResult = new OptimizationResult
        {
            beforeDrawCalls = originalDrawCalls,
            afterDrawCalls = optimizedDrawCalls,
            spritesAnalyzed = allSpriteRenderers.Count,
            spritesCulled = culledSprites,
            textureGroups = textureGroups.Count,
            materialGroups = materialGroups.Count,
            recommendations = string.Join("; ", recommendations)
        };
    }
    
    void LogAnalysisResults()
    {
        GameLogger.LogVerbose("=== DRAW CALL ANALYSIS RESULTS ===");
        GameLogger.LogVerbose($"Total Sprite Renderers: {allSpriteRenderers.Count}");
        GameLogger.LogVerbose($"Active Draw Calls: {originalDrawCalls}");
        GameLogger.LogVerbose($"Unique Textures: {textureGroups.Count}");
        GameLogger.LogVerbose($"Unique Materials: {materialGroups.Count}");
        GameLogger.LogVerbose($"Culled Sprites: {culledSprites}");
        
        // Log largest texture groups
        var topTextures = textureGroups.OrderByDescending(kvp => kvp.Value.Count).Take(5);
        GameLogger.LogVerbose("Top texture usage:");
        foreach (var kvp in topTextures)
        {
            GameLogger.LogVerbose($"  {kvp.Key.name}: {kvp.Value.Count} sprites");
        }
        
        GameLogger.LogVerbose($"Recommendations: {lastResult.recommendations}");
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
        
        // Draw call stats
        Color drawCallColor = originalDrawCalls > 100 ? Color.red : originalDrawCalls > 50 ? Color.yellow : Color.green;
        normalStyle.normal.textColor = drawCallColor;
        GUI.Label(new Rect(x, y, 400, lineHeight), $"Draw Calls: {originalDrawCalls} (Culled: {culledSprites})", normalStyle);
        y += lineHeight;
        
        // Texture analysis
        normalStyle.normal.textColor = Color.white;
        GUI.Label(new Rect(x, y, 400, lineHeight), $"Unique Textures: {textureGroups.Count} | Materials: {materialGroups.Count}", normalStyle);
        y += lineHeight;
        
        // Optimization status
        if (enableAutomaticOptimization)
        {
            goodStyle.normal.textColor = Color.green;
            GUI.Label(new Rect(x, y, 400, lineHeight), "✓ Auto-optimization enabled", goodStyle);
        }
        else
        {
            warningStyle.normal.textColor = Color.yellow;
            GUI.Label(new Rect(x, y, 400, lineHeight), "⚠ Auto-optimization disabled", warningStyle);
        }
        y += lineHeight;
        
        // Culling stats
        if (enableFrustumCulling || enableDistanceCulling)
        {
            GUI.Label(new Rect(x, y, 400, lineHeight), $"Culling: Distance({maxRenderDistance}m) Frustum({enableFrustumCulling})", normalStyle);
            y += lineHeight;
        }
        
        return y;
    }
    
    public override string GetPerformanceMetrics()
    {
        return $"Draw Calls: {originalDrawCalls} | Culled: {culledSprites} | Textures: {textureGroups.Count}";
    }
    
    // Public API
    public int GetDrawCallCount() => originalDrawCalls;
    public int GetCulledSpriteCount() => culledSprites;
    public int GetTextureCount() => textureGroups.Count;
    public OptimizationResult GetLastAnalysisResult() => lastResult;
    
    [ContextMenu("Force Analyze and Optimize")]
    public void ForceOptimization()
    {
        AnalyzeScene();
        ApplyOptimizations();
    }
}
