using UnityEngine;
using UnityEditor;
using UnityEditor.Compilation;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System;

/// <summary>
/// Automatically discovers entity types and manages color configuration
/// </summary>
[InitializeOnLoad]
public static class EntityTypeAutoDiscovery
{
    private static EntityTypeColorConfig cachedConfig;
    private static double lastScanTime;
    private static HashSet<string> knownEntityTypes = new HashSet<string>();
    
    // Events for when entity types are discovered/changed
    public static event System.Action<List<System.Type>> OnEntityTypesDiscovered;
    public static event System.Action<System.Type> OnNewEntityTypeFound;
    
    static EntityTypeAutoDiscovery()
    {
        // Subscribe to compilation events
        CompilationPipeline.compilationStarted += OnCompilationStarted;
        CompilationPipeline.compilationFinished += OnCompilationFinished;
        
        // Subscribe to asset database events
        EditorApplication.delayCall += () => {
            ScanForEntityTypes();
        };
        
        Debug.Log("🔍 EntityTypeAutoDiscovery initialized - watching for new entity types!");
    }
    
    /// <summary>
    /// Get or create the entity type color configuration
    /// </summary>
    public static EntityTypeColorConfig GetOrCreateConfig()
    {
        if (cachedConfig == null)
        {
            // Try to find existing config
            cachedConfig = Resources.Load<EntityTypeColorConfig>("EntityTypeColorConfig");
            
            if (cachedConfig == null)
            {
                // Search in all asset paths
                string[] guids = AssetDatabase.FindAssets("t:EntityTypeColorConfig");
                
                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    cachedConfig = AssetDatabase.LoadAssetAtPath<EntityTypeColorConfig>(path);
                }
            }
            
            // If still not found, create a new one
            if (cachedConfig == null)
            {
                cachedConfig = CreateDefaultConfig();
            }
        }
        
