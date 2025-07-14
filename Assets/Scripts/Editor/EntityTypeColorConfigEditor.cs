using UnityEngine;
using UnityEditor;
using System.Linq;

[CustomEditor(typeof(EntityTypeColorConfig))]
public class EntityTypeColorConfigEditor : Editor
{
    private EntityTypeColorConfig config;
    private Vector2 scrollPosition;
    private bool showIconPresets = false;
    private int selectedTypeIndex = -1;
    
    public override void OnInspectorGUI()
    {
        config = (EntityTypeColorConfig)target;
        serializedObject.Update();
        
        DrawHeader();
        EditorGUILayout.Space(10);
        
        DrawAutoDiscoverySettings();
        EditorGUILayout.Space(10);
        
        DrawDefaultSettings();
        EditorGUILayout.Space(10);
        
        DrawEntityTypesList();
        EditorGUILayout.Space(10);
        
        DrawIconPresets();
        EditorGUILayout.Space(10);
        
        DrawActions();
        
        serializedObject.ApplyModifiedProperties();
    }
    
    private void DrawHeader()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        var headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 16,
            alignment = TextAnchor.MiddleCenter
        };
        
        EditorGUILayout.LabelField("🎨 Entity Type Color Configuration", headerStyle);
        EditorGUILayout.LabelField("Manage colors and icons for entity types in your project", EditorStyles.centeredGreyMiniLabel);
        
        EditorGUILayout.EndVertical();
    }
    
    private void DrawAutoDiscoverySettings()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("🔍 Auto-Discovery Settings", EditorStyles.boldLabel);
        
        EditorGUILayout.PropertyField(serializedObject.FindProperty("autoDiscoverNewTypes"), 
            new GUIContent("Auto Discover New Types", "Automatically find and configure new entity types when scripts are compiled"));
        
        EditorGUILayout.PropertyField(serializedObject.FindProperty("assignRandomColorsToNewTypes"), 
            new GUIContent("Assign Random Colors", "Automatically assign random colors to newly discovered types"));
        
        if (config.assignRandomColorsToNewTypes)
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("randomColorPalette"), 
                new GUIContent("Random Color Palette", "Colors to choose from when auto-assigning"), true);
        }
        
        EditorGUILayout.EndVertical();
    }
    
    private void DrawDefaultSettings()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("⚙️ Default Settings", EditorStyles.boldLabel);
        
        EditorGUILayout.PropertyField(serializedObject.FindProperty("defaultColor"), 
            new GUIContent("Default Color", "Color used for types not explicitly configured"));
        
        EditorGUILayout.PropertyField(serializedObject.FindProperty("defaultIcon"), 
            new GUIContent("Default Icon", "Icon used for types not explicitly configured"));
        
        EditorGUILayout.EndVertical();
    }
    
    private void DrawEntityTypesList()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("🏗️ Entity Type Colors", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Add Type", GUILayout.Width(80)))
        {
            config.entityTypeColors.Add(new EntityTypeColorConfig.EntityTypeColor());
            EditorUtility.SetDirty(config);
        }
        
        if (GUILayout.Button("Scan Types", GUILayout.Width(80)))
        {
            EntityTypeAutoDiscovery.ScanForEntityTypes();
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(5);
        
        if (config.entityTypeColors.Count == 0)
        {
            EditorGUILayout.HelpBox("No entity types configured. Click 'Scan Types' to auto-discover entity types in your project, or 'Add Type' to manually add one.", MessageType.Info);
        }
        else
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.MaxHeight(300));
            
            for (int i = 0; i < config.entityTypeColors.Count; i++)
            {
                DrawEntityTypeColor(i);
                
                if (i < config.entityTypeColors.Count - 1)
                {
                    EditorGUILayout.Space(3);
                    EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
                    EditorGUILayout.Space(3);
                }
            }
            
            EditorGUILayout.EndScrollView();
        }
        
        EditorGUILayout.EndVertical();
    }
    
    private void DrawEntityTypeColor(int index)
    {
        var entityType = config.entityTypeColors[index];
        
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        EditorGUILayout.BeginHorizontal();
        
        // Color preview box
        var colorRect = GUILayoutUtility.GetRect(20, 20, GUILayout.Width(20));
        EditorGUI.DrawRect(colorRect, entityType.color);
        // Draw border
        var borderRect = new Rect(colorRect.x, colorRect.y, colorRect.width, colorRect.height);
        EditorGUI.DrawRect(new Rect(borderRect.x, borderRect.y, borderRect.width, 1), Color.black);
        EditorGUI.DrawRect(new Rect(borderRect.x, borderRect.y + borderRect.height - 1, borderRect.width, 1), Color.black);
        EditorGUI.DrawRect(new Rect(borderRect.x, borderRect.y, 1, borderRect.height), Color.black);
        EditorGUI.DrawRect(new Rect(borderRect.x + borderRect.width - 1, borderRect.y, 1, borderRect.height), Color.black);
        
        // Type name
        entityType.typeName = EditorGUILayout.TextField(entityType.typeName, GUILayout.MinWidth(100));
        
        // Icon
        entityType.icon = EditorGUILayout.TextField(entityType.icon, GUILayout.Width(40));
        
        // Core type indicator
        if (entityType.isCoreType)
        {
            EditorGUILayout.LabelField("🔒", EditorStyles.miniLabel, GUILayout.Width(20));
        }
        else
        {
            GUILayout.Space(20);
        }
        
        // Remove button (only for non-core types)
        GUI.enabled = !entityType.isCoreType;
        if (GUILayout.Button("✖", GUILayout.Width(25)))
        {
            config.entityTypeColors.RemoveAt(index);
            EditorUtility.SetDirty(config);
            return;
        }
        GUI.enabled = true;
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        
        // Color picker
        var newColor = EditorGUILayout.ColorField(new GUIContent("", "Choose color for this entity type"), 
                                                 entityType.color, true, true, false, GUILayout.Width(50));
        if (newColor != entityType.color)
        {
            entityType.color = newColor;
            EditorUtility.SetDirty(config);
        }
        
        // Random color button
        if (GUILayout.Button("🎲", GUILayout.Width(25)))
        {
            entityType.color = config.GetRandomColor();
            EditorUtility.SetDirty(config);
        }
        
        // Icon presets button
        if (GUILayout.Button("🎭", GUILayout.Width(25)))
        {
            selectedTypeIndex = index;
            showIconPresets = !showIconPresets;
        }
        
        // Description
        entityType.description = EditorGUILayout.TextField(entityType.description);
        
        EditorGUILayout.EndHorizontal();
        
        // Core type toggle
        GUI.enabled = !entityType.isCoreType;
        var wasCoreType = entityType.isCoreType;
        entityType.isCoreType = EditorGUILayout.Toggle("Core Type (Protected)", entityType.isCoreType);
        if (entityType.isCoreType != wasCoreType)
        {
            EditorUtility.SetDirty(config);
        }
        GUI.enabled = true;
        
        EditorGUILayout.EndVertical();
    }
    
    private void DrawIconPresets()
    {
        if (!showIconPresets || selectedTypeIndex < 0 || selectedTypeIndex >= config.entityTypeColors.Count)
            return;
            
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("🎭 Icon Presets", EditorStyles.boldLabel);
        
        int iconsPerRow = 8;
        for (int i = 0; i < config.iconPresets.Length; i += iconsPerRow)
        {
            EditorGUILayout.BeginHorizontal();
            
            for (int j = 0; j < iconsPerRow && i + j < config.iconPresets.Length; j++)
            {
                string icon = config.iconPresets[i + j];
                
                if (GUILayout.Button(icon, GUILayout.Width(30), GUILayout.Height(25)))
                {
                    config.entityTypeColors[selectedTypeIndex].icon = icon;
                    showIconPresets = false;
                    selectedTypeIndex = -1;
                    EditorUtility.SetDirty(config);
                }
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        EditorGUILayout.EndVertical();
    }
    
    private void DrawActions()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("🛠️ Actions", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("Initialize with Defaults"))
        {
            if (EditorUtility.DisplayDialog("Initialize with Defaults", 
                "This will reset all configurations to default values. Continue?", 
                "Initialize", "Cancel"))
            {
                config.InitializeWithDefaults();
            }
        }
        
        if (GUILayout.Button("Scan for New Types"))
        {
            var discoveredTypes = EntityTypeAutoDiscovery.ScanForEntityTypes();
            Debug.Log($"Discovered {discoveredTypes.Count} entity types");
        }
        
        if (GUILayout.Button("Refresh Windows"))
        {
            EntityTypeAutoDiscovery.RefreshConfigCache();
            
            // Refresh hierarchy windows
            var hierarchyWindows = Resources.FindObjectsOfTypeAll<EntityHierarchyWindow>();
            foreach (var window in hierarchyWindows)
            {
                window.Repaint();
            }
        }
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("Export Configuration"))
        {
            ExportConfiguration();
        }
        
        if (GUILayout.Button("Import Configuration"))
        {
            ImportConfiguration();
        }
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.EndVertical();
    }
    
    private void ExportConfiguration()
    {
        string path = EditorUtility.SaveFilePanel("Export Entity Type Configuration", "", "EntityTypeConfig", "json");
        
        if (!string.IsNullOrEmpty(path))
        {
            var exportData = new
            {
                entityTypes = config.entityTypeColors.Select(etc => new {
                    typeName = etc.typeName,
                    color = new { r = etc.color.r, g = etc.color.g, b = etc.color.b, a = etc.color.a },
                    icon = etc.icon,
                    description = etc.description,
                    isCoreType = etc.isCoreType
                }).ToArray(),
                defaultColor = new { r = config.defaultColor.r, g = config.defaultColor.g, b = config.defaultColor.b, a = config.defaultColor.a },
                defaultIcon = config.defaultIcon,
                autoDiscoverNewTypes = config.autoDiscoverNewTypes,
                assignRandomColorsToNewTypes = config.assignRandomColorsToNewTypes
            };
            
            string json = JsonUtility.ToJson(exportData, true);
            System.IO.File.WriteAllText(path, json);
            
            Debug.Log($"Configuration exported to: {path}");
        }
    }
    
    private void ImportConfiguration()
    {
        string path = EditorUtility.OpenFilePanel("Import Entity Type Configuration", "", "json");
        
        if (!string.IsNullOrEmpty(path))
        {
            try
            {
                string json = System.IO.File.ReadAllText(path);
                // Note: Full import implementation would require custom JSON parsing
                // This is a placeholder for the import functionality
                Debug.Log("Import functionality would be implemented here");
                EditorUtility.DisplayDialog("Import", "Import functionality is not yet implemented in this example.", "OK");
            }
            catch (System.Exception ex)
            {
                EditorUtility.DisplayDialog("Import Error", $"Failed to import configuration: {ex.Message}", "OK");
            }
        }
    }
}