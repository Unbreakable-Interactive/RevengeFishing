using UnityEngine;
using System.Collections.Generic;
using System;

/// <summary>
/// Data structure representing a complete scene for comparison
/// </summary>
[System.Serializable]
public class SceneData
{
    public string scenePath;
    public string sceneName;
    public List<GameObjectData> gameObjects = new List<GameObjectData>();
}

/// <summary>
/// Data structure representing a GameObject and all its data
/// </summary>
[System.Serializable]
public class GameObjectData
{
    public string name;
    public string path;
    public bool isActive;
    public string tag;
    public int layer;
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 scale;
    public List<ComponentData> components = new List<ComponentData>();
    public List<GameObjectData> children = new List<GameObjectData>();
    
    public GameObjectData FindChild(string childPath)
    {
        if (path == childPath)
            return this;
            
        foreach (var child in children)
        {
            var found = child.FindChild(childPath);
            if (found != null)
                return found;
        }
        
        return null;
    }
    
    public ComponentData FindComponent(string componentType)
    {
        return components.Find(c => c.type == componentType);
    }
}

/// <summary>
/// Data structure representing a component and its properties
/// </summary>
[System.Serializable]
public class ComponentData
{
    public string type;
    public string assemblyQualifiedName;
    public Dictionary<string, string> properties = new Dictionary<string, string>();
    
    public bool HasProperty(string propertyName)
    {
        return properties.ContainsKey(propertyName);
    }
    
    public string GetProperty(string propertyName)
    {
        return properties.TryGetValue(propertyName, out string value) ? value : null;
    }
    
    public List<string> GetPropertyNames()
    {
        return new List<string>(properties.Keys);
    }
}

/// <summary>
/// Represents a difference between two GameObjects
/// </summary>
[System.Serializable]
public class GameObjectDiff
{
    public string gameObjectPath;
    public DiffType type;
    public GameObjectData sourceData;
    public GameObjectData targetData;
    public List<ComponentDiff> componentDiffs = new List<ComponentDiff>();
    public List<PropertyChange> propertyChanges = new List<PropertyChange>();
    public bool isSelected = false;
    
    public GameObjectDiff(string path, DiffType diffType)
    {
        gameObjectPath = path;
        type = diffType;
    }
}

/// <summary>
/// Represents a difference in a component
/// </summary>
[System.Serializable]
public class ComponentDiff
{
    public string componentType;
    public ComponentDiffType type;
    public List<PropertyChange> propertyChanges = new List<PropertyChange>();
    
    public ComponentDiff(string componentType, ComponentDiffType diffType)
    {
        this.componentType = componentType;
        this.type = diffType;
    }
}

/// <summary>
/// Represents a change in a property value
/// </summary>
[System.Serializable]
public class PropertyChange
{
    public string propertyName;
    public string oldValue;
    public string newValue;
    public PropertyChangeType changeType;
    
    public PropertyChange(string name, string oldVal, string newVal, PropertyChangeType type)
    {
        propertyName = name;
        oldValue = oldVal;
        newValue = newVal;
        changeType = type;
    }
}

/// <summary>
/// Types of differences between GameObjects
/// </summary>
public enum DiffType
{
    Addition,     // GameObject exists in source but not target
    Deletion,     // GameObject exists in target but not source
    Modification  // GameObject exists in both but has differences
}

/// <summary>
/// Types of component differences
/// </summary>
public enum ComponentDiffType
{
    Added,     // Component exists in source but not target
    Removed,   // Component exists in target but not source
    Modified   // Component exists in both but has property differences
}

/// <summary>
/// Types of property changes
/// </summary>
public enum PropertyChangeType
{
    ValueChanged,  // Property value is different
    TypeChanged,   // Property type is different
    Added,         // Property exists in source but not target
    Removed        // Property exists in target but not source
}

/// <summary>
/// Configuration for scene comparison behavior
/// </summary>
[System.Serializable]
public class ComparisonSettings
{
    [Header("Comparison Options")]
    public bool compareTransforms = true;
    public bool compareComponents = true;
    public bool compareActiveStates = true;
    public bool compareTagsAndLayers = true;
    public bool comparePrefabConnections = true;
    
    [Header("Precision Settings")]
    public float positionTolerance = 0.001f;
    public float rotationTolerance = 0.001f;
    public float scaleTolerance = 0.001f;
    
    [Header("Filter Settings")]
    public List<string> ignoredComponentTypes = new List<string>
    {
        "UnityEngine.Transform" // Transform is handled separately
    };
    
    public List<string> ignoredPropertyNames = new List<string>
    {
        "m_InstanceID",
        "m_LocalIdentfierInFile"
    };
    
    [Header("Performance")]
    public bool enableProgressBar = true;
    public int maxDepthLevels = 20;
    
    public static ComparisonSettings Default => new ComparisonSettings();
}