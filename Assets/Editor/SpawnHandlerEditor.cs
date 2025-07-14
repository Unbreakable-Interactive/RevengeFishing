using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SpawnHandler))]
public class SpawnHandlerEditor : Editor
{
    private SerializedProperty spawnConfigProp;
    private SerializedProperty useScriptableObjectConfigProp;
    private SerializedProperty configuredSpawnPointsProp;
    // private SerializedProperty objectPoolProp;
    // private SerializedProperty playerMovementProp;
    // private SerializedProperty powerLevelScalerProp;
    private SerializedProperty enableSpawnLogsProp;
    
    // Legacy properties
    private SerializedProperty spawnPointsProp;
    private SerializedProperty spawnIntervalProp;
    private SerializedProperty poolNameProp;
    private SerializedProperty enableAutoSpawningProp;
    private SerializedProperty maxEnemiesProp;
    private SerializedProperty minPlayerDistanceProp;

    private void OnEnable()
    {
        // New system properties
        spawnConfigProp = serializedObject.FindProperty("spawnConfig");
        useScriptableObjectConfigProp = serializedObject.FindProperty("useScriptableObjectConfig");
        configuredSpawnPointsProp = serializedObject.FindProperty("configuredSpawnPoints");
        // objectPoolProp = serializedObject.FindProperty("objectPool");
        // playerMovementProp = serializedObject.FindProperty("playerMovement");
        // powerLevelScalerProp = serializedObject.FindProperty("powerLevelScaler");
        enableSpawnLogsProp = serializedObject.FindProperty("enableSpawnLogs");
        
        // Legacy properties
        spawnPointsProp = serializedObject.FindProperty("spawnPoints");
        spawnIntervalProp = serializedObject.FindProperty("spawnInterval");
        poolNameProp = serializedObject.FindProperty("poolName");
        enableAutoSpawningProp = serializedObject.FindProperty("enableAutoSpawning");
        maxEnemiesProp = serializedObject.FindProperty("maxEnemies");
        minPlayerDistanceProp = serializedObject.FindProperty("minPlayerDistance");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        SpawnHandler spawnHandler = (SpawnHandler)target;

        // Header
        EditorGUILayout.Space();
        GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel);
        headerStyle.fontSize = 14;
        headerStyle.normal.textColor = Color.cyan;
        EditorGUILayout.LabelField("🎯 Spawn Handler Configuration", headerStyle);
        EditorGUILayout.Space();

        // Configuration section
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Configuration", EditorStyles.boldLabel);
        
        EditorGUILayout.PropertyField(useScriptableObjectConfigProp, new GUIContent("Use Scriptable Object Config", 
            "Enable modern ScriptableObject-based configuration system. Recommended for better organization and reusability."));
        
        if (useScriptableObjectConfigProp.boolValue)
        {
            EditorGUILayout.PropertyField(spawnConfigProp, new GUIContent("Spawn Config", 
                "The ScriptableObject configuration asset that defines how this spawner behaves."));
            
            // Show helpful info about the current config
            if (spawnConfigProp.objectReferenceValue != null)
            {
                var config = spawnConfigProp.objectReferenceValue as SpawnHandlerConfig;
                if (config != null)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.BeginVertical("helpbox");
                    EditorGUILayout.LabelField("📋 Current Config Summary:", EditorStyles.miniLabel);
                    EditorGUILayout.LabelField($"• Type: {config.spawnType}", EditorStyles.miniLabel);
                    EditorGUILayout.LabelField($"• Pool: {config.poolName}", EditorStyles.miniLabel);
                    EditorGUILayout.LabelField($"• Interval: {config.spawnInterval}s", EditorStyles.miniLabel);
                    EditorGUILayout.LabelField($"• Max Enemies: {config.maxEnemies}", EditorStyles.miniLabel);
                    if (config.spawnType == SpawnHandlerConfig.SpawnType.Limited)
                    {
                        EditorGUILayout.LabelField($"• Max Spawns: {(config.maxSpawns < 0 ? "Unlimited" : config.maxSpawns.ToString())}", EditorStyles.miniLabel);
                    }
                    EditorGUILayout.EndVertical();
                }
            }
            else
            {
                EditorGUILayout.HelpBox("No Spawn Config assigned! Create one via: Create → Spawning System → SpawnHandler Config", MessageType.Warning);
            }
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();

