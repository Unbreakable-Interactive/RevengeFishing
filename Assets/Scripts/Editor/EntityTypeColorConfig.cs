using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

[CreateAssetMenu(fileName = "EntityTypeColorConfig", menuName = "Entity System/Entity Color Configuration")]
public class EntityTypeColorConfig : ScriptableObject
{
    [System.Serializable]
    public class EntityTypeColor
    {
        [Tooltip("The full name of the entity type (e.g., 'Player', 'Enemy')")]
        public string typeName;
        
        [Tooltip("Color used for this entity type in hierarchy visualizations")]
        public Color color = Color.white;
        
        [Tooltip("Icon/emoji used to represent this entity type")]
        public string icon = "📦";
        
        [Tooltip("Description of what this entity type represents")]
        [TextArea(2, 3)]
        public string description;
        
        [Tooltip("Is this a core entity type (cannot be deleted)")]
        public bool isCoreType = false;
    }
    
    [Header("Entity Type Colors & Icons")]
    [Tooltip("Configure colors and icons for each entity type in your project")]
    public List<EntityTypeColor> entityTypeColors = new List<EntityTypeColor>();
    
    [Header("Default Settings")]
    [Tooltip("Default color for entity types not explicitly configured")]
    public Color defaultColor = new Color(0.4f, 0.4f, 0.4f);
    
    [Tooltip("Default icon for entity types not explicitly configured")]
    public string defaultIcon = "📦";
    
    [Header("Auto-Discovery Settings")]
    [Tooltip("Automatically discover new entity types when scripts are compiled")]
    public bool autoDiscoverNewTypes = true;
    
    [Tooltip("Assign random colors to newly discovered types")]
    public bool assignRandomColorsToNewTypes = true;
    
    [Tooltip("Color palette for random assignment")]
    public Color[] randomColorPalette = new Color[]
    {
        new Color(0.2f, 0.4f, 0.7f),  // Deep blue
        new Color(0.2f, 0.6f, 0.3f),  // Forest green  
        new Color(0.8f, 0.4f, 0.2f),  // Warm orange
        new Color(0.7f, 0.3f, 0.5f),  // Deep rose
        new Color(0.5f, 0.3f, 0.7f),  // Rich purple
        new Color(0.1f, 0.5f, 0.7f),  // Cyan
        new Color(0.2f, 0.5f, 0.6f),  // Teal
        new Color(0.6f, 0.4f, 0.2f),  // Brown
        new Color(0.7f, 0.5f, 0.3f),  // Gold
        new Color(0.4f, 0.7f, 0.2f),  // Lime
        new Color(0.6f, 0.2f, 0.7f),  // Violet
        new Color(0.8f, 0.3f, 0.3f)   // Crimson
    };
    
    [Header("Icon Presets")]
    [Tooltip("Common icons you can use for entity types")]
    public string[] iconPresets = new string[]
    {
        "🏛️", "🐟", "👹", "🚶", "🎣", "🪝", "🎯", "🔧",
        "⚔️", "🛡️", "💎", "🎪", "🌟", "🔥", "❄️", "⚡",
        "🌊", "🌱", "🦅", "🐺", "🦈", "🐙", "🦀", "🐢"
    };
    
    private Dictionary<string, EntityTypeColor> colorLookup;
    private bool lookupBuilt = false;
    
    /// <summary>
    /// Get color for a specific entity type
    /// </summary>
    public Color GetColorForType(string typeName)
    {
        BuildLookupIfNeeded();
        
        if (colorLookup.ContainsKey(typeName))
            return colorLookup[typeName].color;
            
        return defaultColor;
    }
    
    /// <summary>
    /// Get color for a specific entity type
    /// </summary>
    public Color GetColorForType(System.Type type)
    {
        return GetColorForType(type.Name);
    }
    
    /// <summary>
    /// Get icon for a specific entity type
    /// </summary>
    public string GetIconForType(string typeName)
    {
        BuildLookupIfNeeded();
        
        if (colorLookup.ContainsKey(typeName))
            return colorLookup[typeName].icon;
            
        return defaultIcon;
    }
    
    /// <summary>
    /// Get icon for a specific entity type
    /// </summary>
    public string GetIconForType(System.Type type)
    {
        return GetIconForType(type.Name);
    }
    
