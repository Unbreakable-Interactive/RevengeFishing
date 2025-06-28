using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

[CustomEditor(typeof(Entity), true)]
public class EntityHierarchyInspector : Editor
{
    private static readonly Color EntityColor = new Color(0.2f, 0.4f, 0.7f);       // Deep blue
    private static readonly Color PlayerColor = new Color(0.2f, 0.6f, 0.3f);     // Forest green  
    private static readonly Color EnemyColor = new Color(0.8f, 0.4f, 0.2f);      // Warm orange
    private static readonly Color LandEnemyColor = new Color(0.7f, 0.3f, 0.5f);  // Deep rose
    private static readonly Color FishermanColor = new Color(0.5f, 0.3f, 0.7f);  // Rich purple
    private static readonly Color FishingProjectileColor = new Color(0.1f, 0.5f, 0.7f); // Cyan
    private static readonly Color FishingHookColor = new Color(0.2f, 0.5f, 0.6f); // Teal
    private static readonly Color DroppedToolColor = new Color(0.6f, 0.4f, 0.2f); // Brown
    
    private Dictionary<System.Type, bool> foldoutStates = new Dictionary<System.Type, bool>();
    private Dictionary<System.Type, List<FieldInfo>> hierarchyFields = new Dictionary<System.Type, List<FieldInfo>>();
    private bool isInitialized = false;

    public override void OnInspectorGUI()
    {
        if (!isInitialized)
        {
            InitializeHierarchy();
            isInitialized = true;
        }

        serializedObject.Update();

        EditorGUILayout.Space(5);
        DrawHierarchyTitle();
        EditorGUILayout.Space(10);

        // Draw each inheritance level
        foreach (var kvp in hierarchyFields.OrderBy(x => GetHierarchyOrder(x.Key)))
        {
            DrawInheritanceLevel(kvp.Key, kvp.Value);
            EditorGUILayout.Space(5);
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void InitializeHierarchy()
    {
        hierarchyFields.Clear();
        foldoutStates.Clear();

        System.Type targetType = target.GetType();
        System.Type currentType = targetType;

        // Build inheritance chain from child to parent
        List<System.Type> inheritanceChain = new List<System.Type>();
        while (currentType != null && currentType != typeof(MonoBehaviour) && currentType != typeof(Component))
        {
            inheritanceChain.Add(currentType);
            currentType = currentType.BaseType;
        }

        // Process each type in the chain
        foreach (System.Type type in inheritanceChain)
        {
            var fields = GetDeclaredFieldsForType(type);
            if (fields.Count > 0)
            {
                hierarchyFields[type] = fields;
                foldoutStates[type] = GetDefaultFoldoutState(type);
            }
        }
    }

    private List<FieldInfo> GetDeclaredFieldsForType(System.Type type)
    {
        var fields = new List<FieldInfo>();
        
        // Get all serialized fields declared specifically in this type (not inherited)
        var declaredFields = type.GetFields(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(f => f.IsPublic || f.GetCustomAttribute<SerializeField>() != null)
            .Where(f => !f.GetCustomAttributes<System.ObsoleteAttribute>().Any())
            .OrderBy(f => GetFieldOrder(f))
            .ToList();

        fields.AddRange(declaredFields);
        return fields;
    }

    private void DrawHierarchyTitle()
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        
        var titleStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 14,
            alignment = TextAnchor.MiddleCenter
        };
        
        EditorGUILayout.LabelField($"🏗️ Entity Hierarchy Inspector", titleStyle);
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        EditorGUILayout.LabelField($"Current Type: {target.GetType().Name}", EditorStyles.miniLabel);
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
    }

    private void DrawInheritanceLevel(System.Type type, List<FieldInfo> fields)
    {
        Color levelColor = GetColorForType(type);
        string levelIcon = GetIconForType(type);
        
        // Create colored background
        var backgroundRect = EditorGUILayout.BeginVertical();
        EditorGUI.DrawRect(backgroundRect, levelColor);
        
        EditorGUILayout.Space(3);
        
        // Header with foldout
        EditorGUILayout.BeginHorizontal();
        
        string headerText = $"{levelIcon} {type.Name}";
        if (type.BaseType != null && type.BaseType != typeof(MonoBehaviour) && type.BaseType != typeof(Component))
        {
            headerText += $" : {type.BaseType.Name}";
        }
        
        var headerStyle = new GUIStyle(EditorStyles.foldout)
        {
            fontStyle = FontStyle.Bold,
            fontSize = 12,
            normal = { textColor = Color.white },
            onNormal = { textColor = Color.white },
            focused = { textColor = Color.white },
            onFocused = { textColor = Color.white }
        };
        
        foldoutStates[type] = EditorGUILayout.Foldout(foldoutStates[type], headerText, headerStyle);
        
        // Field count badge
        var badgeStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            normal = { 
                background = CreateSolidTexture(Color.black * 0.3f),
                textColor = Color.white
            },
            padding = new RectOffset(6, 6, 2, 2),
            margin = new RectOffset(0, 5, 2, 2),
            fontStyle = FontStyle.Bold
        };
        EditorGUILayout.LabelField($"{fields.Count}", badgeStyle, GUILayout.Width(25));
        
        EditorGUILayout.EndHorizontal();
        
        // Fields content
        if (foldoutStates[type])
        {
            EditorGUILayout.Space(2);
            
            if (fields.Count == 0)
            {
                var noFieldsStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    normal = { textColor = Color.white * 0.8f },
                    fontStyle = FontStyle.Italic
                };
                EditorGUILayout.LabelField("   No serialized fields", noFieldsStyle);
            }
            else
            {
                EditorGUI.indentLevel++;
                foreach (var field in fields)
                {
                    DrawField(field);
                }
                EditorGUI.indentLevel--;
            }
        }
        
