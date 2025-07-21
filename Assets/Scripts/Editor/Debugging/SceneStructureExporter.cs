using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;

namespace DebugTools.SystemMonitor
{
    public class SceneStructureVisualizerComplete : EditorWindow
    {
        [MenuItem("Tools/Debug Tools/Live Runtime Bug Detector")]
        public static void ShowWindow()
        {
            // Just export and open - no scene objects needed!
            string sceneData = ExportSceneData();
            string htmlPath = CreateVisualizerHTML(sceneData);
            Application.OpenURL($"file://{htmlPath}");
            Debug.Log("🚀 Bug Detector opened! This analyzes your current scene structure and detects potential issues.");
        }

        private static string ExportSceneData()
        {
            var sceneInfo = new SceneInfo
            {
                sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
                systems = new List<GameSystemData>(),
                objects = new List<GameObjectData>(),
                staticIssues = new List<IssueData>(),
                summary = new SceneSummary()
            };

            var rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            var allObjects = new List<GameObject>();
            
            foreach (var rootObj in rootObjects)
            {
                CollectAllObjects(rootObj, allObjects);
            }

            var systemGroups = GroupObjectsBySystem(allObjects);
            
            foreach (var systemGroup in systemGroups)
            {
                var systemData = CreateSystemData(systemGroup.Key, systemGroup.Value);
                sceneInfo.systems.Add(systemData);
            }

            foreach (var obj in allObjects)
            {
                var objData = CreateObjectData(obj);
                sceneInfo.objects.Add(objData);
                DetectStaticIssues(obj, objData, sceneInfo.staticIssues);
            }

            sceneInfo.summary = CreateSceneSummary(sceneInfo.systems, sceneInfo.staticIssues);
            return JsonUtility.ToJson(sceneInfo, true);
        }

        private static void CollectAllObjects(GameObject obj, List<GameObject> allObjects)
        {
            allObjects.Add(obj);
            for (int i = 0; i < obj.transform.childCount; i++)
            {
                CollectAllObjects(obj.transform.GetChild(i).gameObject, allObjects);
            }
        }

        private static Dictionary<string, List<GameObject>> GroupObjectsBySystem(List<GameObject> allObjects)
        {
            var groups = new Dictionary<string, List<GameObject>>();

            foreach (var obj in allObjects)
            {
                string systemType = DetermineSystemType(obj);
                
                if (!groups.ContainsKey(systemType))
                {
                    groups[systemType] = new List<GameObject>();
                }
                groups[systemType].Add(obj);
            }

            return groups;
        }

        private static string DetermineSystemType(GameObject obj)
        {
            if (obj.CompareTag("Player") || obj.name.ToLower().Contains("player"))
                return "Player Systems";
            
            if (obj.CompareTag("Enemy") || obj.name.ToLower().Contains("enemy") || obj.name.ToLower().Contains("fish"))
                return "Enemy Systems";
                
            if (obj.CompareTag("FishingRod") || obj.CompareTag("DropTool") || obj.name.ToLower().Contains("tool"))
                return "Tool Systems";
                
            if (obj.CompareTag("WaterSurface") || obj.name.ToLower().Contains("water") || obj.name.ToLower().Contains("ocean"))
                return "Water Environment";
                
            if (obj.CompareTag("GameController") || obj.name.ToLower().Contains("manager") || obj.name.ToLower().Contains("controller"))
                return "Game Controllers";
                
            if (obj.CompareTag("MainCamera") || obj.name.ToLower().Contains("camera") || obj.name.ToLower().Contains("cinemachine"))
                return "Camera Systems";
                
            if (obj.layer == LayerMask.NameToLayer("UI") || obj.name.ToLower().Contains("ui") || obj.name.ToLower().Contains("canvas"))
                return "UI Systems";
                
            if (obj.name.ToLower().Contains("background") || obj.name.ToLower().Contains("island") || obj.name.ToLower().Contains("environment"))
                return "Environment";
                
            if (obj.name.ToLower().Contains("platform") || obj.name.ToLower().Contains("boat") || obj.name.ToLower().Contains("ship") || 
                obj.name.ToLower().Contains("puent") || obj.name.ToLower().Contains("fisherman") ||
                obj.layer == LayerMask.NameToLayer("Platform") || obj.layer == LayerMask.NameToLayer("BoatPlatform"))
                return "Platform & Fishermen";

            return "Other Objects";
        }

