using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;
using System;

public class SceneComparisonTool : EditorWindow
{
    [MenuItem("Tools/Scene Management/Scene Comparison Tool")]
    public static void ShowWindow()
    {
        var window = GetWindow<SceneComparisonTool>("Scene Comparison");
        window.minSize = new Vector2(800, 600);
        window.Show();
    }

    private SceneAsset currentScene;
    private SceneAsset targetScene;
    private SceneData currentSceneData;
    private SceneData targetSceneData;
    private List<GameObjectDiff> differences = new List<GameObjectDiff>();
    private Vector2 scrollPosition;
    private bool showAdditions = true;
    private bool showDeletions = true;
    private bool showModifications = true;
    private bool autoRefresh = false;
    private string searchFilter = "";
    private DiffCategory selectedCategory = DiffCategory.All;

    private enum DiffCategory
    {
        All,
        Additions,
        Deletions,
        Modifications
    }

    private void OnGUI()
    {
        DrawHeader();
        EditorGUILayout.Space(10);
        
        DrawSceneSelection();
        EditorGUILayout.Space(10);
        
        if (currentSceneData != null && targetSceneData != null)
        {
            DrawComparisonControls();
            EditorGUILayout.Space(10);
            
            DrawDifferencesList();
        }
        else
        {
            DrawInstructions();
        }
    }

