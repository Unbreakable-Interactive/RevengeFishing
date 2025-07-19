using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System;

/// <summary>
/// Analyzes differences between two scenes
/// </summary>
public static class SceneDiffAnalyzer
{
    private static ComparisonSettings settings = ComparisonSettings.Default;
    
    /// <summary>
    /// Compare two scenes and return list of differences
    /// </summary>
    public static List<GameObjectDiff> CompareScenes(SceneData sourceScene, SceneData targetScene, ComparisonSettings customSettings = null)
    {
        if (customSettings != null)
            settings = customSettings;
        else
            settings = ComparisonSettings.Default;
            
        var differences = new List<GameObjectDiff>();
        
        // Create lookup dictionaries for efficient comparison
        var sourceObjects = CreateGameObjectLookup(sourceScene.gameObjects);
        var targetObjects = CreateGameObjectLookup(targetScene.gameObjects);
        
        // Find additions (in source but not in target)
        foreach (var kvp in sourceObjects)
        {
            if (!targetObjects.ContainsKey(kvp.Key))
            {
                var diff = new GameObjectDiff(kvp.Key, DiffType.Addition)
                {
                    sourceData = kvp.Value
                };
                differences.Add(diff);
            }
        }
        
        // Find deletions (in target but not in source)
        foreach (var kvp in targetObjects)
        {
            if (!sourceObjects.ContainsKey(kvp.Key))
            {
                var diff = new GameObjectDiff(kvp.Key, DiffType.Deletion)
                {
                    targetData = kvp.Value
                };
                differences.Add(diff);
            }
        }
        
        // Find modifications (in both but different)
        foreach (var kvp in sourceObjects)
        {
            if (targetObjects.TryGetValue(kvp.Key, out GameObjectData targetData))
            {
                var modifications = CompareGameObjects(kvp.Value, targetData);
                if (modifications.componentDiffs.Count > 0 || modifications.propertyChanges.Count > 0)
                {
                    modifications.type = DiffType.Modification;
                    modifications.gameObjectPath = kvp.Key;
                    modifications.sourceData = kvp.Value;
                    modifications.targetData = targetData;
                    differences.Add(modifications);
                }
            }
        }
        
        return differences.OrderBy(d => d.gameObjectPath).ToList();
    }
    
    /// <summary>
    /// Create a lookup dictionary from GameObject hierarchy
    /// </summary>
    private static Dictionary<string, GameObjectData> CreateGameObjectLookup(List<GameObjectData> gameObjects)
    {
        var lookup = new Dictionary<string, GameObjectData>();
        
        foreach (var go in gameObjects)
        {
            AddToLookupRecursive(go, lookup);
        }
        
        return lookup;
    }
    
    /// <summary>
    /// Recursively add GameObjects to lookup dictionary
    /// </summary>
    private static void AddToLookupRecursive(GameObjectData gameObject, Dictionary<string, GameObjectData> lookup)
    {
        lookup[gameObject.path] = gameObject;
        
        foreach (var child in gameObject.children)
        {
            AddToLookupRecursive(child, lookup);
        }
    }
    
    /// <summary>
    /// Compare two GameObjects and return differences
    /// </summary>
    private static GameObjectDiff CompareGameObjects(GameObjectData source, GameObjectData target)
    {
        var diff = new GameObjectDiff(source.path, DiffType.Modification);
        
        // Compare basic properties
        if (settings.compareActiveStates && source.isActive != target.isActive)
        {
            diff.propertyChanges.Add(new PropertyChange("isActive", 
                target.isActive.ToString(), source.isActive.ToString(), PropertyChangeType.ValueChanged));
        }
        
        if (settings.compareTagsAndLayers)
        {
            if (source.tag != target.tag)
            {
                diff.propertyChanges.Add(new PropertyChange("tag", 
                    target.tag, source.tag, PropertyChangeType.ValueChanged));
            }
            
            if (source.layer != target.layer)
            {
                diff.propertyChanges.Add(new PropertyChange("layer", 
                    target.layer.ToString(), source.layer.ToString(), PropertyChangeType.ValueChanged));
            }
        }
        
        // Compare transform
        if (settings.compareTransforms)
        {
            CompareTransforms(source, target, diff);
        }
        
        // Compare components
        if (settings.compareComponents)
        {
            CompareComponents(source, target, diff);
        }
        
        return diff;
    }
    
    /// <summary>
    /// Compare transform properties between two GameObjects
    /// </summary>
    private static void CompareTransforms(GameObjectData source, GameObjectData target, GameObjectDiff diff)
    {
        // Position
        if (!Vector3Approximately(source.position, target.position, settings.positionTolerance))
        {
            diff.propertyChanges.Add(new PropertyChange("position", 
                target.position.ToString(), source.position.ToString(), PropertyChangeType.ValueChanged));
        }
        
        // Rotation
        if (!QuaternionApproximately(source.rotation, target.rotation, settings.rotationTolerance))
        {
            diff.propertyChanges.Add(new PropertyChange("rotation", 
                target.rotation.ToString(), source.rotation.ToString(), PropertyChangeType.ValueChanged));
        }
        
        // Scale
        if (!Vector3Approximately(source.scale, target.scale, settings.scaleTolerance))
        {
            diff.propertyChanges.Add(new PropertyChange("scale", 
                target.scale.ToString(), source.scale.ToString(), PropertyChangeType.ValueChanged));
        }
    }
    