    /// <summary>
    /// Add or update a type configuration
    /// </summary>
    public void SetTypeConfig(string typeName, Color color, string icon = null, string description = null)
    {
        var existing = entityTypeColors.Find(etc => etc.typeName == typeName);
        
        if (existing != null)
        {
            existing.color = color;
            if (icon != null) existing.icon = icon;
            if (description != null) existing.description = description;
        }
        else
        {
            entityTypeColors.Add(new EntityTypeColor
            {
                typeName = typeName,
                color = color,
                icon = icon ?? defaultIcon,
                description = description ?? $"Auto-discovered entity type: {typeName}"
            });
        }
        
        // Force rebuild of lookup
        lookupBuilt = false;
        BuildLookupIfNeeded();
        
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }
    
    /// <summary>
    /// Check if a type is configured
    /// </summary>
    public bool HasTypeConfig(string typeName)
    {
        BuildLookupIfNeeded();
        return colorLookup.ContainsKey(typeName);
    }
    
    /// <summary>
    /// Get all configured type names
    /// </summary>
    public string[] GetConfiguredTypeNames()
    {
        BuildLookupIfNeeded();
        return colorLookup.Keys.ToArray();
    }
    
    /// <summary>
    /// Remove a type configuration (only if not a core type)
    /// </summary>
    public bool RemoveTypeConfig(string typeName)
    {
        var existing = entityTypeColors.Find(etc => etc.typeName == typeName);
        
        if (existing != null && !existing.isCoreType)
        {
            entityTypeColors.Remove(existing);
            lookupBuilt = false;
            
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// Get a random color from the palette
    /// </summary>
    public Color GetRandomColor()
    {
        if (randomColorPalette.Length == 0)
            return defaultColor;
            
        return randomColorPalette[UnityEngine.Random.Range(0, randomColorPalette.Length)];
    }
    
    /// <summary>
    /// Initialize with default entity types
    /// </summary>
    [ContextMenu("Initialize with Default Types")]
    public void InitializeWithDefaults()
    {
        entityTypeColors.Clear();
        
        // Add core entity types
        var coreTypes = new[]
        {
            new EntityTypeColor { typeName = "Entity", color = new Color(0.2f, 0.4f, 0.7f), icon = "🏛️", description = "Base entity class", isCoreType = true },
            new EntityTypeColor { typeName = "Player", color = new Color(0.2f, 0.6f, 0.3f), icon = "🐟", description = "Player character", isCoreType = true },
            new EntityTypeColor { typeName = "Enemy", color = new Color(0.8f, 0.4f, 0.2f), icon = "👹", description = "Base enemy class", isCoreType = true },
            new EntityTypeColor { typeName = "LandEnemy", color = new Color(0.7f, 0.3f, 0.5f), icon = "🚶", description = "Land-based enemy", isCoreType = true },
            new EntityTypeColor { typeName = "Fisherman", color = new Color(0.5f, 0.3f, 0.7f), icon = "🎣", description = "Fisherman character", isCoreType = true },
            new EntityTypeColor { typeName = "FishingProjectile", color = new Color(0.1f, 0.5f, 0.7f), icon = "🪝", description = "Fishing projectile", isCoreType = true },
            new EntityTypeColor { typeName = "FishingHook", color = new Color(0.2f, 0.5f, 0.6f), icon = "🎯", description = "Fishing hook", isCoreType = true },
            new EntityTypeColor { typeName = "DroppedTool", color = new Color(0.6f, 0.4f, 0.2f), icon = "🔧", description = "Dropped tool item", isCoreType = true }
        };
        
        entityTypeColors.AddRange(coreTypes);
        
        lookupBuilt = false;
        
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        Debug.Log("EntityTypeColorConfig initialized with default types!");
#endif
    }
    
    private void BuildLookupIfNeeded()
    {
        if (!lookupBuilt || colorLookup == null)
        {
            colorLookup = new Dictionary<string, EntityTypeColor>();
            
            foreach (var typeColor in entityTypeColors)
            {
                if (!string.IsNullOrEmpty(typeColor.typeName))
                {
                    colorLookup[typeColor.typeName] = typeColor;
                }
            }
            
            lookupBuilt = true;
        }
    }
    
    private void OnValidate()
    {
        // Force rebuild lookup when inspector values change
        lookupBuilt = false;
    }
}