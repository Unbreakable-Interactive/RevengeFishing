// using UnityEngine;
// using UnityEditor;
// using System.Collections.Generic;
// using System.Linq;
// using System.Reflection;
// using System;
//
// public class ClassRelationshipVisualizer : EditorWindow
// {
//     private Vector2 scrollPosition;
//     private Dictionary<Type, Rect> nodePositions = new Dictionary<Type, Rect>();
//     private Dictionary<Type, Color> nodeColors = new Dictionary<Type, Color>();
//     private List<ClassNode> nodes = new List<ClassNode>();
//     private List<ClassRelationship> relationships = new List<ClassRelationship>();
//     private bool autoLayout = true;
//     private float nodeWidth = 120f;
//     private float nodeHeight = 80f;
//     private float gridSpacing = 150f;
//
//     [System.Serializable]
//     public class ClassNode
//     {
//         public Type type;
//         public Vector2 position;
//         public List<FieldInfo> fields;
//         public List<PropertyInfo> properties;
//         public List<MethodInfo> methods;
//         public bool isAbstract;
//         public bool isInterface;
//         
//         public ClassNode(Type type)
//         {
//             this.type = type;
//             this.isAbstract = type.IsAbstract && !type.IsInterface;
//             this.isInterface = type.IsInterface;
//             this.fields = GetRelevantFields(type);
//             this.properties = GetRelevantProperties(type);
//             this.methods = GetRelevantMethods(type);
//         }
//
//         private List<FieldInfo> GetRelevantFields(Type type)
//         {
//             if (type.IsEnum) return new List<FieldInfo>(); // Enums don't have relevant fields to show
//             
//             return type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)
//                 .Where(f => f.IsPublic || f.GetCustomAttribute<SerializeField>() != null)
//                 .Where(f => !f.Name.Contains("k__BackingField")) // Skip auto-property backing fields
//                 .Take(3) // Limit for display
//                 .ToList();
//         }
//
//         private List<PropertyInfo> GetRelevantProperties(Type type)
//         {
//             if (type.IsEnum) return new List<PropertyInfo>(); // Enums don't have properties
//             
//             return type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
//                 .Where(p => p.CanRead) // Only readable properties
//                 .Take(2) // Limit for display
//                 .ToList();
//         }
//
//         private List<MethodInfo> GetRelevantMethods(Type type)
//         {
//             if (type.IsEnum) return new List<MethodInfo>(); // Enums don't have custom methods
//             
//             return type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
//                 .Where(m => !m.IsSpecialName && !m.Name.StartsWith("get_") && !m.Name.StartsWith("set_"))
//                 .Where(m => !m.Name.StartsWith("add_") && !m.Name.StartsWith("remove_")) // Skip event methods
//                 .Take(2) // Limit for display
//                 .ToList();
//         }
//     }
//
//     [System.Serializable]
//     public class ClassRelationship
//     {
//         public Type from;
//         public Type to;
//         public RelationshipType relationshipType;
//         public string fieldName;
//
//         public enum RelationshipType
//         {
//             Inheritance,    // is-a
//             Composition,    // has-a (strong)
//             Aggregation,    // has-a (weak)
//             Dependency,     // uses
//             Interface       // implements
//         }
//     }
//
//     [MenuItem("Tools/Class Relationship Visualizer")]
//     public static void ShowWindow()
//     {
//         var window = GetWindow<ClassRelationshipVisualizer>("Class Relations");
//         window.minSize = new Vector2(800, 600);
//         window.AnalyzeProject();
//         window.Show();
//     }
//
//     private void OnGUI()
//     {
//         DrawToolbar();
//         
//         // Calculate content size for scrolling
//         Rect contentRect = new Rect(0, 25, 2000, 1500);
//         
//         scrollPosition = GUI.BeginScrollView(new Rect(0, 25, position.width, position.height - 25), 
//             scrollPosition, contentRect);
//
//         DrawBackground(contentRect);
//         DrawRelationships();
//         DrawNodes();
//         
//         GUI.EndScrollView();
//         
//         HandleInput();
//     }
//
//     private void DrawToolbar()
//     {
//         EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
//         
//         if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60)))
//         {
//             AnalyzeProject();
//         }
//         
//         if (GUILayout.Button("Auto Layout", EditorStyles.toolbarButton, GUILayout.Width(80)))
//         {
//             AutoLayoutNodes();
//         }
//         
//         GUILayout.FlexibleSpace();
//         
//         EditorGUILayout.LabelField("Class Relationship Diagram", EditorStyles.boldLabel);
//         
//         GUILayout.FlexibleSpace();
//         EditorGUILayout.EndHorizontal();
//     }
//
//     private void DrawBackground(Rect contentRect)
//     {
//         // Draw grid
//         Handles.BeginGUI();
//         Handles.color = new Color(0.5f, 0.5f, 0.5f, 0.1f);
//         
//         for (float x = 0; x < contentRect.width; x += 50)
//         {
//             Handles.DrawLine(new Vector3(x, 0), new Vector3(x, contentRect.height));
//         }
//         
//         for (float y = 0; y < contentRect.height; y += 50)
//         {
//             Handles.DrawLine(new Vector3(0, y), new Vector3(contentRect.width, y));
//         }
//         
//         Handles.EndGUI();
//     }
//
//     private void DrawNodes()
//     {
//         foreach (var node in nodes)
//         {
//             DrawNode(node);
//         }
//     }
//
//     private void DrawNode(ClassNode node)
//     {
//         Rect nodeRect = new Rect(node.position.x, node.position.y, nodeWidth, nodeHeight);
//         
//         // Determine node color
//         Color nodeColor = GetNodeColor(node);
//         
//         // Draw node background
//         EditorGUI.DrawRect(nodeRect, nodeColor);
//         
//         // Draw border
//         Handles.BeginGUI();
//         Handles.color = Color.black;
//         Handles.DrawLine(new Vector3(nodeRect.x, nodeRect.y), new Vector3(nodeRect.x + nodeRect.width, nodeRect.y));
//         Handles.DrawLine(new Vector3(nodeRect.x + nodeRect.width, nodeRect.y), new Vector3(nodeRect.x + nodeRect.width, nodeRect.y + nodeRect.height));
//         Handles.DrawLine(new Vector3(nodeRect.x + nodeRect.width, nodeRect.y + nodeRect.height), new Vector3(nodeRect.x, nodeRect.y + nodeRect.height));
//         Handles.DrawLine(new Vector3(nodeRect.x, nodeRect.y + nodeRect.height), new Vector3(nodeRect.x, nodeRect.y));
//         Handles.EndGUI();
//         
//         // Draw content
//         GUILayout.BeginArea(nodeRect);
//         
//         var titleStyle = new GUIStyle(EditorStyles.boldLabel)
//         {
//             fontSize = 10,
//             alignment = TextAnchor.UpperCenter,
//             normal = { textColor = Color.white }
//         };
//         
//         var contentStyle = new GUIStyle(EditorStyles.miniLabel)
//         {
//             fontSize = 8,
//             normal = { textColor = Color.white }
//         };
//         
//         // Class name
//         GUILayout.Label(GetDisplayName(node), titleStyle);
//         
//         // Type indicators
//         if (node.isInterface)
//             GUILayout.Label("<<interface>>", contentStyle);
//         else if (node.isAbstract)
//             GUILayout.Label("<<abstract>>", contentStyle);
//         else if (node.type.IsEnum)
//             GUILayout.Label("<<enum>>", contentStyle);
//         else if (node.type.IsAbstract && node.type.IsSealed)
//             GUILayout.Label("<<static>>", contentStyle);
//         else if (typeof(ScriptableObject).IsAssignableFrom(node.type))
//             GUILayout.Label("<<ScriptableObject>>", contentStyle);
//         else if (typeof(MonoBehaviour).IsAssignableFrom(node.type))
//             GUILayout.Label("<<MonoBehaviour>>", contentStyle);
//         
//         // Show enum values for enums
//         if (node.type.IsEnum)
//         {
//             var enumValues = Enum.GetNames(node.type);
//             GUILayout.Label("Values:", contentStyle);
//             foreach (var enumValue in enumValues.Take(2))
//             {
//                 GUILayout.Label($"• {enumValue}", contentStyle);
//             }
//             if (enumValues.Length > 2)
//                 GUILayout.Label($"... +{enumValues.Length - 2} more", contentStyle);
//         }
//         else
//         {
//             // Show some fields
//             if (node.fields.Count > 0)
//             {
//                 GUILayout.Label("Fields:", contentStyle);
//                 foreach (var field in node.fields)
//                 {
//                     GUILayout.Label($"• {field.Name}", contentStyle);
//                 }
//             }
//             
//             // Show some methods
//             if (node.methods.Count > 0)
//             {
//                 GUILayout.Label("Methods:", contentStyle);
//                 foreach (var method in node.methods)
//                 {
//                     GUILayout.Label($"• {method.Name}()", contentStyle);
//                 }
//             }
//         }
//         
//         GUILayout.EndArea();
//         
//         nodePositions[node.type] = nodeRect;
//     }
//
//     private void DrawRelationships()
//     {
//         Handles.BeginGUI();
//         
//         foreach (var relationship in relationships)
//         {
//             DrawRelationship(relationship);
//         }
//         
//         Handles.EndGUI();
//     }
//
//     private void DrawRelationship(ClassRelationship relationship)
//     {
//         if (!nodePositions.ContainsKey(relationship.from) || !nodePositions.ContainsKey(relationship.to))
//             return;
//
//         Rect fromRect = nodePositions[relationship.from];
//         Rect toRect = nodePositions[relationship.to];
//         
//         Vector3 fromPos = new Vector3(fromRect.center.x, fromRect.center.y);
//         Vector3 toPos = new Vector3(toRect.center.x, toRect.center.y);
//         
//         // Set color and style based on relationship type
//         switch (relationship.relationshipType)
//         {
//             case ClassRelationship.RelationshipType.Inheritance:
//                 Handles.color = Color.blue;
//                 DrawArrow(fromPos, toPos, true);
//                 break;
//             case ClassRelationship.RelationshipType.Composition:
//                 Handles.color = Color.red;
//                 DrawArrow(fromPos, toPos, false);
//                 break;
//             case ClassRelationship.RelationshipType.Aggregation:
//                 Handles.color = Color.yellow;
//                 DrawArrow(fromPos, toPos, false);
//                 break;
//             case ClassRelationship.RelationshipType.Dependency:
//                 Handles.color = Color.gray;
//                 DrawDashedLine(fromPos, toPos);
//                 break;
//             case ClassRelationship.RelationshipType.Interface:
//                 Handles.color = Color.green;
//                 DrawDashedArrow(fromPos, toPos);
//                 break;
//         }
//     }
//
//     private void DrawArrow(Vector3 from, Vector3 to, bool hollow = false)
//     {
//         Handles.DrawLine(from, to);
//         
//         Vector3 direction = (to - from).normalized;
//         Vector3 arrowHead1 = to - direction * 10 + Vector3.Cross(direction, Vector3.forward) * 5;
//         Vector3 arrowHead2 = to - direction * 10 - Vector3.Cross(direction, Vector3.forward) * 5;
//         
//         Handles.DrawLine(to, arrowHead1);
//         Handles.DrawLine(to, arrowHead2);
//         
//         if (hollow)
//         {
//             Handles.DrawLine(arrowHead1, arrowHead2);
//         }
//     }
//
//     private void DrawDashedLine(Vector3 from, Vector3 to)
//     {
//         float distance = Vector3.Distance(from, to);
//         Vector3 direction = (to - from).normalized;
//         
//         for (float i = 0; i < distance; i += 10)
//         {
//             Vector3 start = from + direction * i;
//             Vector3 end = from + direction * Mathf.Min(i + 5, distance);
//             Handles.DrawLine(start, end);
//         }
//     }
//
//     private void DrawDashedArrow(Vector3 from, Vector3 to)
//     {
//         DrawDashedLine(from, to);
//         
//         Vector3 direction = (to - from).normalized;
//         Vector3 arrowHead1 = to - direction * 10 + Vector3.Cross(direction, Vector3.forward) * 5;
//         Vector3 arrowHead2 = to - direction * 10 - Vector3.Cross(direction, Vector3.forward) * 5;
//         
//         Handles.DrawLine(to, arrowHead1);
//         Handles.DrawLine(to, arrowHead2);
//         Handles.DrawLine(arrowHead1, arrowHead2);
//     }
//
//     private Color GetNodeColor(ClassNode node)
//     {
//         // Entity hierarchy colors (same as other tools)
//         if (node.type == typeof(Entity)) return new Color(0.2f, 0.4f, 0.7f);
//         if (node.type == typeof(Player)) return new Color(0.2f, 0.6f, 0.3f);
//         if (node.type == typeof(Enemy)) return new Color(0.8f, 0.4f, 0.2f);
//         if (node.type == typeof(LandEnemy)) return new Color(0.7f, 0.3f, 0.5f);
//         if (node.type == typeof(Fisherman)) return new Color(0.5f, 0.3f, 0.7f);
//         if (node.type == typeof(FishingProjectile)) return new Color(0.3f, 0.7f, 0.8f);
//         if (node.type == typeof(FishingHook)) return new Color(0.2f, 0.5f, 0.6f);
//         if (node.type == typeof(DroppedTool)) return new Color(0.6f, 0.4f, 0.2f);
//         
//         // ScriptableObject types
//         if (typeof(ScriptableObject).IsAssignableFrom(node.type))
//             return new Color(0.9f, 0.7f, 0.3f); // Golden yellow
//         
//         // MonoBehaviour types (components)
//         if (typeof(MonoBehaviour).IsAssignableFrom(node.type))
//             return new Color(0.4f, 0.7f, 0.4f); // Light green
//         
//         // Interfaces
//         if (node.isInterface) 
//             return new Color(0.9f, 0.9f, 0.5f); // Light yellow
//         
//         // Abstract classes
//         if (node.isAbstract) 
//             return new Color(0.7f, 0.7f, 0.7f); // Light gray
//         
//         // Enums
//         if (node.type.IsEnum)
//             return new Color(0.8f, 0.5f, 0.8f); // Light purple
//         
//         // Static classes
//         if (node.type.IsAbstract && node.type.IsSealed)
//             return new Color(0.5f, 0.8f, 0.8f); // Light cyan
//         
//         // Regular classes
//         return new Color(0.6f, 0.6f, 0.8f); // Light blue-gray
//     }
//
//     private string GetDisplayName(ClassNode node)
//     {
//         string name = node.type.Name;
//         if (name.Length > 12)
//             return name.Substring(0, 12) + "...";
//         return name;
//     }
//
//     private void AnalyzeProject()
//     {
//         nodes.Clear();
//         relationships.Clear();
//         nodePositions.Clear();
//         
//         // Find ALL types in the Scripts folder
//         var allTypes = FindAllProjectTypes();
//         
//         // Create nodes
//         foreach (var type in allTypes)
//         {
//             nodes.Add(new ClassNode(type));
//         }
//         
//         // Analyze relationships
//         foreach (var type in allTypes)
//         {
//             AnalyzeTypeRelationships(type, allTypes);
//         }
//         
//         AutoLayoutNodes();
//     }
//
//     private List<Type> FindAllProjectTypes()
//     {
//         var projectTypes = new List<Type>();
//         
//         try
//         {
//             // ONLY get the main Assembly-CSharp (user scripts)
//             var mainAssembly = System.AppDomain.CurrentDomain.GetAssemblies()
//                 .FirstOrDefault(a => a.GetName().Name == "Assembly-CSharp");
//             
//             if (mainAssembly == null) 
//             {
//                 Debug.LogWarning("Assembly-CSharp not found. Project may not be compiled yet.");
//                 return projectTypes;
//             }
//             
//             // Get types ONLY from user scripts assembly
//             var types = mainAssembly.GetTypes()
//                 .Where(t => IsProjectType(t))
//                 .Take(50) // SAFETY LIMIT - max 50 types
//                 .ToList();
//             
//             projectTypes.AddRange(types);
//             
//             Debug.Log($"Found {projectTypes.Count} project types to analyze");
//         }
//         catch (Exception ex)
//         {
//             Debug.LogError($"Error finding project types: {ex.Message}");
//         }
//         
//         return projectTypes;
//     }
//
//     private bool IsProjectType(Type type)
//     {
//         if (type == null) return false;
//         
//         // Skip compiler-generated types
//         if (type.Name.Contains("<") || type.Name.Contains("$") || type.Name.Contains("+")) return false;
//         
//         // Skip generic type definitions
//         if (type.IsGenericTypeDefinition) return false;
//         
//         // Skip Unity's built-in types
//         string namespaceName = type.Namespace ?? "";
//         if (namespaceName.StartsWith("UnityEngine") || 
//             namespaceName.StartsWith("UnityEditor") ||
//             namespaceName.StartsWith("Unity."))
//             return false;
//         
//         // Only include our project types
//         return true;
//     }
//
//     private void AnalyzeTypeRelationships(Type type, List<Type> allTypes)
//     {
//         try
//         {
//             // SAFETY: Limit relationship analysis
//             int maxRelationships = 10;
//             int relationshipCount = 0;
//             
//             // Inheritance relationships
//             if (type.BaseType != null && allTypes.Contains(type.BaseType) && relationshipCount < maxRelationships)
//             {
//                 relationships.Add(new ClassRelationship
//                 {
//                     from = type,
//                     to = type.BaseType,
//                     relationshipType = ClassRelationship.RelationshipType.Inheritance
//                 });
//                 relationshipCount++;
//             }
//             
//             // Interface implementations (limited)
//             foreach (var interfaceType in type.GetInterfaces().Take(3))
//             {
//                 if (allTypes.Contains(interfaceType) && relationshipCount < maxRelationships)
//                 {
//                     relationships.Add(new ClassRelationship
//                     {
//                         from = type,
//                         to = interfaceType,
//                         relationshipType = ClassRelationship.RelationshipType.Interface
//                     });
//                     relationshipCount++;
//                 }
//             }
//             
//             // Field relationships (LIMITED and SAFE)
//             var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
//                 .Where(f => f.IsPublic || f.GetCustomAttribute<SerializeField>() != null)
//                 .Take(5); // Only analyze first 5 fields
//             
//             foreach (var field in fields)
//             {
//                 if (relationshipCount >= maxRelationships) break;
//                 
//                 Type fieldType = field.FieldType;
//                 
//                 // Handle arrays and generic collections safely
//                 if (fieldType.IsArray)
//                     fieldType = fieldType.GetElementType();
//                 else if (fieldType.IsGenericType && fieldType.GetGenericArguments().Length > 0)
//                     fieldType = fieldType.GetGenericArguments().FirstOrDefault();
//                 
//                 if (fieldType != null && allTypes.Contains(fieldType) && fieldType != type)
//                 {
//                     relationships.Add(new ClassRelationship
//                     {
//                         from = type,
//                         to = fieldType,
//                         relationshipType = ClassRelationship.RelationshipType.Composition,
//                         fieldName = field.Name
//                     });
//                     relationshipCount++;
//                 }
//             }
//         }
//         catch (Exception ex)
//         {
//             Debug.LogWarning($"Error analyzing relationships for {type.Name}: {ex.Message}");
//         }
//     }
//
//     private void AutoLayoutNodes()
//     {
//         if (nodes.Count == 0) return;
//         
//         // Separate nodes by type for better organization
//         var entityNodes = nodes.Where(n => typeof(Entity).IsAssignableFrom(n.type)).ToList();
//         var scriptableObjectNodes = nodes.Where(n => typeof(ScriptableObject).IsAssignableFrom(n.type) && !typeof(Entity).IsAssignableFrom(n.type)).ToList();
//         var monoBehaviourNodes = nodes.Where(n => typeof(MonoBehaviour).IsAssignableFrom(n.type) && !typeof(Entity).IsAssignableFrom(n.type)).ToList();
//         var interfaceNodes = nodes.Where(n => n.isInterface).ToList();
//         var enumNodes = nodes.Where(n => n.type.IsEnum).ToList();
//         var staticClassNodes = nodes.Where(n => n.type.IsAbstract && n.type.IsSealed).ToList();
//         var regularClassNodes = nodes.Where(n => !typeof(MonoBehaviour).IsAssignableFrom(n.type) && 
//                                                   !typeof(ScriptableObject).IsAssignableFrom(n.type) && 
//                                                   !n.isInterface && !n.type.IsEnum && 
//                                                   !(n.type.IsAbstract && n.type.IsSealed)).ToList();
//         
//         float currentY = 50f;
//         float sectionSpacing = gridSpacing * 1.5f;
//         
//         // Layout Entity hierarchy
//         if (entityNodes.Count > 0)
//         {
//             LayoutHierarchicalSection(entityNodes, 50f, currentY, "Entity Hierarchy");
//             currentY += CalculateSectionHeight(entityNodes) + sectionSpacing;
//         }
//         
//         // Layout ScriptableObjects
//         if (scriptableObjectNodes.Count > 0)
//         {
//             LayoutGridSection(scriptableObjectNodes, 50f, currentY, "ScriptableObjects");
//             currentY += CalculateSectionHeight(scriptableObjectNodes) + sectionSpacing;
//         }
//         
//         // Layout MonoBehaviours
//         if (monoBehaviourNodes.Count > 0)
//         {
//             LayoutGridSection(monoBehaviourNodes, 50f, currentY, "MonoBehaviours");
//             currentY += CalculateSectionHeight(monoBehaviourNodes) + sectionSpacing;
//         }
//         
//         // Layout regular classes
//         if (regularClassNodes.Count > 0)
//         {
//             LayoutGridSection(regularClassNodes, 50f, currentY, "Classes");
//             currentY += CalculateSectionHeight(regularClassNodes) + sectionSpacing;
//         }
//         
//         // Layout interfaces
//         if (interfaceNodes.Count > 0)
//         {
//             LayoutGridSection(interfaceNodes, 50f, currentY, "Interfaces");
//             currentY += CalculateSectionHeight(interfaceNodes) + sectionSpacing;
//         }
//         
//         // Layout enums
//         if (enumNodes.Count > 0)
//         {
//             LayoutGridSection(enumNodes, 50f, currentY, "Enums");
//             currentY += CalculateSectionHeight(enumNodes) + sectionSpacing;
//         }
//         
//         // Layout static classes
//         if (staticClassNodes.Count > 0)
//         {
//             LayoutGridSection(staticClassNodes, 50f, currentY, "Static Classes");
//         }
//         
//         Repaint();
//     }
//
//     private void LayoutHierarchicalSection(List<ClassNode> nodes, float startX, float startY, string sectionName)
//     {
//         // For Entity hierarchy, use tree layout
//         var rootNodes = nodes.Where(n => n.type.BaseType == null || !nodes.Any(other => other.type == n.type.BaseType)).ToList();
//         
//         float currentX = startX;
//         
//         foreach (var rootNode in rootNodes)
//         {
//             currentX += LayoutNodeTree(rootNode, nodes, currentX, startY, 0) + gridSpacing;
//         }
//     }
//     
//     private float LayoutNodeTree(ClassNode node, List<ClassNode> allNodes, float x, float y, int depth)
//     {
//         node.position = new Vector2(x, y + depth * gridSpacing);
//         
//         // Find children
//         var children = allNodes.Where(n => n.type.BaseType == node.type).ToList();
//         
//         if (children.Count == 0)
//             return nodeWidth;
//         
//         float totalWidth = 0;
//         float childX = x;
//         
//         foreach (var child in children)
//         {
//             float childWidth = LayoutNodeTree(child, allNodes, childX, y, depth + 1);
//             childX += childWidth + 20f; // Small spacing between siblings
//             totalWidth += childWidth + 20f;
//         }
//         
//         // Center parent over children
//         node.position = new Vector2(x + (totalWidth - 20f) / 2f - nodeWidth / 2f, y + depth * gridSpacing);
//         
//         return Math.Max(totalWidth - 20f, nodeWidth);
//     }
//
//     private void LayoutGridSection(List<ClassNode> nodes, float startX, float startY, string sectionName)
//     {
//         int columns = Math.Max(1, (int)Math.Sqrt(nodes.Count));
//         
//         for (int i = 0; i < nodes.Count; i++)
//         {
//             int row = i / columns;
//             int col = i % columns;
//             
//             nodes[i].position = new Vector2(
//                 startX + col * gridSpacing,
//                 startY + row * gridSpacing
//             );
//         }
//     }
//     
//     private float CalculateSectionHeight(List<ClassNode> nodes)
//     {
//         if (nodes.Count == 0) return 0f;
//         
//         int columns = Math.Max(1, (int)Math.Sqrt(nodes.Count));
//         int rows = (int)Math.Ceiling((float)nodes.Count / columns);
//         
//         return rows * gridSpacing;
//     }
//
//     private void HandleInput()
//     {
//         Event e = Event.current;
//         
//         if (e.type == EventType.MouseDown && e.button == 0)
//         {
//             // Handle node dragging (future feature)
//         }
//     }
// }