    /// <summary>
    /// Compare components between two GameObjects
    /// </summary>
    private static void CompareComponents(GameObjectData source, GameObjectData target, GameObjectDiff diff)
    {
        // Create component lookups
        var sourceComponents = source.components.ToDictionary(c => c.type, c => c);
        var targetComponents = target.components.ToDictionary(c => c.type, c => c);
        
        // Find added components
        foreach (var kvp in sourceComponents)
        {
            if (!targetComponents.ContainsKey(kvp.Key) && !IsIgnoredComponent(kvp.Key))
            {
                var componentDiff = new ComponentDiff(kvp.Key, ComponentDiffType.Added);
                diff.componentDiffs.Add(componentDiff);
            }
        }
        
        // Find removed components
        foreach (var kvp in targetComponents)
        {
            if (!sourceComponents.ContainsKey(kvp.Key) && !IsIgnoredComponent(kvp.Key))
            {
                var componentDiff = new ComponentDiff(kvp.Key, ComponentDiffType.Removed);
                diff.componentDiffs.Add(componentDiff);
            }
        }
        
        // Find modified components
        foreach (var kvp in sourceComponents)
        {
            if (targetComponents.TryGetValue(kvp.Key, out ComponentData targetComponent) && !IsIgnoredComponent(kvp.Key))
            {
                var componentDiff = CompareComponentData(kvp.Value, targetComponent);
                if (componentDiff.propertyChanges.Count > 0)
                {
                    diff.componentDiffs.Add(componentDiff);
                }
            }
        }
    }
    
    /// <summary>
    /// Compare two components and return differences
    /// </summary>
    private static ComponentDiff CompareComponentData(ComponentData source, ComponentData target)
    {
        var diff = new ComponentDiff(source.type, ComponentDiffType.Modified);
        
        var sourceProps = source.properties;
        var targetProps = target.properties;
        
        // Find property additions
        foreach (var kvp in sourceProps)
        {
            if (!targetProps.ContainsKey(kvp.Key) && !IsIgnoredProperty(kvp.Key))
            {
                diff.propertyChanges.Add(new PropertyChange(kvp.Key, 
                    "null", kvp.Value, PropertyChangeType.Added));
            }
        }
        
        // Find property removals
        foreach (var kvp in targetProps)
        {
            if (!sourceProps.ContainsKey(kvp.Key) && !IsIgnoredProperty(kvp.Key))
            {
                diff.propertyChanges.Add(new PropertyChange(kvp.Key, 
                    kvp.Value, "null", PropertyChangeType.Removed));
            }
        }
        
        // Find property modifications
        foreach (var kvp in sourceProps)
        {
            if (targetProps.TryGetValue(kvp.Key, out string targetValue) && !IsIgnoredProperty(kvp.Key))
            {
                if (kvp.Value != targetValue)
                {
                    diff.propertyChanges.Add(new PropertyChange(kvp.Key, 
                        targetValue, kvp.Value, PropertyChangeType.ValueChanged));
                }
            }
        }
        
        return diff;
    }
    
    /// <summary>
    /// Check if component type should be ignored
    /// </summary>
    private static bool IsIgnoredComponent(string componentType)
    {
        return settings.ignoredComponentTypes.Contains(componentType);
    }
    
    /// <summary>
    /// Check if property should be ignored
    /// </summary>
    private static bool IsIgnoredProperty(string propertyName)
    {
        return settings.ignoredPropertyNames.Any(ignored => 
            propertyName.Contains(ignored) || propertyName.StartsWith("m_") && ignored.StartsWith("m_"));
    }
    
    /// <summary>
    /// Compare Vector3 values with tolerance
    /// </summary>
    private static bool Vector3Approximately(Vector3 a, Vector3 b, float tolerance)
    {
        return Mathf.Abs(a.x - b.x) < tolerance &&
               Mathf.Abs(a.y - b.y) < tolerance &&
               Mathf.Abs(a.z - b.z) < tolerance;
    }
    
    /// <summary>
    /// Compare Quaternion values with tolerance
    /// </summary>
    private static bool QuaternionApproximately(Quaternion a, Quaternion b, float tolerance)
    {
        return Mathf.Abs(a.x - b.x) < tolerance &&
               Mathf.Abs(a.y - b.y) < tolerance &&
               Mathf.Abs(a.z - b.z) < tolerance &&
               Mathf.Abs(a.w - b.w) < tolerance;
    }
    
    /// <summary>
    /// Get statistics about the differences
    /// </summary>
    public static DiffStatistics GetStatistics(List<GameObjectDiff> differences)
    {
        var stats = new DiffStatistics();
        
        foreach (var diff in differences)
        {
            switch (diff.type)
            {
                case DiffType.Addition:
                    stats.additions++;
                    break;
                case DiffType.Deletion:
                    stats.deletions++;
                    break;
                case DiffType.Modification:
                    stats.modifications++;
                    stats.componentChanges += diff.componentDiffs.Count;
                    stats.propertyChanges += diff.propertyChanges.Count + 
                                           diff.componentDiffs.Sum(c => c.propertyChanges.Count);
                    break;
            }
        }
        
        stats.totalDifferences = differences.Count;
        return stats;
    }
}

/// <summary>
/// Statistics about scene differences
/// </summary>
[System.Serializable]
public class DiffStatistics
{
    public int totalDifferences;
    public int additions;
    public int deletions;
    public int modifications;
    public int componentChanges;
    public int propertyChanges;
    
    public override string ToString()
    {
        return $"Total: {totalDifferences} (➕{additions} ➖{deletions} 🔄{modifications}) " +
               $"Components: {componentChanges}, Properties: {propertyChanges}";
    }
}