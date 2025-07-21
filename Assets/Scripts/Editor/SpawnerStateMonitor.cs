using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System;

namespace DebugTools.SystemMonitor
{
    public class SpawnerStateMonitor : EditorWindow
    {
        [MenuItem("Tools/Debug Tools/Spawner State Monitor")]
        public static void ShowWindow()
        {
            var window = GetWindow<SpawnerStateMonitor>("Spawner Monitor");
            window.minSize = new Vector2(600, 500);
            window.Show();
        }

        private Dictionary<int, SpawnerInfo> spawnerRegistry = new Dictionary<int, SpawnerInfo>();
        private Dictionary<int, List<SpawnedObjectInfo>> spawnedObjects = new Dictionary<int, List<SpawnedObjectInfo>>();
        private Vector2 scrollPosition;
        private Vector2 objectScrollPosition;
        private double lastRefreshTime;
        private double lastFullScanTime;
        private double lastRepaintTime;
        private int selectedSpawnerID = -1;
        private bool showStateDetails = true;
        private bool showPositionTracking = true;
        private bool highlightStateErrors = true;
        private float refreshRate = 0.1f;
        private bool isMonitoring = false;

        private bool IsActivelyMonitoring => Application.isPlaying;

        [Serializable]
        private class SpawnerInfo
        {
            public int instanceID;
            public string spawnerName;
            public MonoBehaviour spawnerComponent;
            public GameObject gameObject;
            public SpawnerType type;
            public int activeCount;
            public int maxCapacity;
            public float lastSpawnTime;
            public Vector3 lastSpawnPosition;
            public bool hasErrors;
            public string errorMessage;
            public List<Transform> spawnPoints = new List<Transform>();
            public Dictionary<string, object> lastFrameState = new Dictionary<string, object>();
            public Dictionary<string, object> currentFrameState = new Dictionary<string, object>();
            public List<string> stateChangeLog = new List<string>();

            public SpawnerInfo(MonoBehaviour comp)
            {
                spawnerComponent = comp;
                gameObject = comp.gameObject;
                instanceID = comp.GetInstanceID();
                spawnerName = comp.GetType().Name;
                type = DetermineSpawnerType(comp);
                RefreshStatus();
            }

            public void RefreshStatus()
            {
                if (spawnerComponent == null)
                {
                    hasErrors = true;
                    errorMessage = "Spawner component is null";
                    return;
                }

                hasErrors = false;
                errorMessage = "";
                
                lastFrameState = new Dictionary<string, object>(currentFrameState);
                currentFrameState.Clear();

                AnalyzeSpawnerState();
                DetectStateChanges();
            }

            private SpawnerType DetermineSpawnerType(MonoBehaviour comp)
            {
                string typeName = comp.GetType().Name;
                if (typeName.Contains("Hook"))
                    return SpawnerType.HookSpawner;
                else if (typeName.Contains("Spawn") || typeName.Contains("Handler"))
                    return SpawnerType.EnemySpawner;
                else if (typeName.Contains("Pool"))
                    return SpawnerType.ObjectPool;
                else
                    return SpawnerType.Unknown;
            }

            private void AnalyzeSpawnerState()
            {
                try
                {
                    var type = spawnerComponent.GetType();

                    if (type.Name == "SpawnHandler")
                    {
                        AnalyzeSpawnHandler();
                    }
                    else if (type.Name == "HookSpawner")
                    {
                        AnalyzeHookSpawner();
                    }
                    else
                    {
                        AnalyzeGenericSpawner();
                    }
                }
                catch (Exception ex)
                {
                    hasErrors = true;
                    errorMessage = $"Analysis failed: {ex.Message}";
                }
            }

