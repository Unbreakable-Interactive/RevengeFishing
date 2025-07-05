using System.Collections.Generic;
using UnityEngine;

public class GameObjectCulling : PerformanceComponentBase
{
    public override string ComponentName => "GameObject Culling";
    
    [Header("Culling Settings")]
    public Camera cullingCamera;
    public float cullingDistance = 50f;
    public float updateInterval = 0.1f; // Check every 100ms instead of every frame
    
    [Header("Object Management")]
    public Transform[] staticObjectParents; // Drag your Plane objects here
    public LayerMask cullingLayerMask = -1;
    
    [Header("Performance")]
    public int maxObjectsPerFrame = 20; // Limit objects processed per frame
    public bool enableDistanceCulling = true;
    public bool enableFrustumCulling = true;
    
    [Header("Debug")]
    public KeyCode toggleDebugKey = KeyCode.F2;
    
    private List<CullableObject> allCullableObjects = new List<CullableObject>();
    private int currentObjectIndex = 0;
    private float nextUpdateTime = 0f;
    
    private int culledCount = 0;
    private int totalObjects = 0;

    [System.Serializable]
    public class CullableObject
    {
        public GameObject gameObject;
        public Renderer[] renderers;
        public Collider2D[] colliders;
        public bool wasVisible;
        public Vector3 position;
        
        public CullableObject(GameObject go)
        {
            gameObject = go;
            renderers = go.GetComponentsInChildren<Renderer>();
            colliders = go.GetComponentsInChildren<Collider2D>();
            wasVisible = true;
            position = go.transform.position;
        }
        
        public void SetVisible(bool visible)
        {
            if (wasVisible == visible) return; // No change needed
            
            wasVisible = visible;
            gameObject.SetActive(visible);
        }
    }

    // IPerformanceComponent implementation
    public override void Initialize()
    {
        if (cullingCamera == null)
            cullingCamera = Camera.main;
            
        InitializeCullableObjects();
        Debug.Log($"{ComponentName}: Initialized with {totalObjects} objects");
    }

    void Start()
    {
        // Handled by base class
    }

    void InitializeCullableObjects()
    {
        allCullableObjects.Clear();
        
        foreach (Transform parent in staticObjectParents)
        {
            if (parent == null) continue;
            
            // Add all children of static parents
            AddChildrenToCullable(parent);
        }
        
        totalObjects = allCullableObjects.Count;
        Debug.Log($"Found {totalObjects} cullable objects across {staticObjectParents.Length} parent objects");
    }
    
    void AddChildrenToCullable(Transform parent)
    {
        foreach (Transform child in parent)
        {
            // Skip if object is on excluded layer
            if ((cullingLayerMask.value & (1 << child.gameObject.layer)) == 0)
                continue;
                
            // Only add objects with renderers (visual objects)
            if (child.GetComponentInChildren<Renderer>() != null)
            {
                allCullableObjects.Add(new CullableObject(child.gameObject));
            }
            
            // Recursively add children
            AddChildrenToCullable(child);
        }
    }

    void Update()
    {
        if (!isEnabled) return;
        
        if (Input.GetKeyDown(toggleDebugKey))
            ShowDebugInfo = !ShowDebugInfo;
            
        UpdateComponent();
    }
    
    public override void UpdateComponent()
    {
        if (Time.time < nextUpdateTime) return;
        nextUpdateTime = Time.time + updateInterval;
        
        ProcessCullingBatch();
    }
    
    void ProcessCullingBatch()
    {
        if (allCullableObjects.Count == 0) return;
        
        culledCount = 0;
        int processed = 0;
        
        for (int i = 0; i < maxObjectsPerFrame && processed < totalObjects; i++)
        {
            if (currentObjectIndex >= allCullableObjects.Count)
                currentObjectIndex = 0;
                
            CullableObject obj = allCullableObjects[currentObjectIndex];
            
            if (obj.gameObject == null)
            {
                allCullableObjects.RemoveAt(currentObjectIndex);
                totalObjects--;
                continue;
            }
            
            bool shouldBeVisible = ShouldObjectBeVisible(obj);
            obj.SetVisible(shouldBeVisible);
            
            if (!shouldBeVisible) culledCount++;
            
            currentObjectIndex++;
            processed++;
        }
    }
    
    bool ShouldObjectBeVisible(CullableObject obj)
    {
        Vector3 objPos = obj.position;
        Vector3 cameraPos = cullingCamera.transform.position;
        
        // Distance culling
        if (enableDistanceCulling)
        {
            float distance = Vector3.Distance(objPos, cameraPos);
            if (distance > cullingDistance)
                return false;
        }
        
        // Frustum culling
        if (enableFrustumCulling)
        {
            Plane[] planes = GeometryUtility.CalculateFrustumPlanes(cullingCamera);
            Bounds bounds = new Bounds(objPos, Vector3.one * 2f); // Approximate bounds
            
            if (!GeometryUtility.TestPlanesAABB(planes, bounds))
                return false;
        }
        
        return true;
    }
    
    // OnGUI removed - handled by PerformanceManager composite GUI
    
    public override float RenderDebugGUI(float startY)
    {
        if (!ShowDebugInfo) return startY;
        
        GUIStyle normalStyle = GetDebugGUIStyle(14, Color.white);
        
        float x = 30f; // Indent for section content
        float y = startY;
        float lineHeight = 20f;
        
        // Stats
        GUI.Label(new Rect(x, y, 300, lineHeight), $"Total Objects: {totalObjects}", normalStyle);
        y += lineHeight;
        GUI.Label(new Rect(x, y, 300, lineHeight), $"Culled Objects: {culledCount}", normalStyle);
        y += lineHeight;
        GUI.Label(new Rect(x, y, 300, lineHeight), $"Visible Objects: {totalObjects - culledCount}", normalStyle);
        y += lineHeight;
        GUI.Label(new Rect(x, y, 300, lineHeight), $"Culling Distance: {cullingDistance}m", normalStyle);
        y += lineHeight;
        
        return y;
    }
    
    public override string GetPerformanceMetrics()
    {
        return $"Culled: {culledCount}/{totalObjects} | Distance: {cullingDistance}m";
    }
    
    // Public methods for runtime adjustment
    public void SetCullingDistance(float distance)
    {
        cullingDistance = distance;
    }
    
    public void SetUpdateInterval(float interval)
    {
        updateInterval = interval;
    }
    
    [ContextMenu("Refresh Cullable Objects")]
    public void RefreshCullableObjects()
    {
        InitializeCullableObjects();
    }
}