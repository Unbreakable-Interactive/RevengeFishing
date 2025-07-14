using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

public class EntityHierarchyWindow : EditorWindow
{
    private Vector2 scrollPosition;
    private Dictionary<System.Type, Vector2> typePositions = new Dictionary<System.Type, Vector2>();
    private static Dictionary<Color, Texture2D> textureCache = new Dictionary<Color, Texture2D>();
    private EntityTypeColorConfig colorConfig;
    private List<System.Type> discoveredEntityTypes = new List<System.Type>();
    private double lastRefreshTime = 0;
    private bool autoRefresh = true;

    [MenuItem("Tools/Entity System/Entity Hierarchy Visualizer")]
    public static void ShowWindow()
    {
        var window = GetWindow<EntityHierarchyWindow>("Entity Hierarchy");
        window.minSize = new Vector2(400, 300);
        window.Show();
    }
    
    private void OnEnable()
    {
        // Subscribe to entity type discovery events
        EntityTypeAutoDiscovery.OnEntityTypesDiscovered += OnEntityTypesDiscovered;
        EntityTypeAutoDiscovery.OnNewEntityTypeFound += OnNewEntityTypeFound;
        
        // Initial refresh
        RefreshEntityTypes();
    }
    
    private void OnDisable()
    {
        // Unsubscribe from events
        EntityTypeAutoDiscovery.OnEntityTypesDiscovered -= OnEntityTypesDiscovered;
        EntityTypeAutoDiscovery.OnNewEntityTypeFound -= OnNewEntityTypeFound;
    }
    
    private void OnEntityTypesDiscovered(List<System.Type> types)
    {
        discoveredEntityTypes = types;
        Repaint();
    }
    
    private void OnNewEntityTypeFound(System.Type type)
    {
        Debug.Log($"🆕 EntityHierarchyWindow: New entity type detected: {type.Name}");
        RefreshEntityTypes();
    }
    
    private void RefreshEntityTypes()
    {
        colorConfig = EntityTypeAutoDiscovery.GetOrCreateConfig();
        discoveredEntityTypes = EntityTypeAutoDiscovery.ScanForEntityTypes();
        lastRefreshTime = EditorApplication.timeSinceStartup;
        
        if (discoveredEntityTypes.Count == 0)
        {
            // Fallback to hardcoded types if discovery fails
            discoveredEntityTypes = new List<System.Type>
            {
                typeof(Entity),
                typeof(Player),
                typeof(Enemy),
                typeof(LandEnemy),
                typeof(Fisherman),
                typeof(FishingProjectile),
                typeof(FishingHook),
                typeof(DroppedTool)
            }.Where(t => t != null).ToList();
        }
    }

    private void OnGUI()
    {
        // Auto-refresh check (throttled)
        if (autoRefresh && EditorApplication.timeSinceStartup - lastRefreshTime > 10.0) // Refresh every 10 seconds max
        {
            RefreshEntityTypes();
        }
        
        DrawHeader();
        EditorGUILayout.Space(10);
        
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        DrawHierarchyTree();
        
        EditorGUILayout.EndScrollView();
        
        EditorGUILayout.Space(10);
        DrawLegend();
        DrawControls();
    }