            private void AnalyzeSpawnHandler()
            {
                var fields = spawnerComponent.GetType().GetFields(
                    System.Reflection.BindingFlags.NonPublic | 
                    System.Reflection.BindingFlags.Instance | 
                    System.Reflection.BindingFlags.Public);

                Transform[] configuredSpawnPoints = null;
                Transform[] legacySpawnPoints = null;

                foreach (var field in fields)
                {
                    try
                    {
                        var value = field.GetValue(spawnerComponent);
                        currentFrameState[field.Name] = value;

                        if (field.Name == "configuredSpawnPoints")
                        {
                            configuredSpawnPoints = value as Transform[];
                        }
                        else if (field.Name == "spawnPoints")
                        {
                            legacySpawnPoints = value as Transform[];
                        }
                        else if (field.Name == "currentEnemyCount")
                        {
                            activeCount = value != null ? (int)value : 0;
                        }
                        else if (field.Name == "maxEnemies")
                        {
                            maxCapacity = value != null ? (int)value : 0;
                        }
                        else if (field.Name == "lastSpawnTime")
                        {
                            lastSpawnTime = value != null ? (float)value : 0;
                        }
                    }
                    catch (Exception ex)
                    {
                        currentFrameState[field.Name] = $"Error: {ex.Message}";
                    }
                }

                Transform[] activeSpawnPoints = null;
                if (configuredSpawnPoints != null && configuredSpawnPoints.Length > 0)
                {
                    activeSpawnPoints = configuredSpawnPoints;
                    currentFrameState["ActiveSpawnPointSource"] = "configuredSpawnPoints";
                }
                else if (legacySpawnPoints != null && legacySpawnPoints.Length > 0)
                {
                    activeSpawnPoints = legacySpawnPoints;
                    currentFrameState["ActiveSpawnPointSource"] = "spawnPoints (legacy)";
                }

                if (activeSpawnPoints != null)
                {
                    spawnPoints = activeSpawnPoints.Where(t => t != null).ToList();
                    currentFrameState["ActiveSpawnPointCount"] = spawnPoints.Count;
                    currentFrameState["TotalSpawnPointSlots"] = activeSpawnPoints.Length;
                }
                else
                {
                    spawnPoints.Clear();
                    currentFrameState["ActiveSpawnPointSource"] = "none found";
                }

                ValidateSpawnHandler();
            }