        private static GameSystemData CreateSystemData(string systemName, List<GameObject> objects)
        {
            var activeObjects = objects.Count(o => o.activeInHierarchy);
            var totalComponents = objects.Sum(o => o.GetComponents<Component>().Length);
            var hasIssues = objects.Any(o => HasObjectIssues(o));

            return new GameSystemData
            {
                name = systemName,
                icon = GetSystemIcon(systemName),
                color = GetSystemColor(systemName),
                objectCount = objects.Count,
                activeCount = activeObjects,
                componentCount = totalComponents,
                hasIssues = hasIssues,
                status = GetSystemStatus(systemName, objects),
                description = GetSystemDescription(systemName),
                criticalObjects = objects.Where(o => IsCriticalObject(o)).Select(o => o.name).ToList()
            };
        }

        private static GameObjectData CreateObjectData(GameObject obj)
        {
            var components = obj.GetComponents<Component>();
            var componentList = components.Where(c => c != null).Select(c => c.GetType().Name).ToList();

            return new GameObjectData
            {
                id = obj.GetInstanceID(),
                name = obj.name,
                tag = obj.tag,
                layer = LayerMask.LayerToName(obj.layer),
                isActive = obj.activeInHierarchy,
                position = obj.transform.position,
                parentId = obj.transform.parent ? obj.transform.parent.gameObject.GetInstanceID() : 0,
                childCount = obj.transform.childCount,
                components = componentList,
                systemType = DetermineSystemType(obj),
                icon = GetObjectIcon(obj),
                color = GetObjectColor(obj),
                priority = GetObjectPriority(obj),
                isPrefab = PrefabUtility.IsPartOfAnyPrefab(obj),
                prefabStatus = GetPrefabStatus(obj),
                hasMissingScripts = components.Any(c => c == null)
            };
        }

        private static void DetectStaticIssues(GameObject obj, GameObjectData objData, List<IssueData> issues)
        {
            var objectIssues = new List<IssueData>();

            // 🌉 PUENTE PLATFORM DETECTION - This is your key bug!
            if (obj.name.ToLower().Contains("puent") && obj.transform.childCount > 0)
            {
                objectIssues.Add(new IssueData
                {
                    objectId = obj.GetInstanceID(),
                    objectName = obj.name,
                    type = "warning",
                    category = "🌉 PUENTE PLATFORM WITH CHILDREN",
                    message = $"Platform '{obj.name}' has {obj.transform.childCount} children. If this gets destroyed during gameplay, all fishermen will disappear!",
                    severity = "high",
                    solution = "Check scripts that might call Destroy() on this platform. Look for collision detection, game managers, or cleanup scripts.",
                    autoFixAvailable = false
                });
            }

            // Parent-child destruction risk
            if (obj.transform.childCount >= 3 && !obj.name.ToLower().Contains("canvas") && !obj.name.ToLower().Contains("ui"))
            {
                objectIssues.Add(new IssueData
                {
                    objectId = obj.GetInstanceID(),
                    objectName = obj.name,
                    type = "info",
                    category = "Parent with Many Children",
                    message = $"'{obj.name}' has {obj.transform.childCount} children. If destroyed, all children will be lost.",
                    severity = "medium",
                    solution = "Monitor this object during gameplay to ensure it's not destroyed unexpectedly.",
                    autoFixAvailable = false
                });
            }

            // Missing component detection
            var allComponents = obj.GetComponents<Component>();
            if (allComponents.Any(c => c == null))
            {
                objectIssues.Add(new IssueData
                {
                    objectId = obj.GetInstanceID(),
                    objectName = obj.name,
                    type = "error",
                    category = "Missing Component",
                    message = "Object has missing component references (pink script icons)",
                    severity = "high",
                    solution = "Remove missing component slots or reassign the scripts",
                    autoFixAvailable = false
                });
            }

            // Fisherman without platform parent
            if (obj.name.ToLower().Contains("fisherman") && obj.transform.parent == null)
            {
                objectIssues.Add(new IssueData
                {
                    objectId = obj.GetInstanceID(),
                    objectName = obj.name,
                    type = "warning",
                    category = "Disconnected Fisherman",
                    message = "Fisherman is not child of any platform - may not be affected by platform destruction",
                    severity = "medium",
                    solution = "Ensure fisherman objects are children of their respective platforms",
                    autoFixAvailable = false
                });
            }

            issues.AddRange(objectIssues);
        }

