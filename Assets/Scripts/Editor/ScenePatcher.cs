using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Reflection;

/// <summary>
/// Applies scene differences to target scenes
/// </summary>
public static class ScenePatcher
{
    /// <summary>
    /// Apply a list of changes to the target scene
    /// </summary>
    public static bool ApplyChanges(SceneAsset targetSceneAsset, List<GameObjectDiff> changes)
    {
        if (targetSceneAsset == null || changes == null || changes.Count == 0)
        {
            Debug.LogWarning("ScenePatcher: Invalid parameters for ApplyChanges");
            return false;
        }

        string targetScenePath = AssetDatabase.GetAssetPath(targetSceneAsset);
        var originalScene = SceneManager.GetActiveScene();
        
        try
        {
            EditorUtility.DisplayProgressBar("Applying Changes", "Opening target scene...", 0f);
            
            // Open target scene
            var targetScene = EditorSceneManager.OpenScene(targetScenePath, OpenSceneMode.Single);
            
            if (!targetScene.IsValid())
            {
                Debug.LogError($"ScenePatcher: Failed to open target scene: {targetScenePath}");
                return false;
            }

            bool hasChanges = false;
            int processedChanges = 0;

            foreach (var change in changes)
            {
                EditorUtility.DisplayProgressBar("Applying Changes", 
                    $"Processing {change.gameObjectPath}...", 
                    (float)processedChanges / changes.Count);

                try
                {
                    if (ApplyIndividualChange(targetScene, change))
                    {
                        hasChanges = true;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"ScenePatcher: Failed to apply change to {change.gameObjectPath}: {ex.Message}");
                }

                processedChanges++;
            }

            if (hasChanges)
            {
                EditorUtility.DisplayProgressBar("Applying Changes", "Saving scene...", 0.9f);
                
                // Mark scene as dirty and save
                EditorSceneManager.MarkSceneDirty(targetScene);
                EditorSceneManager.SaveScene(targetScene);
                
                Debug.Log($"ScenePatcher: Successfully applied {processedChanges} changes to {targetScene.name}");
            }
            else
            {
                Debug.Log("ScenePatcher: No changes were applied");
            }

            return hasChanges;
        }
        catch (Exception ex)
        {
            Debug.LogError($"ScenePatcher: Error applying changes: {ex.Message}");
            return false;
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            
            // Restore original scene if it was different
            if (originalScene.IsValid() && originalScene.path != targetScenePath)
            {
                EditorSceneManager.OpenScene(originalScene.path, OpenSceneMode.Single);
            }
        }
    }

    /// <summary>
    /// Apply a single change to the target scene
    /// </summary>
    private static bool ApplyIndividualChange(Scene targetScene, GameObjectDiff change)
    {
        switch (change.type)
        {
            case DiffType.Addition:
                return ApplyAddition(targetScene, change);
                
            case DiffType.Deletion:
                return ApplyDeletion(targetScene, change);
                
            case DiffType.Modification:
                return ApplyModification(targetScene, change);
                
            default:
                Debug.LogWarning($"ScenePatcher: Unknown diff type: {change.type}");
                return false;
        }
    }

    /// <summary>
    /// Apply an addition (create new GameObject)
    /// </summary>
    private static bool ApplyAddition(Scene targetScene, GameObjectDiff change)
    {
        if (change.sourceData == null)
        {
            Debug.LogWarning($"ScenePatcher: No source data for addition: {change.gameObjectPath}");
            return false;
        }

        try
        {
            // Create the GameObject
            var newGameObject = CreateGameObjectFromData(change.sourceData, targetScene);
            
            if (newGameObject != null)
            {
                Debug.Log($"ScenePatcher: Added GameObject: {change.gameObjectPath}");
                return true;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"ScenePatcher: Error creating GameObject {change.gameObjectPath}: {ex.Message}");
        }

        return false;
    }