    private void DrawHeader()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        
        var headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 14,
            normal = { textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black }
        };
        
        GUILayout.FlexibleSpace();
        EditorGUILayout.LabelField("🏗️ Entity Inheritance Hierarchy", headerStyle);
        GUILayout.FlexibleSpace();
        
        EditorGUILayout.EndHorizontal();
    }

    private void DrawHierarchyTree()
    {
        EditorGUILayout.BeginVertical();
        
        if (discoveredEntityTypes.Count == 0)
        {
            EditorGUILayout.HelpBox("No entity types discovered. Make sure you have Entity scripts in your project.", MessageType.Info);
            if (GUILayout.Button("Refresh Entity Types"))
            {
                RefreshEntityTypes();
            }
            EditorGUILayout.EndVertical();
            return;
        }
        
        // Build hierarchy based on inheritance
        var rootTypes = discoveredEntityTypes.Where(t => IsRootEntityType(t)).ToList();
        
        foreach (var rootType in rootTypes)
        {
            DrawTypeHierarchy(rootType, 0);
            EditorGUILayout.Space(20);
        }
        
        EditorGUILayout.EndVertical();
    }
    
    private bool IsRootEntityType(System.Type type)
    {
        // A type is root if its base type is not in our discovered entity types
        var baseType = type.BaseType;
        while (baseType != null && baseType != typeof(MonoBehaviour))
        {
            if (discoveredEntityTypes.Contains(baseType))
            {
                return false;
            }
            baseType = baseType.BaseType;
        }
        return true;
    }
    
    private void DrawTypeHierarchy(System.Type type, int level)
    {
        // Draw current type
        if (level > 0)
        {
            DrawConnection();
        }
        
        DrawTypeBox(type, level);
        
        // Find and draw children
        var children = discoveredEntityTypes.Where(t => t.BaseType == type).ToList();
        
        if (children.Count > 0)
        {
            EditorGUILayout.Space(10);
            
            if (children.Count == 1)
            {
                // Single child - draw directly below
                DrawTypeHierarchy(children[0], level + 1);
            }
            else
            {
                // Multiple children - draw side by side
                EditorGUILayout.BeginHorizontal();
                
                foreach (var child in children)
                {
                    EditorGUILayout.BeginVertical(GUILayout.Width(150));
                    DrawTypeHierarchy(child, level + 1);
                    EditorGUILayout.EndVertical();
                    
                    if (child != children.Last())
                    {
                        GUILayout.FlexibleSpace();
                    }
                }
                
                EditorGUILayout.EndHorizontal();
            }
        }
    }

    private void DrawTypeBox(System.Type type, int level)
    {
        Color typeColor = GetColorForType(type);
        string icon = GetIconForType(type);
        
        var boxStyle = new GUIStyle(GUI.skin.box)
        {
            normal = { background = CreateColorTexture(typeColor) },
            border = new RectOffset(2, 2, 2, 2),
            padding = new RectOffset(10, 10, 8, 8),
            alignment = TextAnchor.MiddleCenter
        };
        
        var labelStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 12,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white }
        };
        
        EditorGUILayout.BeginVertical(boxStyle);
        
        // Type name with icon
        EditorGUILayout.LabelField($"{icon} {type.Name}", labelStyle);
        
        // Show field count
        var fieldCount = GetFieldCountForType(type);
        if (fieldCount > 0)
        {
            var countStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white * 0.8f },
                fontStyle = FontStyle.Italic
            };
            EditorGUILayout.LabelField($"{fieldCount} fields", countStyle);
        }
        
        // Add inheritance info
        if (type.BaseType != null && type.BaseType != typeof(MonoBehaviour))
        {
            var inheritanceStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Italic,
                normal = { textColor = Color.white * 0.7f }
            };
            EditorGUILayout.LabelField($"extends {type.BaseType.Name}", inheritanceStyle);
        }
        
        EditorGUILayout.EndVertical();
        
        // Add selection button
        if (GUILayout.Button($"Find {type.Name}s in Scene", EditorStyles.miniButton))
        {
            FindTypeInScene(type);
        }
    }

    private void DrawConnection()
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        
        var connectionStyle = new GUIStyle(EditorStyles.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 16,
            normal = { textColor = Color.gray }
        };
        
        EditorGUILayout.LabelField("↓", connectionStyle, GUILayout.Width(20));
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
    }

    private void DrawLegend()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Legend", EditorStyles.boldLabel);
        
        foreach (var type in discoveredEntityTypes.Take(10)) // Limit to first 10 to avoid clutter
        {
            EditorGUILayout.BeginHorizontal();
            
            // Color box
            var colorRect = GUILayoutUtility.GetRect(15, 15);
            EditorGUI.DrawRect(colorRect, GetColorForType(type));
            
            // Type name
            EditorGUILayout.LabelField($"{GetIconForType(type)} {type.Name}", EditorStyles.miniLabel);
            
            EditorGUILayout.EndHorizontal();
        }
        
        if (discoveredEntityTypes.Count > 10)
        {
            EditorGUILayout.LabelField($"... and {discoveredEntityTypes.Count - 10} more types", EditorStyles.miniLabel);
        }
        
        EditorGUILayout.EndVertical();
    }
    
    private void DrawControls()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Controls", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        autoRefresh = EditorGUILayout.Toggle("Auto Refresh", autoRefresh);
        
        if (GUILayout.Button("Manual Refresh", GUILayout.Width(100)))
        {
            RefreshEntityTypes();
        }
        
        if (GUILayout.Button("Open Color Config", GUILayout.Width(120)))
        {
            if (colorConfig != null)
            {
                Selection.activeObject = colorConfig;
                EditorGUIUtility.PingObject(colorConfig);
            }
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.LabelField($"Last refresh: {(EditorApplication.timeSinceStartup - lastRefreshTime):F1}s ago", EditorStyles.miniLabel);
        EditorGUILayout.LabelField($"Discovered types: {discoveredEntityTypes.Count}", EditorStyles.miniLabel);
        
        EditorGUILayout.EndVertical();
    }

    private Color GetColorForType(System.Type type)
    {
        if (colorConfig != null)
        {
            return colorConfig.GetColorForType(type);
        }
        
        // Fallback to default colors
        return new Color(0.4f, 0.4f, 0.4f);
    }

    private string GetIconForType(System.Type type)
    {
        if (colorConfig != null)
        {
            return colorConfig.GetIconForType(type);
        }
        
        // Fallback icons
        if (type == typeof(Entity)) return "🏛️";
        if (type == typeof(Player)) return "🐟";
        if (type == typeof(Enemy)) return "👹";
        if (type == typeof(LandEnemy)) return "🚶";
        if (type == typeof(Fisherman)) return "🎣";
        if (type == typeof(FishingProjectile)) return "🪝";
        if (type == typeof(FishingHook)) return "🎯";
        if (type == typeof(DroppedTool)) return "🔧";
        return "📦";
    }

    private int GetFieldCountForType(System.Type type)
    {
        return type.GetFields(BindingFlags.DeclaredOnly | 
                             BindingFlags.Instance | 
                             BindingFlags.Public | 
                             BindingFlags.NonPublic)
                  .Where(f => f.IsPublic || f.GetCustomAttribute<SerializeField>() != null)
                  .Count();
    }

    private void FindTypeInScene(System.Type type)
    {
        // Use reflection to find objects of the specific type in the scene
        var objects = Resources.FindObjectsOfTypeAll(typeof(MonoBehaviour))
            .Where(obj => type.IsAssignableFrom(obj.GetType()) && obj.hideFlags == HideFlags.None)
            .Cast<MonoBehaviour>()
            .Where(obj => obj.gameObject.scene.isLoaded)
            .ToArray();

        if (objects.Length == 0)
        {
            Debug.Log($"No {type.Name} objects found in current scene.");
            return;
        }

        Debug.Log($"Found {objects.Length} {type.Name} object(s) in scene:");

        // Select first object found
        Selection.activeObject = objects[0];

        foreach (var obj in objects)
        {
            Debug.Log($"  - {obj.name} at {obj.transform.position}", obj);
        }
    }

    private Texture2D CreateColorTexture(Color color)
    {
        if (!textureCache.ContainsKey(color))
        {
            var texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            textureCache[color] = texture;
        }
        return textureCache[color];
    }

    private void OnDestroy()
    {
        // Clean up cached textures when window is destroyed
        foreach (var texture in textureCache.Values)
        {
            if (texture != null)
                DestroyImmediate(texture);
        }
        textureCache.Clear();
    }
}