        private static bool HasObjectIssues(GameObject obj)
        {
            var components = obj.GetComponents<Component>();
            return components.Any(c => c == null) || 
                   obj.name.StartsWith("GameObject") || 
                   obj.name.Contains("(Clone)") ||
                   components.Length > 8;
        }

        private static bool IsCriticalObject(GameObject obj)
        {
            return obj.CompareTag("Player") || 
                   obj.CompareTag("GameController") || 
                   obj.CompareTag("MainCamera") ||
                   obj.CompareTag("FishingRod") ||
                   obj.name.ToLower().Contains("manager") ||
                   obj.name.ToLower().Contains("puent") ||
                   (obj.transform.childCount > 2);
        }

        private static SceneSummary CreateSceneSummary(List<GameSystemData> systems, List<IssueData> staticIssues)
        {
            return new SceneSummary
            {
                totalObjects = systems.Sum(s => s.objectCount),
                totalSystems = systems.Count,
                activeObjects = systems.Sum(s => s.activeCount),
                totalStaticIssues = staticIssues.Count,
                totalRuntimeIssues = 0,
                criticalIssues = staticIssues.Count(i => i.severity == "high"),
                warningIssues = staticIssues.Count(i => i.severity == "medium"),
                infoIssues = staticIssues.Count(i => i.severity == "low"),
                healthScore = CalculateSceneHealth(systems, staticIssues),
                hasRuntimeDetection = false
            };
        }

        private static int CalculateSceneHealth(List<GameSystemData> systems, List<IssueData> staticIssues)
        {
            int baseScore = 100;
            baseScore -= staticIssues.Count(i => i.severity == "high") * 20;
            baseScore -= staticIssues.Count(i => i.severity == "medium") * 10;
            baseScore -= staticIssues.Count(i => i.severity == "low") * 5;
            return Mathf.Max(0, baseScore);
        }

        // Helper methods with emojis
        private static string GetSystemIcon(string systemName)
        {
            switch (systemName)
            {
                case "Player Systems": return "👤";
                case "Enemy Systems": return "🐟";
                case "Tool Systems": return "🎣";
                case "Water Environment": return "🌊";
                case "Game Controllers": return "🎮";
                case "Camera Systems": return "📷";
                case "UI Systems": return "📱";
                case "Environment": return "🏝️";
                case "Platform & Fishermen": return "⚓";
                default: return "📦";
            }
        }

        private static string GetSystemColor(string systemName)
        {
            switch (systemName)
            {
                case "Player Systems": return "#4CAF50";
                case "Enemy Systems": return "#F44336";
                case "Tool Systems": return "#FF9800";
                case "Water Environment": return "#00BCD4";
                case "Game Controllers": return "#2196F3";
                case "Camera Systems": return "#9C27B0";
                case "UI Systems": return "#673AB7";
                case "Environment": return "#8BC34A";
                case "Platform & Fishermen": return "#795548";
                default: return "#757575";
            }
        }

        private static string GetSystemStatus(string systemName, List<GameObject> objects)
        {
            bool hasIssues = objects.Any(o => HasObjectIssues(o));
            bool allActive = objects.All(o => o.activeInHierarchy);
            
            if (hasIssues) return "warning";
            if (!allActive) return "inactive";
            return "healthy";
        }

        private static string GetSystemDescription(string systemName)
        {
            switch (systemName)
            {
                case "Player Systems": return "Player character, controls, and player-related functionality";
                case "Enemy Systems": return "Fish enemies, AI behaviors, and enemy spawning";
                case "Tool Systems": return "Fishing rod, drop tool, and interactive equipment";
                case "Water Environment": return "Water surface, underwater areas, and water physics";
                case "Game Controllers": return "Game managers, state controllers, and core systems";
                case "Camera Systems": return "Camera controls, Cinemachine, and view management";
                case "UI Systems": return "User interface, menus, and HUD elements";
                case "Environment": return "Background objects, islands, and decorative elements";
                case "Platform & Fishermen": return "🌉 Boat platforms, puente bridges, and fishermen NPCs - CRITICAL FOR YOUR BUG!";
                default: return "Miscellaneous game objects";
            }
        }