    /// <summary>
    /// Apply a deletion (remove GameObject)
    /// </summary>
    private static bool ApplyDeletion(Scene targetScene, GameObjectDiff change)
    {
        var targetObject = FindGameObjectInScene(targetScene, change.gameObjectPath);
        
        if (targetObject != null)
        {
            try
            {
                UnityEngine.Object.DestroyImmediate(targetObject);
                Debug.Log($"ScenePatcher: Deleted GameObject: {change.gameObjectPath}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"ScenePatcher: Error deleting GameObject {change.gameObjectPath}: {ex.Message}");
            }
        }
        else
        {
            Debug.LogWarning($"ScenePatcher: GameObject not found for deletion: {change.gameObjectPath}");
        }

        return false;
    }

    /// <summary>
    /// Apply modifications to existing GameObject
    /// </summary>
    private static bool ApplyModification(Scene targetScene, GameObjectDiff change)
    {
        var targetObject = FindGameObjectInScene(targetScene, change.gameObjectPath);
        
        if (targetObject == null)
        {
            Debug.LogWarning($"ScenePatcher: GameObject not found for modification: {change.gameObjectPath}");
            return false;
        }

        bool hasChanges = false;

        try
        {
            // Apply property changes
            if (ApplyPropertyChanges(targetObject, change.propertyChanges))
            {
                hasChanges = true;
            }

            // Apply component changes
            if (ApplyComponentChanges(targetObject, change.componentDiffs))
            {
                hasChanges = true;
            }

            if (hasChanges)
            {
                Debug.Log($"ScenePatcher: Modified GameObject: {change.gameObjectPath}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"ScenePatcher: Error modifying GameObject {change.gameObjectPath}: {ex.Message}");
        }

        return hasChanges;
    }

    /// <summary>
    /// Create a GameObject from GameObjectData
    /// </summary>
    private static GameObject CreateGameObjectFromData(GameObjectData data, Scene targetScene)
    {
        // Create the GameObject
        var go = new GameObject(data.name);
        
        // Move to target scene
        SceneManager.MoveGameObjectToScene(go, targetScene);
        
        // Set basic properties
        go.SetActive(data.isActive);
        go.tag = data.tag;
        go.layer = data.layer;
        
        // Set transform
        var transform = go.transform;
        transform.position = data.position;
        transform.rotation = data.rotation;
        transform.localScale = data.scale;
        
        // Add components
        foreach (var componentData in data.components)
        {
            try
            {
                CreateComponentFromData(go, componentData);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"ScenePatcher: Failed to create component {componentData.type}: {ex.Message}");
            }
        }
        
        // Create children recursively
        foreach (var childData in data.children)
        {
            var childObject = CreateGameObjectFromData(childData, targetScene);
            if (childObject != null)
            {
                childObject.transform.SetParent(transform);
            }
        }
        
        return go;
    }

    /// <summary>
    /// Create a component from ComponentData
    /// </summary>
    private static Component CreateComponentFromData(GameObject gameObject, ComponentData componentData)
    {
        // Get component type
        var componentType = Type.GetType(componentData.assemblyQualifiedName);
        if (componentType == null)
        {
            componentType = FindTypeByName(componentData.type);
        }
        
        if (componentType == null)
        {
            Debug.LogWarning($"ScenePatcher: Could not find component type: {componentData.type}");
            return null;
        }
        
        // Add component
        var component = gameObject.AddComponent(componentType);
        
        // Set properties
        ApplyComponentProperties(component, componentData.properties);
        
        return component;
    }

    /// <summary>
    /// Apply property changes to a GameObject
    /// </summary>
    private static bool ApplyPropertyChanges(GameObject gameObject, List<PropertyChange> propertyChanges)
    {
        bool hasChanges = false;

        foreach (var change in propertyChanges)
        {
            try
            {
                if (ApplyGameObjectProperty(gameObject, change))
                {
                    hasChanges = true;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"ScenePatcher: Failed to apply property {change.propertyName}: {ex.Message}");
            }
        }

        return hasChanges;
    }

    /// <summary>
    /// Apply component changes to a GameObject
    /// </summary>
    private static bool ApplyComponentChanges(GameObject gameObject, List<ComponentDiff> componentDiffs)
    {
        bool hasChanges = false;

        foreach (var componentDiff in componentDiffs)
        {
            try
            {
                switch (componentDiff.type)
                {
                    case ComponentDiffType.Added:
                        if (AddComponent(gameObject, componentDiff))
                        {
                            hasChanges = true;
                        }
                        break;
                        
                    case ComponentDiffType.Removed:
                        if (RemoveComponent(gameObject, componentDiff.componentType))
                        {
                            hasChanges = true;
                        }
                        break;
                        
                    case ComponentDiffType.Modified:
                        if (ModifyComponent(gameObject, componentDiff))
                        {
                            hasChanges = true;
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"ScenePatcher: Failed to apply component change {componentDiff.componentType}: {ex.Message}");
            }
        }

        return hasChanges;
    }

    /// <summary>
    /// Apply a property change to a GameObject
    /// </summary>
    private static bool ApplyGameObjectProperty(GameObject gameObject, PropertyChange change)
    {
        switch (change.propertyName)
        {
            case "isActive":
                bool activeValue = bool.Parse(change.newValue);
                if (gameObject.activeSelf != activeValue)
                {
                    gameObject.SetActive(activeValue);
                    return true;
                }
                break;
                
            case "tag":
                if (gameObject.tag != change.newValue)
                {
                    gameObject.tag = change.newValue;
                    return true;
                }
                break;
                
            case "layer":
                int layerValue = int.Parse(change.newValue);
                if (gameObject.layer != layerValue)
                {
                    gameObject.layer = layerValue;
                    return true;
                }
                break;
                
            case "position":
                Vector3 position = ParseVector3(change.newValue);
                if (gameObject.transform.position != position)
                {
                    gameObject.transform.position = position;
                    return true;
                }
                break;
                
            case "rotation":
                Quaternion rotation = ParseQuaternion(change.newValue);
                if (gameObject.transform.rotation != rotation)
                {
                    gameObject.transform.rotation = rotation;
                    return true;
                }
                break;
                
            case "scale":
                Vector3 scale = ParseVector3(change.newValue);
                if (gameObject.transform.localScale != scale)
                {
                    gameObject.transform.localScale = scale;
                    return true;
                }
                break;
        }

        return false;
    }

    /// <summary>
    /// Add a component to GameObject
    /// </summary>
    private static bool AddComponent(GameObject gameObject, ComponentDiff componentDiff)
    {
        var componentType = FindTypeByName(componentDiff.componentType);
        if (componentType == null)
        {
            Debug.LogWarning($"ScenePatcher: Could not find component type: {componentDiff.componentType}");
            return false;
        }

        var component = gameObject.AddComponent(componentType);
        
        // Apply property changes to the new component
        var propertyDict = componentDiff.propertyChanges.ToDictionary(
            p => p.propertyName, 
            p => p.newValue
        );
        
        ApplyComponentProperties(component, propertyDict);
        return true;
    }

    /// <summary>
    /// Remove a component from GameObject
    /// </summary>
    private static bool RemoveComponent(GameObject gameObject, string componentTypeName)
    {
        var componentType = FindTypeByName(componentTypeName);
        if (componentType == null)
        {
            return false;
        }

        var component = gameObject.GetComponent(componentType);
        if (component != null)
        {
            UnityEngine.Object.DestroyImmediate(component);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Modify an existing component
    /// </summary>
    private static bool ModifyComponent(GameObject gameObject, ComponentDiff componentDiff)
    {
        var componentType = FindTypeByName(componentDiff.componentType);
        if (componentType == null)
        {
            return false;
        }

        var component = gameObject.GetComponent(componentType);
        if (component == null)
        {
            return false;
        }

        var propertyDict = componentDiff.propertyChanges.ToDictionary(
            p => p.propertyName, 
            p => p.newValue
        );

        return ApplyComponentProperties(component, propertyDict);
    }

    /// <summary>
    /// Apply properties to a component using SerializedObject
    /// </summary>
    private static bool ApplyComponentProperties(Component component, Dictionary<string, string> properties)
    {
        if (component == null || properties.Count == 0)
            return false;

        bool hasChanges = false;
        var serializedObject = new SerializedObject(component);

        foreach (var kvp in properties)
        {
            var property = serializedObject.FindProperty(kvp.Key);
            if (property != null)
            {
                if (SetPropertyValue(property, kvp.Value))
                {
                    hasChanges = true;
                }
            }
        }

        if (hasChanges)
        {
            serializedObject.ApplyModifiedProperties();
        }

        return hasChanges;
    }

    /// <summary>
    /// Set a SerializedProperty value from string
    /// </summary>
    private static bool SetPropertyValue(SerializedProperty property, string value)
    {
        try
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.Integer:
                    int intValue = int.Parse(value);
                    if (property.intValue != intValue)
                    {
                        property.intValue = intValue;
                        return true;
                    }
                    break;
                    
                case SerializedPropertyType.Boolean:
                    bool boolValue = bool.Parse(value);
                    if (property.boolValue != boolValue)
                    {
                        property.boolValue = boolValue;
                        return true;
                    }
                    break;
                    
                case SerializedPropertyType.Float:
                    float floatValue = float.Parse(value);
                    if (Mathf.Abs(property.floatValue - floatValue) > Mathf.Epsilon)
                    {
                        property.floatValue = floatValue;
                        return true;
                    }
                    break;
                    
                case SerializedPropertyType.String:
                    if (property.stringValue != value)
                    {
                        property.stringValue = value;
                        return true;
                    }
                    break;
                    
                case SerializedPropertyType.Vector2:
                    Vector2 vector2Value = ParseVector2(value);
                    if (property.vector2Value != vector2Value)
                    {
                        property.vector2Value = vector2Value;
                        return true;
                    }
                    break;
                    
                case SerializedPropertyType.Vector3:
                    Vector3 vector3Value = ParseVector3(value);
                    if (property.vector3Value != vector3Value)
                    {
                        property.vector3Value = vector3Value;
                        return true;
                    }
                    break;
                    
                case SerializedPropertyType.Quaternion:
                    Quaternion quaternionValue = ParseQuaternion(value);
                    if (property.quaternionValue != quaternionValue)
                    {
                        property.quaternionValue = quaternionValue;
                        return true;
                    }
                    break;
                    
                case SerializedPropertyType.Color:
                    Color colorValue = ParseColor(value);
                    if (property.colorValue != colorValue)
                    {
                        property.colorValue = colorValue;
                        return true;
                    }
                    break;
                    
                // Handle other property types as needed
                default:
                    Debug.LogWarning($"ScenePatcher: Unsupported property type: {property.propertyType}");
                    break;
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"ScenePatcher: Failed to set property {property.name}: {ex.Message}");
        }

        return false;
    }

    /// <summary>
    /// Find GameObject in scene by path
    /// </summary>
    private static GameObject FindGameObjectInScene(Scene scene, string path)
    {
        var rootObjects = scene.GetRootGameObjects();
        
        foreach (var rootObject in rootObjects)
        {
            var found = FindGameObjectByPath(rootObject, path);
            if (found != null)
                return found;
        }
        
        return null;
    }

    /// <summary>
    /// Find GameObject by path recursively
    /// </summary>
    private static GameObject FindGameObjectByPath(GameObject rootObject, string targetPath)
    {
        if (GetGameObjectPath(rootObject) == targetPath)
            return rootObject;
            
        for (int i = 0; i < rootObject.transform.childCount; i++)
        {
            var child = rootObject.transform.GetChild(i).gameObject;
            var found = FindGameObjectByPath(child, targetPath);
            if (found != null)
                return found;
        }
        
        return null;
    }

    /// <summary>
    /// Get GameObject path
    /// </summary>
    private static string GetGameObjectPath(GameObject go)
    {
        string path = go.name;
        Transform parent = go.transform.parent;
        
        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }
        
        return path;
    }

    /// <summary>
    /// Find type by name
    /// </summary>
    private static Type FindTypeByName(string typeName)
    {
        // First try the simple name
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            var type = assembly.GetType(typeName);
            if (type != null)
                return type;
                
            // Try with UnityEngine namespace
            type = assembly.GetType($"UnityEngine.{typeName}");
            if (type != null)
                return type;
        }
        
        return null;
    }

    // Parsing utility methods
    private static Vector2 ParseVector2(string value)
    {
        value = value.Trim('(', ')');
        var parts = value.Split(',');
        return new Vector2(float.Parse(parts[0]), float.Parse(parts[1]));
    }

    private static Vector3 ParseVector3(string value)
    {
        value = value.Trim('(', ')');
        var parts = value.Split(',');
        return new Vector3(float.Parse(parts[0]), float.Parse(parts[1]), float.Parse(parts[2]));
    }

    private static Quaternion ParseQuaternion(string value)
    {
        value = value.Trim('(', ')');
        var parts = value.Split(',');
        return new Quaternion(float.Parse(parts[0]), float.Parse(parts[1]), float.Parse(parts[2]), float.Parse(parts[3]));
    }

    private static Color ParseColor(string value)
    {
        value = value.Replace("RGBA", "").Trim('(', ')');
        var parts = value.Split(',');
        return new Color(float.Parse(parts[0]), float.Parse(parts[1]), float.Parse(parts[2]), float.Parse(parts[3]));
    }
}