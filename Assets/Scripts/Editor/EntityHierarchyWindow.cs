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
    private static readonly Dictionary<System.Type, Color> TypeColors = new Dictionary<System.Type, Color>
    {
        { typeof(Entity), new Color(0.2f, 0.4f, 0.7f) },
        { typeof(Player), new Color(0.2f, 0.6f, 0.3f) },
        { typeof(Enemy), new Color(0.8f, 0.4f, 0.2f) },
        { typeof(LandEnemy), new Color(0.7f, 0.3f, 0.5f) },
        { typeof(Fisherman), new Color(0.5f, 0.3f, 0.7f) },
        { typeof(FishingProjectile), new Color(0.3f, 0.7f, 0.8f) },
        { typeof(FishingHook), new Color(0.2f, 0.5f, 0.6f) },
        { typeof(DroppedTool), new Color(0.6f, 0.4f, 0.2f) }
    };

    [MenuItem("Tools/Entity Hierarchy Visualizer")]
    public static void ShowWindow()
    {
        var window = GetWindow<EntityHierarchyWindow>("Entity Hierarchy");
        window.minSize = new Vector2(400, 300);
        window.Show();
    }

    private void OnGUI()
    {
        DrawHeader();
        EditorGUILayout.Space(10);
        
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        DrawHierarchyTree();
        
        EditorGUILayout.EndScrollView();
        
        EditorGUILayout.Space(10);
        DrawLegend();
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
        var hierarchyTypes = new List<System.Type>
        {
            typeof(Entity),
            typeof(Player),
            typeof(Enemy),
            typeof(LandEnemy),
            typeof(Fisherman),
            typeof(FishingProjectile),
            typeof(FishingHook),
            typeof(DroppedTool)
        };

        EditorGUILayout.BeginVertical();
        
        // Draw Entity (root)
        DrawTypeBox(typeof(Entity), 0);
        
        // Draw connections and child types
        EditorGUILayout.Space(20);
        EditorGUILayout.BeginHorizontal();
        
        // Player branch
        EditorGUILayout.BeginVertical(GUILayout.Width(140));
        DrawConnection();
        DrawTypeBox(typeof(Player), 1);
        EditorGUILayout.EndVertical();
        
        GUILayout.FlexibleSpace();
        
        // Enemy branch
        EditorGUILayout.BeginVertical(GUILayout.Width(140));
        DrawConnection();
        DrawTypeBox(typeof(Enemy), 1);
        
        EditorGUILayout.Space(20);
        DrawConnection();
        DrawTypeBox(typeof(LandEnemy), 2);
        
        EditorGUILayout.Space(20);
        DrawConnection();
        DrawTypeBox(typeof(Fisherman), 3);
        
        EditorGUILayout.EndVertical();
        
        GUILayout.FlexibleSpace();
        
        // FishingProjectile branch
        EditorGUILayout.BeginVertical(GUILayout.Width(140));
        DrawConnection();
        DrawTypeBox(typeof(FishingProjectile), 1);
        
        EditorGUILayout.Space(20);
        DrawConnection();
        DrawTypeBox(typeof(FishingHook), 2);
        
        EditorGUILayout.EndVertical();
        
        GUILayout.FlexibleSpace();
        
        // DroppedTool branch
        EditorGUILayout.BeginVertical(GUILayout.Width(140));
        DrawConnection();
        DrawTypeBox(typeof(DroppedTool), 1);
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }

    private void DrawTypeBox(System.Type type, int level)
    {
        Color typeColor = TypeColors.ContainsKey(type) ? TypeColors[type] : Color.white;
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
        
        foreach (var kvp in TypeColors)
        {
            EditorGUILayout.BeginHorizontal();
            
            // Color box
            var colorRect = GUILayoutUtility.GetRect(15, 15);
            EditorGUI.DrawRect(colorRect, kvp.Value);
            
            // Type name
            EditorGUILayout.LabelField($"{GetIconForType(kvp.Key)} {kvp.Key.Name}", EditorStyles.miniLabel);
            
            EditorGUILayout.EndHorizontal();
        }
        
        EditorGUILayout.EndVertical();
    }

    private string GetIconForType(System.Type type)
    {
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