            private void AnalyzeHookSpawner()
            {
                var fields = spawnerComponent.GetType().GetFields(
                    System.Reflection.BindingFlags.NonPublic | 
                    System.Reflection.BindingFlags.Instance | 
                    System.Reflection.BindingFlags.Public);

                foreach (var field in fields)
                {
                    try
                    {
                        var value = field.GetValue(spawnerComponent);
                        currentFrameState[field.Name] = value;

                        if (field.Name.Contains("currentHook") || field.Name.Contains("hookHandler"))
                        {
                            activeCount = value != null ? 1 : 0;
                            maxCapacity = 1;
                        }
                        else if (field.Name == "spawnPoint" || field.Name == "_spawnPoint")
                        {
                            if (value is Transform spawnPoint && spawnPoint != null)
                            {
                                spawnPoints = new List<Transform> { spawnPoint };
                                lastSpawnPosition = spawnPoint.position;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        currentFrameState[field.Name] = $"Error: {ex.Message}";
                    }
                }

                ValidateHookSpawner();
            }

            private void AnalyzeGenericSpawner()
            {
                var fields = spawnerComponent.GetType().GetFields(
                    System.Reflection.BindingFlags.NonPublic | 
                    System.Reflection.BindingFlags.Instance | 
                    System.Reflection.BindingFlags.Public);

                foreach (var field in fields)
                {
                    try
                    {
                        var value = field.GetValue(spawnerComponent);
                        currentFrameState[field.Name] = value;

                        if (field.Name.ToLower().Contains("count"))
                        {
                            if (int.TryParse(value?.ToString(), out int count))
                                activeCount = count;
                        }
                        else if (field.Name.ToLower().Contains("max"))
                        {
                            if (int.TryParse(value?.ToString(), out int max))
                                maxCapacity = max;
                        }
                    }
                    catch (Exception ex)
                    {
                        currentFrameState[field.Name] = $"Error: {ex.Message}";
                    }
                }
            }

            private void ValidateSpawnHandler()
            {
                hasErrors = false;
                errorMessage = "";

                bool hasConfiguredPoints = currentFrameState.ContainsKey("configuredSpawnPoints") && 
                                          currentFrameState["configuredSpawnPoints"] != null;
                bool hasLegacyPoints = currentFrameState.ContainsKey("spawnPoints") && 
                                      currentFrameState["spawnPoints"] != null;

                if (!hasConfiguredPoints && !hasLegacyPoints)
                {
                    hasErrors = true;
                    errorMessage = "No spawn point arrays found (neither configuredSpawnPoints nor spawnPoints)";
                    return;
                }

                if (spawnPoints.Count == 0)
                {
                    if (hasConfiguredPoints && !hasLegacyPoints)
                    {
                        hasErrors = true;
                        errorMessage = "configuredSpawnPoints array exists but is empty or contains null references";
                    }
                    else if (!hasConfiguredPoints && hasLegacyPoints)
                    {
                        hasErrors = true;
                        errorMessage = "spawnPoints array exists but is empty or contains null references";
                    }
                    else
                    {
                        hasErrors = true;
                        errorMessage = "Both spawn point arrays exist but neither has valid Transform references";
                    }
                    return;
                }

                var activeSource = currentFrameState.ContainsKey("ActiveSpawnPointSource") ? 
                                  currentFrameState["ActiveSpawnPointSource"].ToString() : "unknown";
                
                int totalSlots = currentFrameState.ContainsKey("TotalSpawnPointSlots") ? 
                                (int)currentFrameState["TotalSpawnPointSlots"] : 0;
                
                if (spawnPoints.Count < totalSlots)
                {
                    hasErrors = true;
                    errorMessage = $"{activeSource} has {totalSlots - spawnPoints.Count} null references out of {totalSlots} slots";
                    return;
                }

                if (maxCapacity <= 0)
                {
                    hasErrors = true;
                    errorMessage = "maxEnemies not configured or set to zero";
                    return;
                }

                currentFrameState["ValidationStatus"] = $"✓ Using {activeSource} with {spawnPoints.Count} valid spawn points";
            }

            private void ValidateHookSpawner()
            {
                if (spawnPoints.Count == 0)
                {
                    hasErrors = true;
                    errorMessage = "Hook spawn point not configured";
                }
            }

            private void DetectStateChanges()
            {
                foreach (var kvp in currentFrameState)
                {
                    if (lastFrameState.ContainsKey(kvp.Key))
                    {
                        var lastValue = lastFrameState[kvp.Key]?.ToString() ?? "null";
                        var currentValue = kvp.Value?.ToString() ?? "null";
                        
                        if (lastValue != currentValue && ShouldLogStateChange(kvp.Key))
                        {
                            string changeMsg = $"{spawnerName}.{kvp.Key}: {lastValue} -> {currentValue}";
                            stateChangeLog.Add($"[{Time.time:F1}s] {changeMsg}");
                            
                            if (stateChangeLog.Count > 20)
                                stateChangeLog.RemoveAt(0);
                                
                            Debug.Log($"[SpawnerMonitor] {changeMsg}");
                        }
                    }
                }
            }

            private bool ShouldLogStateChange(string fieldName)
            {
                string lowerName = fieldName.ToLower();
                return lowerName.Contains("count") ||
                       lowerName.Contains("spawn") ||
                       lowerName.Contains("hook") ||
                       lowerName.Contains("timer") ||
                       lowerName.Contains("state");
            }
        }

        [Serializable]
        private class SpawnedObjectInfo
        {
            public int instanceID;
            public GameObject gameObject;
            public string objectName;
            public Vector3 currentPosition;
            public Vector3 lastPosition;
            public Vector3 spawnPosition;
            public float spawnTime;
            public bool hasStateError;
            public string stateErrorMessage;
            public Dictionary<string, object> stateSnapshot = new Dictionary<string, object>();
            public Dictionary<string, object> lastStateSnapshot = new Dictionary<string, object>();
            public List<string> errorLog = new List<string>();

            public SpawnedObjectInfo(GameObject obj, Vector3 spawnPos)
            {
                gameObject = obj;
                instanceID = obj.GetInstanceID();
                objectName = obj.name;
                spawnPosition = spawnPos;
                currentPosition = obj.transform.position;
                lastPosition = currentPosition;
                spawnTime = Time.time;
                CaptureStateSnapshot();
            }

            public void RefreshState()
            {
                if (gameObject == null) return;

                lastPosition = currentPosition;
                currentPosition = gameObject.transform.position;
                
                lastStateSnapshot = new Dictionary<string, object>(stateSnapshot);
                
                DetectStateErrors();
                CaptureStateSnapshot();
                DetectStateChanges();
            }

            private void CaptureStateSnapshot()
            {
                if (gameObject == null) return;

                stateSnapshot.Clear();
                
                stateSnapshot["Position"] = gameObject.transform.position.ToString("F2");
                stateSnapshot["Rotation"] = gameObject.transform.rotation.eulerAngles.ToString("F1");
                stateSnapshot["Active"] = gameObject.activeInHierarchy.ToString();
                stateSnapshot["LocalActive"] = gameObject.activeSelf.ToString();

                if (gameObject.name.Contains("Fisherman"))
                {
                    CaptureFishermanState();
                }
                else if (gameObject.name.Contains("Hook"))
                {
                    CaptureHookState();
                }
            }

            private void CaptureFishermanState()
            {
                var fisherman = gameObject.GetComponent<MonoBehaviour>();
                if (fisherman == null) return;

                var fields = fisherman.GetType().GetFields(
                    System.Reflection.BindingFlags.NonPublic | 
                    System.Reflection.BindingFlags.Instance | 
                    System.Reflection.BindingFlags.Public);

                foreach (var field in fields)
                {
                    if (ShouldCaptureField(field.Name))
                    {
                        try
                        {
                            var value = field.GetValue(fisherman);
                            stateSnapshot[field.Name] = value?.ToString() ?? "null";
                        }
                        catch
                        {
                            stateSnapshot[field.Name] = "Error reading value";
                        }
                    }
                }
            }

            private void CaptureHookState()
            {
                var fishingProjectile = gameObject.GetComponent<MonoBehaviour>();
                if (fishingProjectile == null) return;

                var fields = fishingProjectile.GetType().GetFields(
                    System.Reflection.BindingFlags.NonPublic | 
                    System.Reflection.BindingFlags.Instance | 
                    System.Reflection.BindingFlags.Public);

                foreach (var field in fields)
                {
                    if (ShouldCaptureField(field.Name))
                    {
                        try
                        {
                            var value = field.GetValue(fishingProjectile);
                            stateSnapshot[field.Name] = value?.ToString() ?? "null";
                        }
                        catch
                        {
                            stateSnapshot[field.Name] = "Error reading value";
                        }
                    }
                }
            }

            private bool ShouldCaptureField(string fieldName)
            {
                string lowerName = fieldName.ToLower();
                return lowerName.Contains("state") || 
                       lowerName.Contains("hook") ||
                       lowerName.Contains("timer") ||
                       lowerName.Contains("equipped") ||
                       lowerName.Contains("thrown") ||
                       lowerName.Contains("distance") ||
                       lowerName.Contains("target") ||
                       lowerName.Contains("velocity") ||
                       lowerName.Contains("speed");
            }

            private void DetectStateErrors()
            {
                hasStateError = false;
                stateErrorMessage = "";

                float distanceFromSpawn = Vector3.Distance(currentPosition, spawnPosition);
                if (distanceFromSpawn > 100f)
                {
                    LogError($"Object too far from spawn: {distanceFromSpawn:F1}m");
                    return;
                }

                float timeSinceSpawn = Time.time - spawnTime;
                if (timeSinceSpawn > 5f && distanceFromSpawn < 0.1f && gameObject.activeInHierarchy)
                {
                    LogError("Object stuck at spawn position");
                    return;
                }

                float frameMovement = Vector3.Distance(currentPosition, lastPosition);
                if (frameMovement > 20f)
                {
                    LogError($"Object teleported: {frameMovement:F1}m in one frame");
                    return;
                }

                if (objectName.Contains("Fisherman"))
                {
                    DetectFishermanStateErrors();
                }
                else if (objectName.Contains("Hook"))
                {
                    DetectHookStateErrors();
                }
            }

            private void DetectFishermanStateErrors()
            {
                if (stateSnapshot.ContainsKey("hookTimer"))
                {
                    if (float.TryParse(stateSnapshot["hookTimer"].ToString(), out float hookTimer))
                    {
                        if (hookTimer > 30f)
                        {
                            LogError($"Hook timer stuck at {hookTimer:F1}s");
                            return;
                        }
                    }
                }

                bool hasThrownHook = stateSnapshot.ContainsKey("hasThrownHook") && 
                                   stateSnapshot["hasThrownHook"].ToString() == "True";
                bool hasActiveHook = stateSnapshot.ContainsKey("currentHook") && 
                                   stateSnapshot["currentHook"].ToString() != "null";

                if (hasThrownHook && !hasActiveHook)
                {
                    LogError("Inconsistent hook state: thrown but no active hook");
                }
            }

            private void DetectHookStateErrors()
            {
                if (gameObject.activeInHierarchy)
                {
                    float distanceFromSpawn = Vector3.Distance(currentPosition, spawnPosition);
                    if (distanceFromSpawn < 0.1f && Time.time - spawnTime > 1f)
                    {
                        LogError("Active hook hasn't moved from spawn");
                    }
                }
            }

            private void LogError(string message)
            {
                hasStateError = true;
                stateErrorMessage = message;
                
                string errorEntry = $"[{Time.time:F1}s] {message}";
                errorLog.Add(errorEntry);
                
                if (errorLog.Count > 10)
                    errorLog.RemoveAt(0);
                    
                Debug.LogWarning($"[SpawnerMonitor] {objectName}: {message}");
            }

            private void DetectStateChanges()
            {
                foreach (var kvp in stateSnapshot)
                {
                    if (lastStateSnapshot.ContainsKey(kvp.Key))
                    {
                        var lastValue = lastStateSnapshot[kvp.Key]?.ToString() ?? "null";
                        var currentValue = kvp.Value?.ToString() ?? "null";
                        
                        if (lastValue != currentValue && ShouldLogStateChange(kvp.Key))
                        {
                            Debug.Log($"[SpawnerMonitor] {objectName}.{kvp.Key}: {lastValue} -> {currentValue}");
                        }
                    }
                }
            }

            private bool ShouldLogStateChange(string fieldName)
            {
                string lowerName = fieldName.ToLower();
                return lowerName.Contains("state") ||
                       lowerName.Contains("thrown") ||
                       lowerName.Contains("equipped") ||
                       lowerName == "Active" ||
                       lowerName == "LocalActive";
            }
        }

        private enum SpawnerType
        {
            EnemySpawner,
            HookSpawner,
            ObjectPool,
            Unknown
        }

        private void OnEnable()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            EditorApplication.update += OnEditorUpdate;
            RefreshSpawnerData();
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.update -= OnEditorUpdate;
        }