        EditorGUILayout.Space(3);
        EditorGUILayout.EndVertical();
    }

    private void DrawField(FieldInfo field)
    {
        var property = serializedObject.FindProperty(field.Name);
        
        if (property != null)
        {
            // Custom field display with type info
            EditorGUILayout.BeginHorizontal();
            
            // Property field
            EditorGUILayout.PropertyField(property, true);
            
            // Type info
            var typeStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = Color.white * 0.7f },
                fontStyle = FontStyle.Italic
            };
            
            string typeInfo = GetFieldTypeDisplay(field);
            EditorGUILayout.LabelField(typeInfo, typeStyle, GUILayout.Width(80));
            
            EditorGUILayout.EndHorizontal();
        }
        else
        {
            // Fallback for fields not found in serialized object
            EditorGUILayout.BeginHorizontal();
            
            var lockedStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = Color.white * 0.6f },
                fontStyle = FontStyle.Italic
            };
            
            EditorGUILayout.LabelField($"🔒 {field.Name}", lockedStyle);
            
            var lockedTypeStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = Color.white * 0.5f },
                fontStyle = FontStyle.Italic
            };
            
            EditorGUILayout.LabelField(GetFieldTypeDisplay(field), lockedTypeStyle, GUILayout.Width(80));
            EditorGUILayout.EndHorizontal();
        }
    }

    private Color GetColorForType(System.Type type)
    {
        if (type == typeof(Entity)) return EntityColor;
        if (type == typeof(Player)) return PlayerColor;
        if (type == typeof(Enemy)) return EnemyColor;
        if (type == typeof(LandEnemy)) return LandEnemyColor;
        if (type == typeof(Fisherman)) return FishermanColor;
        if (type == typeof(FishingProjectile)) return FishingProjectileColor;
        if (type == typeof(FishingHook)) return FishingHookColor;
        if (type == typeof(DroppedTool)) return DroppedToolColor;
        
        // Default color for other types
        return new Color(0.4f, 0.4f, 0.4f);
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

    private int GetHierarchyOrder(System.Type type)
    {
        if (type == typeof(Entity)) return 0;
        if (type == typeof(Player)) return 1;
        if (type == typeof(Enemy)) return 1;
        if (type == typeof(FishingProjectile)) return 1;
        if (type == typeof(DroppedTool)) return 1;
        if (type == typeof(LandEnemy)) return 2;
        if (type == typeof(FishingHook)) return 2;
        if (type == typeof(Fisherman)) return 3;
        
        // Calculate depth for unknown types
        int depth = 0;
        System.Type current = type;
        while (current != null && current != typeof(Entity) && current != typeof(MonoBehaviour))
        {
            depth++;
            current = current.BaseType;
        }
        return depth;
    }

    private bool GetDefaultFoldoutState(System.Type type)
    {
        // Entity is always expanded by default, others collapsed
        return type == typeof(Entity) || type == target.GetType();
    }

    private int GetFieldOrder(FieldInfo field)
    {
        // Headers come first
        if (field.GetCustomAttribute<HeaderAttribute>() != null) return 0;
        
        // Then public fields
        if (field.IsPublic) return 1;
        
        // Then private SerializeField
        return 2;
    }

    private string GetFieldTypeDisplay(FieldInfo field)
    {
        System.Type fieldType = field.FieldType;
        
        if (fieldType == typeof(int)) return "int";
        if (fieldType == typeof(float)) return "float";
        if (fieldType == typeof(bool)) return "bool";
        if (fieldType == typeof(string)) return "string";
        if (fieldType == typeof(Vector2)) return "Vector2";
        if (fieldType == typeof(Vector3)) return "Vector3";
        if (fieldType.IsEnum) return "enum";
        
        // For complex types, show just the name
        if (fieldType.Name.Length > 10)
            return fieldType.Name.Substring(0, 10) + "...";
        
        return fieldType.Name;
    }

    private Texture2D CreateSolidTexture(Color color)
    {
        var texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }
}