        // Spawn Points section
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Spawn Points", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(configuredSpawnPointsProp, new GUIContent("Configured Spawn Points", 
            "Transform array of spawn points where enemies will appear. Position these where you want enemies to spawn."));
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();

        // Singleton Status section
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Singleton References (Auto-Detected)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("SpawnHandler now uses singletons automatically. No manual references needed!", MessageType.Info);
        
        // Show singleton status (only in play mode for safety)
        if (Application.isPlaying)
        {
            EditorGUILayout.LabelField($"• SimpleObjectPool: {(SimpleObjectPool.Instance != null ? "✅ Ready" : "❌ Not Found")}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"• Player: {(Player.Instance != null ? "✅ Ready" : "❌ Not Found")}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"• PowerLevelScaler: {(PowerLevelScaler.Instance != null ? "✅ Ready" : "❌ Not Found")}", EditorStyles.miniLabel);
        }
        else
        {
            EditorGUILayout.LabelField("• Singleton status available in Play Mode", EditorStyles.miniLabel);
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();

        // Debug section
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Debug", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(enableSpawnLogsProp, new GUIContent("Enable Spawn Logs", 
            "Show detailed spawn information in Console. Turn off for production."));
        EditorGUILayout.EndVertical();

        // Legacy settings (only show if not using ScriptableObject config)
        if (!useScriptableObjectConfigProp.boolValue)
        {
            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical("box");
            
            // Warning header
            GUIStyle warningStyle = new GUIStyle(EditorStyles.boldLabel);
            warningStyle.normal.textColor = Color.yellow;
            EditorGUILayout.LabelField("⚠️ Legacy Settings (Deprecated)", warningStyle);
            EditorGUILayout.HelpBox("These settings are deprecated. Consider migrating to ScriptableObject configs for better organization and reusability.", MessageType.Warning);
            
            EditorGUILayout.PropertyField(spawnPointsProp);
            EditorGUILayout.PropertyField(spawnIntervalProp);
            EditorGUILayout.PropertyField(poolNameProp);
            EditorGUILayout.PropertyField(enableAutoSpawningProp);
            EditorGUILayout.PropertyField(maxEnemiesProp);
            EditorGUILayout.PropertyField(minPlayerDistanceProp);
            EditorGUILayout.EndVertical();
        }

        // Quick action buttons
        EditorGUILayout.Space();
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("🎮 Test Spawn (G Key)", GUILayout.Height(25)))
        {
            if (Application.isPlaying)
            {
                spawnHandler.SpawnSingleAtRandomPoint();
            }
            else
            {
                EditorUtility.DisplayDialog("Test Spawn", "Enter Play Mode to test spawning!", "OK");
            }
        }
        
        if (GUILayout.Button("📋 Create Config Asset", GUILayout.Height(25)))
        {
            CreateSpawnConfigAsset(spawnHandler);
        }
        EditorGUILayout.EndHorizontal();
        
        if (GUILayout.Button("🔧 Check Singleton Status", GUILayout.Height(25)))
        {
            CheckSingletonStatus();
        }
        EditorGUILayout.EndVertical();

        // Runtime info (only in play mode)
        if (Application.isPlaying)
        {
            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("🎮 Runtime Info", EditorStyles.boldLabel);
            
            if (spawnHandler != null)
            {
                EditorGUILayout.LabelField($"Pool Name: {spawnHandler.GetPoolName()}", EditorStyles.miniLabel);
                
                try
                {
                    if (SimpleObjectPool.Instance != null)
                    {
                        int activeCount = SimpleObjectPool.Instance.GetActiveCount(spawnHandler.GetPoolName());
                        EditorGUILayout.LabelField($"Active Objects: {activeCount}", EditorStyles.miniLabel);
                    }
                    else
                    {
                        EditorGUILayout.LabelField("SimpleObjectPool singleton not found", EditorStyles.miniLabel);
                    }
                }
                catch (System.Exception)
                {
                    EditorGUILayout.LabelField("Pool count unavailable", EditorStyles.miniLabel);
                }
            }
            EditorGUILayout.EndVertical();
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void CreateSpawnConfigAsset(SpawnHandler spawnHandler)
    {
        string path = EditorUtility.SaveFilePanelInProject(
            "Create SpawnHandler Config",
            "New SpawnHandler Config",
            "asset",
            "Choose location for new SpawnHandler Config"
        );

        if (!string.IsNullOrEmpty(path))
        {
            SpawnHandlerConfig newConfig = CreateInstance<SpawnHandlerConfig>();
            
            // Copy current settings if not using ScriptableObject
            if (!useScriptableObjectConfigProp.boolValue)
            {
                newConfig.poolName = poolNameProp.stringValue;
                newConfig.spawnInterval = spawnIntervalProp.floatValue;
                newConfig.maxEnemies = maxEnemiesProp.intValue;
                newConfig.minPlayerDistance = minPlayerDistanceProp.floatValue;
                newConfig.enableAutoSpawning = enableAutoSpawningProp.boolValue;
            }
            
            AssetDatabase.CreateAsset(newConfig, path);
            AssetDatabase.SaveAssets();
            
            // Auto-assign the new config
            spawnConfigProp.objectReferenceValue = newConfig;
            useScriptableObjectConfigProp.boolValue = true;
            serializedObject.ApplyModifiedProperties();
            
            EditorGUIUtility.PingObject(newConfig);
            EditorUtility.DisplayDialog("Config Created", $"SpawnHandler Config created at:\n{path}\n\nIt has been automatically assigned to this SpawnHandler.", "OK");
        }
    }

    private void CheckSingletonStatus()
    {
        string report = "🔍 Singleton Status Report:\n\n";
        
        // Use safe checks with FindObjectOfType as fallback
        bool poolReady = false;
        bool playerReady = false; 
        bool scalerReady = false;
        
        if (Application.isPlaying)
        {
            poolReady = SimpleObjectPool.Instance != null;
            playerReady = Player.Instance != null;
            scalerReady = PowerLevelScaler.Instance != null;
        }
        else
        {
            // In edit mode, use FindObjectOfType
            poolReady = FindObjectOfType<SimpleObjectPool>() != null;
            playerReady = FindObjectOfType<Player>() != null;
            scalerReady = FindObjectOfType<PowerLevelScaler>() != null;
        }
        
        report += $"• SimpleObjectPool: {(poolReady ? "✅ Ready" : "❌ Not Found")}\n";
        report += $"• Player: {(playerReady ? "✅ Ready" : "❌ Not Found")}\n";
        report += $"• PowerLevelScaler: {(scalerReady ? "✅ Ready" : "❌ Not Found")}\n\n";
        
        bool allReady = poolReady && playerReady && scalerReady;
        
        if (allReady)
        {
            report += "🎉 All singleton systems are ready for spawning!";
        }
        else
        {
            report += "⚠️ Some singletons are missing. Make sure they exist in the scene:\n";
            if (!poolReady) report += "- Add SimpleObjectPool component to a GameObject\n";
            if (!playerReady) report += "- Ensure Player script has singleton setup\n";
            if (!scalerReady) report += "- Add PowerLevelScaler component to a GameObject\n";
        }
        
        EditorUtility.DisplayDialog("Singleton Status", report, "OK");
    }
}