    private void DrawHeader()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        var titleStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 16,
            alignment = TextAnchor.MiddleCenter
        };
        
        EditorGUILayout.LabelField("🔄 Scene Comparison Tool", titleStyle);
        EditorGUILayout.LabelField("Compare and synchronize changes between scenes", EditorStyles.centeredGreyMiniLabel);
        
        EditorGUILayout.EndVertical();
    }

    private void DrawSceneSelection()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("📁 Scene Selection", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Current Scene (Source):", GUILayout.Width(150));
        var newCurrentScene = (SceneAsset)EditorGUILayout.ObjectField(currentScene, typeof(SceneAsset), false);
        
        if (GUILayout.Button("Use Active", GUILayout.Width(80)))
        {
            var activeScene = SceneManager.GetActiveScene();
            if (!string.IsNullOrEmpty(activeScene.path))
            {
                newCurrentScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(activeScene.path);
            }
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Target Scene (Destination):", GUILayout.Width(150));
        var newTargetScene = (SceneAsset)EditorGUILayout.ObjectField(targetScene, typeof(SceneAsset), false);
        EditorGUILayout.Space(84); // Align with button above
        EditorGUILayout.EndHorizontal();
        
        bool scenesChanged = newCurrentScene != currentScene || newTargetScene != targetScene;
        currentScene = newCurrentScene;
        targetScene = newTargetScene;
        
        EditorGUILayout.Space(5);
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("🔍 Analyze Scenes", GUILayout.Height(25)))
        {
            AnalyzeScenes();
        }
        
        autoRefresh = EditorGUILayout.Toggle("Auto Refresh", autoRefresh);
        EditorGUILayout.EndHorizontal();
        
        if (autoRefresh && scenesChanged && currentScene != null && targetScene != null)
        {
            AnalyzeScenes();
        }
        
        EditorGUILayout.EndVertical();
    }

    private void DrawComparisonControls()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("🎛️ Comparison Controls", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        searchFilter = EditorGUILayout.TextField("Search:", searchFilter, GUILayout.ExpandWidth(true));
        
        selectedCategory = (DiffCategory)EditorGUILayout.EnumPopup("Category:", selectedCategory, GUILayout.Width(200));
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        showAdditions = EditorGUILayout.Toggle("➕ Additions", showAdditions);
        showDeletions = EditorGUILayout.Toggle("➖ Deletions", showDeletions);
        showModifications = EditorGUILayout.Toggle("🔄 Modifications", showModifications);
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("📋 Select All Visible"))
        {
            SelectAllVisible(true);
        }
        
        if (GUILayout.Button("🚫 Deselect All"))
        {
            SelectAllVisible(false);
        }
        
        if (GUILayout.Button("🎯 Apply Selected Changes"))
        {
            ApplySelectedChanges();
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.EndVertical();
    }

    private void DrawDifferencesList()
    {
        var filteredDifferences = GetFilteredDifferences();
        
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField($"📊 Differences Found: {filteredDifferences.Count}", EditorStyles.boldLabel);
        
        if (filteredDifferences.Count == 0)
        {
            EditorGUILayout.HelpBox("No differences found with current filters.", MessageType.Info);
            EditorGUILayout.EndVertical();
            return;
        }
        
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        foreach (var diff in filteredDifferences)
        {
            DrawDifferenceItem(diff);
            EditorGUILayout.Space(2);
        }
        
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private void DrawDifferenceItem(GameObjectDiff diff)
    {
        var bgColor = GetDiffBackgroundColor(diff.type);
        var originalColor = GUI.backgroundColor;
        GUI.backgroundColor = bgColor;
        
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUI.backgroundColor = originalColor;
        
        EditorGUILayout.BeginHorizontal();
        
        // Selection checkbox
        diff.isSelected = EditorGUILayout.Toggle(diff.isSelected, GUILayout.Width(20));
        
        // Diff icon and type
        var icon = GetDiffIcon(diff.type);
        EditorGUILayout.LabelField(icon, GUILayout.Width(25));
        
        // GameObject path
        var pathStyle = new GUIStyle(EditorStyles.boldLabel);
        EditorGUILayout.LabelField(diff.gameObjectPath, pathStyle);
        
        // Actions
        if (GUILayout.Button("👁️", GUILayout.Width(25)))
        {
            HighlightGameObject(diff);
        }
        
        if (GUILayout.Button("🎯", GUILayout.Width(25)))
        {
            ApplyIndividualChange(diff);
        }
        
        EditorGUILayout.EndHorizontal();
        
        // Diff details
        if (diff.componentDiffs.Count > 0)
        {
            EditorGUI.indentLevel++;
            foreach (var componentDiff in diff.componentDiffs)
            {
                DrawComponentDiff(componentDiff);
            }
            EditorGUI.indentLevel--;
        }
        
        EditorGUILayout.EndVertical();
    }

    private void DrawComponentDiff(ComponentDiff componentDiff)
    {
        EditorGUILayout.BeginHorizontal();
        
        var componentIcon = GetComponentIcon(componentDiff.type);
        EditorGUILayout.LabelField($"{componentIcon} {componentDiff.componentType}", EditorStyles.miniLabel, GUILayout.ExpandWidth(true));
        
        if (componentDiff.propertyChanges.Count > 0)
        {
            EditorGUILayout.LabelField($"({componentDiff.propertyChanges.Count} changes)", EditorStyles.miniLabel, GUILayout.Width(80));
        }
        
        EditorGUILayout.EndHorizontal();
        
        // Show property changes if any
        if (componentDiff.propertyChanges.Count > 0)
        {
            EditorGUI.indentLevel++;
            foreach (var propertyChange in componentDiff.propertyChanges.Take(3)) // Show first 3
            {
                EditorGUILayout.LabelField($"• {propertyChange.propertyName}: {propertyChange.oldValue} → {propertyChange.newValue}", EditorStyles.miniLabel);
            }
            
            if (componentDiff.propertyChanges.Count > 3)
            {
                EditorGUILayout.LabelField($"... and {componentDiff.propertyChanges.Count - 3} more", EditorStyles.miniLabel);
            }
            EditorGUI.indentLevel--;
        }
    }

    private void DrawInstructions()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("📋 Instructions", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("1. Select your Current Scene (source of changes)");
        EditorGUILayout.LabelField("2. Select your Target Scene (destination for changes)");
        EditorGUILayout.LabelField("3. Click 'Analyze Scenes' to compare them");
        EditorGUILayout.LabelField("4. Review differences and apply selected changes");
        EditorGUILayout.EndVertical();
    }

    private void AnalyzeScenes()
    {
        if (currentScene == null || targetScene == null)
        {
            EditorUtility.DisplayDialog("Missing Scenes", "Please select both current and target scenes.", "OK");
            return;
        }

        try
        {
            EditorUtility.DisplayProgressBar("Analyzing Scenes", "Loading scene data...", 0f);
            
            currentSceneData = LoadSceneData(AssetDatabase.GetAssetPath(currentScene));
            EditorUtility.DisplayProgressBar("Analyzing Scenes", "Loading target scene...", 0.5f);
            
            targetSceneData = LoadSceneData(AssetDatabase.GetAssetPath(targetScene));
            EditorUtility.DisplayProgressBar("Analyzing Scenes", "Comparing scenes...", 0.8f);
            
            differences = SceneDiffAnalyzer.CompareScenes(currentSceneData, targetSceneData);
            
            EditorUtility.DisplayProgressBar("Analyzing Scenes", "Analysis complete!", 1f);
            
            Debug.Log($"Scene comparison complete. Found {differences.Count} differences.");
        }
        catch (Exception ex)
        {
            EditorUtility.DisplayDialog("Analysis Error", $"Error analyzing scenes: {ex.Message}", "OK");
            Debug.LogError($"Scene analysis error: {ex}");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    private SceneData LoadSceneData(string scenePath)
    {
        var originalScene = SceneManager.GetActiveScene();
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
        
        try
        {
            var sceneData = new SceneData();
            sceneData.scenePath = scenePath;
            sceneData.sceneName = scene.name;
            
            var rootObjects = scene.GetRootGameObjects();
            foreach (var rootObject in rootObjects)
            {
                var gameObjectData = ExtractGameObjectData(rootObject, "");
                sceneData.gameObjects.Add(gameObjectData);
            }
            
            return sceneData;
        }
        finally
        {
            if (scene != originalScene)
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }

    private GameObjectData ExtractGameObjectData(GameObject go, string parentPath)
    {
        var data = new GameObjectData();
        data.name = go.name;
        data.path = string.IsNullOrEmpty(parentPath) ? go.name : $"{parentPath}/{go.name}";
        data.isActive = go.activeInHierarchy;
        data.tag = go.tag;
        data.layer = go.layer;
        
        // Extract transform data
        var transform = go.transform;
        data.position = transform.position;
        data.rotation = transform.rotation;
        data.scale = transform.localScale;
        
        // Extract component data
        var components = go.GetComponents<Component>();
        foreach (var component in components)
        {
            if (component != null && !(component is Transform))
            {
                var componentData = ExtractComponentData(component);
                data.components.Add(componentData);
            }
        }
        
        // Extract children
        for (int i = 0; i < transform.childCount; i++)
        {
            var child = transform.GetChild(i);
            var childData = ExtractGameObjectData(child.gameObject, data.path);
            data.children.Add(childData);
        }
        
        return data;
    }

    private ComponentData ExtractComponentData(Component component)
    {
        var data = new ComponentData();
        data.type = component.GetType().Name;
        data.assemblyQualifiedName = component.GetType().AssemblyQualifiedName;
        
        // Use SerializedObject to get property values
        var serializedObject = new SerializedObject(component);
        var property = serializedObject.GetIterator();
        
        if (property.NextVisible(true))
        {
            do
            {
                if (property.name != "m_Script")
                {
                    data.properties[property.name] = GetPropertyValue(property);
                }
            }
            while (property.NextVisible(false));
        }
        
        return data;
    }

    private string GetPropertyValue(SerializedProperty property)
    {
        switch (property.propertyType)
        {
            case SerializedPropertyType.Integer:
                return property.intValue.ToString();
            case SerializedPropertyType.Boolean:
                return property.boolValue.ToString();
            case SerializedPropertyType.Float:
                return property.floatValue.ToString("F6");
            case SerializedPropertyType.String:
                return property.stringValue ?? "null";
            case SerializedPropertyType.Vector2:
                return property.vector2Value.ToString();
            case SerializedPropertyType.Vector3:
                return property.vector3Value.ToString();
            case SerializedPropertyType.Vector4:
                return property.vector4Value.ToString();
            case SerializedPropertyType.Quaternion:
                return property.quaternionValue.ToString();
            case SerializedPropertyType.Color:
                return property.colorValue.ToString();
            case SerializedPropertyType.ObjectReference:
                return property.objectReferenceValue ? property.objectReferenceValue.name : "null";
            case SerializedPropertyType.Enum:
                return property.enumNames[property.enumValueIndex];
            default:
                return property.propertyType.ToString();
        }
    }

    private List<GameObjectDiff> GetFilteredDifferences()
    {
        return differences.Where(diff =>
        {
            // Category filter
            if (selectedCategory != DiffCategory.All)
            {
                if (selectedCategory == DiffCategory.Additions && diff.type != DiffType.Addition)
                    return false;
                if (selectedCategory == DiffCategory.Deletions && diff.type != DiffType.Deletion)
                    return false;
                if (selectedCategory == DiffCategory.Modifications && diff.type != DiffType.Modification)
                    return false;
            }
            
            // Type filter
            if (diff.type == DiffType.Addition && !showAdditions)
                return false;
            if (diff.type == DiffType.Deletion && !showDeletions)
                return false;
            if (diff.type == DiffType.Modification && !showModifications)
                return false;
            
            // Search filter
            if (!string.IsNullOrEmpty(searchFilter) && 
                !diff.gameObjectPath.ToLower().Contains(searchFilter.ToLower()))
                return false;
            
            return true;
        }).ToList();
    }

    private void SelectAllVisible(bool select)
    {
        var filtered = GetFilteredDifferences();
        foreach (var diff in filtered)
        {
            diff.isSelected = select;
        }
    }

    private void ApplySelectedChanges()
    {
        var selectedDiffs = differences.Where(d => d.isSelected).ToList();
        if (selectedDiffs.Count == 0)
        {
            EditorUtility.DisplayDialog("No Selection", "Please select changes to apply.", "OK");
            return;
        }

        if (EditorUtility.DisplayDialog("Apply Changes", 
            $"Apply {selectedDiffs.Count} selected changes to {targetScene.name}?", 
            "Apply", "Cancel"))
        {
            ScenePatcher.ApplyChanges(targetScene, selectedDiffs);
            EditorUtility.DisplayDialog("Success", "Changes applied successfully!", "OK");
            
            // Refresh analysis
            AnalyzeScenes();
        }
    }

    private void ApplyIndividualChange(GameObjectDiff diff)
    {
        if (EditorUtility.DisplayDialog("Apply Change", 
            $"Apply change to {diff.gameObjectPath}?", 
            "Apply", "Cancel"))
        {
            ScenePatcher.ApplyChanges(targetScene, new List<GameObjectDiff> { diff });
            EditorUtility.DisplayDialog("Success", "Change applied successfully!", "OK");
            
            // Refresh analysis
            AnalyzeScenes();
        }
    }

    private void HighlightGameObject(GameObjectDiff diff)
    {
        // This would highlight the GameObject in the scene view
        Debug.Log($"Highlighting: {diff.gameObjectPath}");
    }

    private Color GetDiffBackgroundColor(DiffType type)
    {
        switch (type)
        {
            case DiffType.Addition:
                return new Color(0.2f, 0.8f, 0.2f, 0.3f);
            case DiffType.Deletion:
                return new Color(0.8f, 0.2f, 0.2f, 0.3f);
            case DiffType.Modification:
                return new Color(0.8f, 0.8f, 0.2f, 0.3f);
            default:
                return Color.white;
        }
    }

    private string GetDiffIcon(DiffType type)
    {
        switch (type)
        {
            case DiffType.Addition:
                return "➕";
            case DiffType.Deletion:
                return "➖";
            case DiffType.Modification:
                return "🔄";
            default:
                return "❓";
        }
    }

    private string GetComponentIcon(ComponentDiffType type)
    {
        switch (type)
        {
            case ComponentDiffType.Added:
                return "➕";
            case ComponentDiffType.Removed:
                return "➖";
            case ComponentDiffType.Modified:
                return "🔄";
            default:
                return "🔧";
        }
    }
}