        private static string GetObjectIcon(GameObject obj)
        {
            if (obj.CompareTag("Player")) return "👤";
            if (obj.CompareTag("Enemy")) return "🐟";
            if (obj.CompareTag("FishingRod")) return "🎣";
            if (obj.CompareTag("DropTool")) return "🔧";
            if (obj.CompareTag("WaterSurface")) return "🌊";
            if (obj.CompareTag("GameController")) return "🎮";
            if (obj.CompareTag("MainCamera")) return "📷";
            if (obj.name.ToLower().Contains("ui")) return "📱";
            if (obj.name.ToLower().Contains("boat") || obj.name.ToLower().Contains("ship")) return "⚓";
            if (obj.name.ToLower().Contains("puent")) return "🌉";
            if (obj.name.ToLower().Contains("fisherman")) return "🎣";
            if (obj.name.ToLower().Contains("background") || obj.name.ToLower().Contains("island")) return "🏝️";
            return "📦";
        }

        private static string GetObjectColor(GameObject obj)
        {
            if (!obj.activeInHierarchy) return "#666666";
            return GetSystemColor(DetermineSystemType(obj));
        }

        private static int GetObjectPriority(GameObject obj)
        {
            if (obj.CompareTag("Player")) return 10;
            if (obj.CompareTag("GameController")) return 9;
            if (obj.CompareTag("MainCamera")) return 8;
            if (obj.name.ToLower().Contains("puent")) return 7;
            if (obj.CompareTag("Enemy")) return 6;
            if (obj.CompareTag("FishingRod") || obj.CompareTag("DropTool")) return 5;
            if (obj.CompareTag("WaterSurface")) return 4;
            return 1;
        }

        private static string GetPrefabStatus(GameObject obj)
        {
            if (!PrefabUtility.IsPartOfAnyPrefab(obj)) return "not_prefab";
            
            var status = PrefabUtility.GetPrefabInstanceStatus(obj);
            switch (status)
            {
                case PrefabInstanceStatus.Connected: return "connected";
                case PrefabInstanceStatus.Disconnected: return "disconnected";
                case PrefabInstanceStatus.MissingAsset: return "missing";
                case PrefabInstanceStatus.NotAPrefab: return "not_prefab";
                default: return "unknown";
            }
        }

        private static string CreateVisualizerHTML(string sceneData)
        {
            string htmlContent = GetHTMLTemplate().Replace("{{SCENE_DATA}}", sceneData);
            string htmlPath = Path.Combine(Application.dataPath, "..", "Temp", "BugDetectorReport.html");
            File.WriteAllText(htmlPath, htmlContent);
            Debug.Log($"🚀 Bug Detector Report created: {htmlPath}");
            return htmlPath;
        }