        private void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                isMonitoring = true;
                RefreshSpawnerData();
                Debug.Log("[SpawnerMonitor] Started continuous runtime monitoring");
            }
            else if (state == PlayModeStateChange.ExitingPlayMode)
            {
                isMonitoring = false;
                Debug.Log("[SpawnerMonitor] Stopped runtime monitoring");
            }
        }

        private void OnEditorUpdate()
        {
            double currentTime = EditorApplication.timeSinceStartup;

            if (IsActivelyMonitoring)
            {
                if (currentTime - lastRefreshTime > refreshRate)
                {
                    RefreshStatesOnly();
                    lastRefreshTime = currentTime;
                }

                if (currentTime - lastFullScanTime > 2.0f)
                {
                    RefreshSpawnerData();
                    lastFullScanTime = currentTime;
                }

                if (currentTime - lastRepaintTime > 0.5f)
                {
                    Repaint();
                    lastRepaintTime = currentTime;
                }
            }
        }

        private void OnGUI()
        {
            DrawToolbar();
            DrawSpawnerList();
            DrawSelectedSpawnerDetails();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            GUILayout.Label("Spawner State Monitor", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();

            if (IsActivelyMonitoring)
            {
                var oldColor = GUI.backgroundColor;
                GUI.backgroundColor = Color.green;
                GUILayout.Label("MONITORING ACTIVE", EditorStyles.miniButton);
                GUI.backgroundColor = oldColor;
            }
            else if (Application.isPlaying)
            {
                var oldColor = GUI.backgroundColor;
                GUI.backgroundColor = Color.yellow;
                GUILayout.Label("PLAY MODE", EditorStyles.miniButton);
                GUI.backgroundColor = oldColor;
            }
            else
            {
                GUILayout.Label("EDIT MODE", EditorStyles.miniButton);
            }

            if (GUILayout.Button("Manual Refresh", EditorStyles.toolbarButton, GUILayout.Width(90)))
            {
                RefreshSpawnerData();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            showStateDetails = EditorGUILayout.Toggle("Show State Details", showStateDetails);
            showPositionTracking = EditorGUILayout.Toggle("Show Position Tracking", showPositionTracking);
            highlightStateErrors = EditorGUILayout.Toggle("Highlight State Errors", highlightStateErrors);
            
            GUILayout.Label("Refresh Rate:", GUILayout.Width(80));
            refreshRate = EditorGUILayout.Slider(refreshRate, 0.05f, 1f, GUILayout.Width(100));
            GUILayout.Label($"{1f/refreshRate:F0} Hz", GUILayout.Width(40));
            
            EditorGUILayout.EndHorizontal();

            if (IsActivelyMonitoring)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.HelpBox(
                    $"Continuous monitoring active - Updates every {refreshRate:F2}s ({1f/refreshRate:F0} Hz)", 
                    MessageType.Info);
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space();
        }

        private void DrawSpawnerList()
        {
            EditorGUILayout.LabelField($"Active Spawners ({spawnerRegistry.Count})", EditorStyles.boldLabel);
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));

            foreach (var spawner in spawnerRegistry.Values.OrderBy(s => s.spawnerName))
            {
                DrawSpawnerRow(spawner);
            }

            if (spawnerRegistry.Count == 0)
            {
                if (IsActivelyMonitoring)
                {
                    EditorGUILayout.HelpBox("No spawners found. Monitoring continues...", MessageType.Info);
                }
                else
                {
                    EditorGUILayout.HelpBox("No spawners found in current scene.", MessageType.Info);
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawSpawnerRow(SpawnerInfo spawner)
        {
            Color backgroundColor = selectedSpawnerID == spawner.instanceID ? Color.cyan : Color.white;
            var oldColor = GUI.backgroundColor;
            GUI.backgroundColor = backgroundColor;

            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            GUI.backgroundColor = oldColor;

            Color statusColor = spawner.hasErrors ? Color.red : 
                               spawner.activeCount > 0 ? Color.green : Color.yellow;

            oldColor = GUI.backgroundColor;
            GUI.backgroundColor = statusColor;
            GUILayout.Label("●", EditorStyles.boldLabel, GUILayout.Width(15));
            GUI.backgroundColor = oldColor;

            if (GUILayout.Button($"{spawner.spawnerName} ({spawner.type})", EditorStyles.linkLabel))
            {
                selectedSpawnerID = spawner.instanceID;
                if (spawner.gameObject != null)
                {
                    Selection.activeGameObject = spawner.gameObject;
                    EditorGUIUtility.PingObject(spawner.gameObject);
                }
            }

            GUILayout.FlexibleSpace();

            GUILayout.Label($"{spawner.activeCount}/{spawner.maxCapacity}", GUILayout.Width(50));
            
            if (spawner.hasErrors)
            {
                GUI.backgroundColor = Color.red;
                if (GUILayout.Button("!", EditorStyles.miniButton, GUILayout.Width(20)))
                {
                    EditorUtility.DisplayDialog("Spawner Error", 
                        $"Spawner: {spawner.spawnerName}\nError: {spawner.errorMessage}", "OK");
                }
                GUI.backgroundColor = oldColor;
            }

            if (IsActivelyMonitoring && spawner.stateChangeLog.Count > 0)
            {
                GUI.backgroundColor = Color.yellow;
                GUILayout.Label($"Changes: {spawner.stateChangeLog.Count}", EditorStyles.miniButton, GUILayout.Width(80));
                GUI.backgroundColor = oldColor;
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawSelectedSpawnerDetails()
        {
            if (selectedSpawnerID == -1 || !spawnerRegistry.ContainsKey(selectedSpawnerID))
                return;

            var spawner = spawnerRegistry[selectedSpawnerID];

            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"Spawner Details: {spawner.spawnerName}", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.LabelField($"Type: {spawner.type}");
            EditorGUILayout.LabelField($"Active Objects: {spawner.activeCount}/{spawner.maxCapacity}");
            EditorGUILayout.LabelField($"Spawn Points: {spawner.spawnPoints.Count}");
            
            if (IsActivelyMonitoring)
            {
                EditorGUILayout.LabelField($"Last Spawn Time: {spawner.lastSpawnTime:F1}s");
                EditorGUILayout.LabelField($"Current Game Time: {Time.time:F1}s");
                EditorGUILayout.LabelField($"Monitoring: ACTIVE ({1f/refreshRate:F0} Hz)");
            }

            if (spawner.hasErrors)
            {
                EditorGUILayout.HelpBox($"Error: {spawner.errorMessage}", MessageType.Error);
            }

            if (showStateDetails && spawner.currentFrameState.Count > 0)
            {
                EditorGUILayout.LabelField("Current State:", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                
                var sortedStates = spawner.currentFrameState.OrderBy(kvp => kvp.Key);
                foreach (var kvp in sortedStates)
                {
                    bool hasChanged = spawner.lastFrameState.ContainsKey(kvp.Key) && 
                                    spawner.lastFrameState[kvp.Key]?.ToString() != kvp.Value?.ToString();
                    
                    if (hasChanged && IsActivelyMonitoring)
                    {
                        var changeColor = GUI.backgroundColor;
                        GUI.backgroundColor = Color.yellow;
                        EditorGUILayout.LabelField($"{kvp.Key}: {kvp.Value}", EditorStyles.miniLabel);
                        GUI.backgroundColor = changeColor;
                    }
                    else
                    {
                        EditorGUILayout.LabelField($"{kvp.Key}: {kvp.Value}", EditorStyles.miniLabel);
                    }
                }
                EditorGUI.indentLevel--;
            }

            if (IsActivelyMonitoring && spawner.stateChangeLog.Count > 0)
            {
                EditorGUILayout.LabelField("Recent State Changes:", EditorStyles.boldLabel);
                foreach (var change in spawner.stateChangeLog.TakeLast(5))
                {
                    EditorGUILayout.LabelField(change, EditorStyles.miniLabel);
                }
            }

            EditorGUILayout.EndVertical();

            if (spawnedObjects.ContainsKey(selectedSpawnerID))
            {
                DrawSpawnedObjectsList(spawnedObjects[selectedSpawnerID]);
            }
        }

        private void DrawSpawnedObjectsList(List<SpawnedObjectInfo> objects)
        {
            EditorGUILayout.LabelField($"Spawned Objects ({objects.Count})", EditorStyles.boldLabel);

            objectScrollPosition = EditorGUILayout.BeginScrollView(objectScrollPosition, GUILayout.Height(250));

            foreach (var obj in objects.Where(o => o.gameObject != null))
            {
                DrawSpawnedObjectRow(obj);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawSpawnedObjectRow(SpawnedObjectInfo obj)
        {
            Color backgroundColor = obj.hasStateError && highlightStateErrors ? 
                                  new Color(1f, 0.8f, 0.8f) : Color.white;
            
            var oldColor = GUI.backgroundColor;
            GUI.backgroundColor = backgroundColor;
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUI.backgroundColor = oldColor;

            EditorGUILayout.BeginHorizontal();

            if (obj.hasStateError && highlightStateErrors)
            {
                oldColor = GUI.backgroundColor;
                GUI.backgroundColor = Color.red;
                GUILayout.Label("!", EditorStyles.boldLabel, GUILayout.Width(15));
                GUI.backgroundColor = oldColor;
            }

            if (GUILayout.Button(obj.objectName, EditorStyles.linkLabel))
            {
                Selection.activeGameObject = obj.gameObject;
                EditorGUIUtility.PingObject(obj.gameObject);
            }

            GUILayout.FlexibleSpace();
            
            if (IsActivelyMonitoring)
            {
                GUILayout.Label($"Age: {Time.time - obj.spawnTime:F1}s", GUILayout.Width(80));
            }

            EditorGUILayout.EndHorizontal();

            if (showPositionTracking)
            {
                EditorGUILayout.LabelField($"Current: {obj.currentPosition.ToString("F1")}");
                EditorGUILayout.LabelField($"Spawned: {obj.spawnPosition.ToString("F1")}");
                float distance = Vector3.Distance(obj.currentPosition, obj.spawnPosition);
                EditorGUILayout.LabelField($"Distance from spawn: {distance:F1}m");
            }

            if (obj.hasStateError)
            {
                EditorGUILayout.HelpBox($"State Error: {obj.stateErrorMessage}", MessageType.Warning);
                
                if (obj.errorLog.Count > 0)
                {
                    EditorGUILayout.LabelField("Recent Errors:", EditorStyles.miniLabel);
                    foreach (var error in obj.errorLog.TakeLast(3))
                    {
                        EditorGUILayout.LabelField(error, EditorStyles.miniLabel);
                    }
                }
            }

            if (showStateDetails && obj.stateSnapshot.Count > 0)
            {
                EditorGUILayout.LabelField("State Snapshot:", EditorStyles.miniLabel);
                EditorGUI.indentLevel++;
                
                var sortedStates = obj.stateSnapshot.OrderBy(kvp => kvp.Key);
                foreach (var kvp in sortedStates)
                {
                    bool hasChanged = obj.lastStateSnapshot.ContainsKey(kvp.Key) && 
                                    obj.lastStateSnapshot[kvp.Key]?.ToString() != kvp.Value?.ToString();
                    
                    if (hasChanged && IsActivelyMonitoring)
                    {
                        oldColor = GUI.backgroundColor;
                        GUI.backgroundColor = Color.yellow;
                        EditorGUILayout.LabelField($"{kvp.Key}: {kvp.Value}", EditorStyles.miniLabel);
                        GUI.backgroundColor = oldColor;
                    }
                    else
                    {
                        EditorGUILayout.LabelField($"{kvp.Key}: {kvp.Value}", EditorStyles.miniLabel);
                    }
                }
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
        }

        private void RefreshSpawnerData()
        {
            spawnerRegistry.Clear();
            spawnedObjects.Clear();

            var allSpawners = FindObjectsOfType<MonoBehaviour>()
                .Where(comp => IsSpawnerComponent(comp))
                .ToArray();

            foreach (var spawner in allSpawners)
            {
                var spawnerInfo = new SpawnerInfo(spawner);
                spawnerRegistry[spawnerInfo.instanceID] = spawnerInfo;
                FindSpawnedObjects(spawnerInfo);
            }

            if (IsActivelyMonitoring)
            {
                Debug.Log($"[SpawnerMonitor] Full scan: {spawnerRegistry.Count} spawners, {spawnedObjects.Values.Sum(list => list.Count)} objects");
            }
        }

        private void RefreshStatesOnly()
        {
            foreach (var spawner in spawnerRegistry.Values.ToList())
            {
                if (spawner.spawnerComponent == null)
                {
                    spawnerRegistry.Remove(spawner.instanceID);
                    continue;
                }
                
                spawner.RefreshStatus();
            }

            foreach (var objectList in spawnedObjects.Values)
            {
                foreach (var obj in objectList.ToList())
                {
                    if (obj.gameObject == null)
                    {
                        objectList.Remove(obj);
                        continue;
                    }
                    
                    obj.RefreshState();
                }
            }
        }

        private bool IsSpawnerComponent(MonoBehaviour comp)
        {
            if (comp == null) return false;
            
            string typeName = comp.GetType().Name;
            return typeName.Contains("Spawn") || 
                   typeName.Contains("Pool") ||
                   typeName == "HookSpawner";
        }

        private void FindSpawnedObjects(SpawnerInfo spawner)
        {
            var objectList = new List<SpawnedObjectInfo>();

            if (spawner.type == SpawnerType.EnemySpawner)
            {
                var enemies = FindObjectsOfType<GameObject>()
                    .Where(obj => (obj.name.Contains("Fisherman") || obj.name.Contains("Enemy")) &&
                                 obj.GetComponent<MonoBehaviour>() != null)
                    .ToArray();

                foreach (var enemy in enemies)
                {
                    var nearestSpawnPoint = spawner.spawnPoints
                        .OrderBy(sp => Vector3.Distance(sp.position, enemy.transform.position))
                        .FirstOrDefault();

                    if (nearestSpawnPoint != null)
                    {
                        var objInfo = new SpawnedObjectInfo(enemy, nearestSpawnPoint.position);
                        objectList.Add(objInfo);
                    }
                }
            }
            else if (spawner.type == SpawnerType.HookSpawner)
            {
                var hooks = FindObjectsOfType<GameObject>()
                    .Where(obj => (obj.name.Contains("Hook") || obj.name.Contains("Projectile")) &&
                                 obj.GetComponent<MonoBehaviour>() != null)
                    .ToArray();

                foreach (var hook in hooks)
                {
                    var objInfo = new SpawnedObjectInfo(hook, spawner.lastSpawnPosition);
                    objectList.Add(objInfo);
                }
            }

            if (objectList.Count > 0)
            {
                spawnedObjects[spawner.instanceID] = objectList;
            }
        }
    }
}