        return cachedConfig;
    }
    
    /// <summary>
    /// Scan for all entity types in the project
    /// </summary>
    [MenuItem("Tools/Entity System/Scan for Entity Types")]
    public static List<System.Type> ScanForEntityTypes()
    {
        var discoveredTypes = new List<System.Type>();
        var config = GetOrCreateConfig();
        
        if (config == null) return discoveredTypes;
        
        try
        {
            // Get all assemblies that might contain entity types
            var assemblies = System.AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.FullName.StartsWith("Unity") && 
                           !a.FullName.StartsWith("System") && 
                           !a.FullName.StartsWith("mscorlib"));
            
            foreach (var assembly in assemblies)
            {
                try
                {
                    var entityTypes = assembly.GetTypes()
                        .Where(t => typeof(MonoBehaviour).IsAssignableFrom(t) && 
                                   (t.Name.Contains("Entity") || IsEntityType(t)))
                        .Where(t => !t.IsAbstract && !t.IsInterface);
                    
                    foreach (var type in entityTypes)
                    {
                        discoveredTypes.Add(type);
                        
                        // Check if this is a new type
                        if (!knownEntityTypes.Contains(type.Name))
                        {
                            HandleNewEntityType(type, config);
                            knownEntityTypes.Add(type.Name);
                        }
                    }
                }
                catch (ReflectionTypeLoadException ex)
                {
                    // Handle assemblies that can't be fully loaded
                    var loadedTypes = ex.Types.Where(t => t != null);
                    foreach (var type in loadedTypes)
                    {
                        if (IsEntityType(type) && !type.IsAbstract && !type.IsInterface)
                        {
                            discoveredTypes.Add(type);
                            
                            if (!knownEntityTypes.Contains(type.Name))
                            {
                                HandleNewEntityType(type, config);
                                knownEntityTypes.Add(type.Name);
                            }
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    // Log but don't stop scanning
                    Debug.LogWarning($"Error scanning assembly {assembly.FullName}: {ex.Message}");
                }
            }
            
            lastScanTime = EditorApplication.timeSinceStartup;
            
            // Notify listeners
            OnEntityTypesDiscovered?.Invoke(discoveredTypes);
            
            Debug.Log($"🔍 EntityTypeAutoDiscovery: Found {discoveredTypes.Count} entity types");
            
            return discoveredTypes;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error during entity type scanning: {ex.Message}");
            return discoveredTypes;
        }
    }
    
    /// <summary>
    /// Check if a type is considered an entity type
    /// </summary>
    private static bool IsEntityType(System.Type type)
    {
        if (type == null) return false;
        
        // Check if it inherits from MonoBehaviour
        if (!typeof(MonoBehaviour).IsAssignableFrom(type)) return false;
        
        // Check if it's in our known entity types or contains "Entity" in name
        if (type.Name.Contains("Entity")) return true;
        
        // Check if it inherits from any known entity types
        var baseType = type.BaseType;
        while (baseType != null && baseType != typeof(MonoBehaviour))
        {
            if (baseType.Name.Contains("Entity") || knownEntityTypes.Contains(baseType.Name))
                return true;
                
            baseType = baseType.BaseType;
        }
        
        // Check for Entity attribute or interface
        return type.GetCustomAttributes(typeof(System.ComponentModel.CategoryAttribute), true)
                  .Any(attr => ((System.ComponentModel.CategoryAttribute)attr).Category == "Entity");
    }
    
    /// <summary>
    /// Handle discovery of a new entity type
    /// </summary>
    private static void HandleNewEntityType(System.Type type, EntityTypeColorConfig config)
    {
        if (config.autoDiscoverNewTypes && !config.HasTypeConfig(type.Name))
        {
            Color newColor = config.assignRandomColorsToNewTypes ? 
                           config.GetRandomColor() : 
                           config.defaultColor;
            
            string newIcon = GuessIconForType(type);
            string description = $"Auto-discovered entity type: {type.Name}";
            
            // Add inheritance info to description
            if (type.BaseType != null && type.BaseType != typeof(MonoBehaviour))
            {
                description += $" (inherits from {type.BaseType.Name})";
            }
            
            config.SetTypeConfig(type.Name, newColor, newIcon, description);
            
            Debug.Log($"🆕 New entity type discovered: {type.Name} - assigned color {newColor} and icon {newIcon}");
            
            // Notify listeners
            OnNewEntityTypeFound?.Invoke(type);
        }
    }
    
    /// <summary>
    /// Guess an appropriate icon for an entity type based on its name
    /// </summary>
    private static string GuessIconForType(System.Type type)
    {
        string typeName = type.Name.ToLower();
        
        if (typeName.Contains("player")) return "🐟";
        if (typeName.Contains("enemy")) return "👹";
        if (typeName.Contains("fish")) return "🐠";
        if (typeName.Contains("hook")) return "🎯";
        if (typeName.Contains("projectile")) return "🪝";
        if (typeName.Contains("tool")) return "🔧";
        if (typeName.Contains("weapon")) return "⚔️";
        if (typeName.Contains("power")) return "⚡";
        if (typeName.Contains("item")) return "💎";
        if (typeName.Contains("npc")) return "🚶";
        if (typeName.Contains("boat") || typeName.Contains("ship")) return "⛵";
        if (typeName.Contains("water")) return "🌊";
        if (typeName.Contains("effect")) return "✨";
        
        return "📦"; // Default
    }
    
    /// <summary>
    /// Create a default configuration asset
    /// </summary>
    private static EntityTypeColorConfig CreateDefaultConfig()
    {
        var config = ScriptableObject.CreateInstance<EntityTypeColorConfig>();
        
        // Create Resources folder if it doesn't exist
        string resourcesPath = "Assets/Resources";
        if (!AssetDatabase.IsValidFolder(resourcesPath))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }
        
        // Save the asset
        string assetPath = $"{resourcesPath}/EntityTypeColorConfig.asset";
        AssetDatabase.CreateAsset(config, assetPath);
        
        // Initialize with defaults
        config.InitializeWithDefaults();
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log($"✅ Created new EntityTypeColorConfig at {assetPath}");
        
        return config;
    }
    
    /// <summary>
    /// Get all currently known entity types
    /// </summary>
    public static string[] GetKnownEntityTypeNames()
    {
        return knownEntityTypes.ToArray();
    }
    
    /// <summary>
    /// Force a refresh of the configuration cache
    /// </summary>
    public static void RefreshConfigCache()
    {
        cachedConfig = null;
        GetOrCreateConfig();
    }
    
    /// <summary>
    /// Check if auto-discovery should run (throttled to avoid performance issues)
    /// </summary>
    private static bool ShouldRunAutoDiscovery()
    {
        return EditorApplication.timeSinceStartup - lastScanTime > 2.0; // Max once every 2 seconds
    }
    
    private static void OnCompilationStarted(object obj)
    {
        // Clear known types before compilation
        knownEntityTypes.Clear();
    }
    
    private static void OnCompilationFinished(object obj)
    {
        if (ShouldRunAutoDiscovery())
        {
            // Delay the scan to ensure assemblies are fully loaded
            EditorApplication.delayCall += () => {
                ScanForEntityTypes();
            };
        }
    }
    
    [MenuItem("Tools/Entity System/Refresh Entity Type Discovery")]
    private static void ManualRefresh()
    {
        RefreshConfigCache();
        ScanForEntityTypes();
        
        // Force refresh of hierarchy windows
        var hierarchyWindows = Resources.FindObjectsOfTypeAll<EntityHierarchyWindow>();
        foreach (var window in hierarchyWindows)
        {
            window.Repaint();
        }
        
        Debug.Log("🔄 Entity type discovery manually refreshed!");
    }
}