        private static string GetHTMLTemplate()
        {
            return @"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>🌉 PuentT1 Bug Detector - RevengeFishing2D</title>
    <style>
        * { margin: 0; padding: 0; box-sizing: border-box; }
        body {
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            background: linear-gradient(135deg, #1e3c72 0%, #2a5298 100%);
            color: #ffffff;
            min-height: 100vh;
        }
        .header {
            background: rgba(0, 0, 0, 0.8);
            padding: 20px;
            text-align: center;
            box-shadow: 0 2px 20px rgba(0, 0, 0, 0.3);
        }
        .header h1 {
            font-size: 2.5em;
            margin-bottom: 10px;
            background: linear-gradient(45deg, #4CAF50, #2196F3);
            -webkit-background-clip: text;
            -webkit-text-fill-color: transparent;
            background-clip: text;
        }
        .scene-info {
            display: flex;
            justify-content: center;
            gap: 30px;
            margin-top: 15px;
        }
        .info-card {
            background: rgba(255, 255, 255, 0.1);
            padding: 10px 20px;
            border-radius: 25px;
            backdrop-filter: blur(10px);
        }
        .main-content {
            max-width: 1400px;
            margin: 0 auto;
            padding: 30px;
        }
        .puente-alert {
            background: rgba(255, 100, 100, 0.2);
            border: 3px solid #FF6B6B;
            border-radius: 15px;
            padding: 25px;
            margin-bottom: 30px;
            text-align: center;
        }
        .puente-title {
            color: #FF6B6B;
            font-size: 2em;
            font-weight: bold;
            margin-bottom: 15px;
        }
        .systems-grid {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(400px, 1fr));
            gap: 20px;
            margin-bottom: 40px;
        }
        .system-card {
            background: rgba(255, 255, 255, 0.95);
            color: #333;
            border-radius: 15px;
            padding: 25px;
            box-shadow: 0 8px 32px rgba(0, 0, 0, 0.3);
            transition: all 0.3s ease;
        }
        .system-card:hover {
            transform: translateY(-5px);
        }
        .issues-section {
            background: rgba(255, 255, 255, 0.1);
            border-radius: 15px;
            padding: 25px;
            margin-top: 30px;
        }
        .issue-item {
            background: rgba(255, 255, 255, 0.9);
            color: #333;
            padding: 15px;
            margin: 10px 0;
            border-radius: 8px;
            border-left: 5px solid var(--issue-color);
        }
        .issue-high { --issue-color: #F44336; }
        .issue-medium { --issue-color: #FF9800; }
        .issue-low { --issue-color: #2196F3; }
    </style>
</head>
<body>
    <div class=""header"">
        <h1 id=""sceneTitle"">🌉 PuentT1 Bug Detector</h1>
        <div class=""scene-info"">
            <div class=""info-card"">
                <div style=""font-size: 1.8em; font-weight: bold;"" id=""healthScore"">100%</div>
                <div>Scene Health</div>
            </div>
            <div class=""info-card"">
                <div style=""font-size: 1.5em;"" id=""totalObjects"">0</div>
                <div>Total Objects</div>
            </div>
            <div class=""info-card"">
                <div style=""font-size: 1.5em;"" id=""issueCount"">0</div>
                <div>🚨 Issues Found</div>
            </div>
        </div>
    </div>

    <div class=""main-content"">
        <div class=""puente-alert"">
            <div class=""puente-title"">🌉 PUENTE PLATFORM ANALYSIS</div>
            <div id=""puenteAnalysis"">Analyzing your scene for platform destruction risks...</div>
        </div>

        <div class=""systems-grid"" id=""systemsGrid""></div>
        
        <div class=""issues-section"">
            <h2 style=""color: #FF6B6B; margin-bottom: 20px;"">🚨 Detected Issues</h2>
            <div id=""issuesContainer""></div>
        </div>
    </div>

    <script>
        const sceneData = {{SCENE_DATA}};

        function init() {
            document.getElementById('sceneTitle').textContent = '🌉 ' + sceneData.sceneName + ' - Bug Analysis Report';
            updateSummary();
            renderSystems();
            renderIssues();
            analyzePuentePlatforms();
        }

        function updateSummary() {
            const summary = sceneData.summary;
            document.getElementById('totalObjects').textContent = summary.totalObjects;
            document.getElementById('healthScore').textContent = summary.healthScore + '%';
            document.getElementById('issueCount').textContent = summary.totalStaticIssues;
        }

        function renderSystems() {
            const container = document.getElementById('systemsGrid');
            container.innerHTML = '';

            sceneData.systems.forEach(system => {
                const systemCard = document.createElement('div');
                systemCard.className = 'system-card';
                
                systemCard.innerHTML = `
                    <div style=""display: flex; align-items: center; margin-bottom: 15px;"">
                        <div style=""font-size: 2.5em; margin-right: 15px;"">${system.icon}</div>
                        <div style=""flex: 1;"">
                            <div style=""font-size: 1.4em; font-weight: bold; margin-bottom: 5px;"">${system.name}</div>
                            <div style=""color: #666; font-size: 0.9em;"">${system.description}</div>
                        </div>
                    </div>
                    <div style=""display: grid; grid-template-columns: repeat(3, 1fr); gap: 15px;"">
                        <div style=""text-align: center; padding: 15px; background: rgba(0, 0, 0, 0.05); border-radius: 10px;"">
                            <div style=""font-size: 1.8em; font-weight: bold; color: ${system.color};"">${system.objectCount}</div>
                            <div style=""font-size: 0.8em; color: #666; margin-top: 5px;"">Objects</div>
                        </div>
                        <div style=""text-align: center; padding: 15px; background: rgba(0, 0, 0, 0.05); border-radius: 10px;"">
                            <div style=""font-size: 1.8em; font-weight: bold; color: ${system.color};"">${system.activeCount}</div>
                            <div style=""font-size: 0.8em; color: #666; margin-top: 5px;"">Active</div>
                        </div>
                        <div style=""text-align: center; padding: 15px; background: rgba(0, 0, 0, 0.05); border-radius: 10px;"">
                            <div style=""font-size: 1.8em; font-weight: bold; color: ${system.color};"">${system.componentCount}</div>
                            <div style=""font-size: 0.8em; color: #666; margin-top: 5px;"">Components</div>
                        </div>
                    </div>
                `;

                container.appendChild(systemCard);
            });
        }

        function renderIssues() {
            const container = document.getElementById('issuesContainer');
            container.innerHTML = '';
            
            if (!sceneData.staticIssues || sceneData.staticIssues.length === 0) {
                container.innerHTML = '<p style=""color: #4CAF50; font-style: italic; text-align: center;"">✅ No issues detected!</p>';
                return;
            }

            sceneData.staticIssues.forEach(issue => {
                const issueDiv = document.createElement('div');
                issueDiv.className = `issue-item issue-${issue.severity}`;
                
                issueDiv.innerHTML = `
                    <div style=""display: flex; justify-content: space-between; align-items: center; margin-bottom: 10px;"">
                        <strong>${issue.objectName}</strong>
                        <span style=""background: var(--issue-color); color: white; padding: 3px 8px; border-radius: 12px; font-size: 0.7em;"">${issue.severity.toUpperCase()}</span>
                    </div>
                    <div style=""margin-bottom: 8px; color: #333; font-weight: bold;"">${issue.category}</div>
                    <div style=""margin-bottom: 8px; color: #666;"">${issue.message}</div>
                    <div style=""color: #4CAF50; font-style: italic; font-size: 0.9em;"">💡 ${issue.solution}</div>
                `;
                
                container.appendChild(issueDiv);
            });
        }

        function analyzePuentePlatforms() {
            const puenteObjects = sceneData.objects.filter(obj => obj.name.toLowerCase().includes('puent'));
            const container = document.getElementById('puenteAnalysis');
            
            if (puenteObjects.length === 0) {
                container.innerHTML = '✅ No puente platforms detected in scene.';
                return;
            }

            let analysis = `Found ${puenteObjects.length} puente platform(s):<br><br>`;
            
            puenteObjects.forEach(platform => {
                analysis += `<div style=""background: rgba(255,255,255,0.1); padding: 10px; margin: 5px 0; border-radius: 5px;"">
                    🌉 <strong>${platform.name}</strong><br>
                    👥 Children: ${platform.childCount}<br>
                    📍 Layer: ${platform.layer}<br>
                    ${platform.childCount > 0 ? 
                        '<span style=""color: #FFD93D;"">⚠️ If this platform gets destroyed, all ' + platform.childCount + ' children will disappear!</span>' : 
                        '<span style=""color: #4CAF50;"">✅ No children - safe to destroy</span>'
                    }
                </div>`;
            });

            container.innerHTML = analysis;
        }

        window.addEventListener('load', init);
    </script>
</body>
</html>";
        }

        // Data classes
        [System.Serializable]
        public class SceneInfo
        {
            public string sceneName;
            public List<GameSystemData> systems = new List<GameSystemData>();
            public List<GameObjectData> objects = new List<GameObjectData>();
            public List<IssueData> staticIssues = new List<IssueData>();
            public SceneSummary summary = new SceneSummary();
        }

        [System.Serializable]
        public class GameSystemData
        {
            public string name;
            public string icon;
            public string color;
            public int objectCount;
            public int activeCount;
            public int componentCount;
            public bool hasIssues;
            public string status;
            public string description;
            public List<string> criticalObjects = new List<string>();
        }

        [System.Serializable]
        public class GameObjectData
        {
            public int id;
            public string name;
            public string tag;
            public string layer;
            public bool isActive;
            public Vector3 position;
            public int parentId;
            public int childCount;
            public List<string> components = new List<string>();
            public string systemType;
            public string icon;
            public string color;
            public int priority;
            public bool isPrefab;
            public string prefabStatus;
            public bool hasMissingScripts;
        }

        [System.Serializable]
        public class IssueData
        {
            public int objectId;
            public string objectName;
            public string type;
            public string category;
            public string message;
            public string severity;
            public string solution;
            public bool autoFixAvailable;
        }

        [System.Serializable]
        public class SceneSummary
        {
            public int totalObjects;
            public int totalSystems;
            public int activeObjects;
            public int totalStaticIssues;
            public int totalRuntimeIssues;
            public int criticalIssues;
            public int warningIssues;
            public int infoIssues;
            public int healthScore;
            public bool hasRuntimeDetection;
        }
    }
}
