#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Security.Cryptography;
using System;

public class DuplicateAssetFinder : EditorWindow
{
    [System.Serializable]
    public class DuplicateGroup
    {
        public string name;
        public List<AssetInfo> assets = new List<AssetInfo>();
        public DuplicateType duplicateType;
        public bool isExpanded = false;
        public bool isSelected = false;
    }

    [System.Serializable]
    public class AssetInfo
    {
        public string path;
        public string name;
        public string type;
        public long fileSize;
        public string hash;
        public bool isSelected = false;
        
        public AssetInfo(string assetPath)
        {
            path = assetPath;
            name = Path.GetFileNameWithoutExtension(assetPath);
            type = AssetDatabase.GetMainAssetTypeAtPath(assetPath)?.Name ?? "Unknown";
            
            string fullPath = Application.dataPath.Replace("Assets", "") + assetPath;
            if (File.Exists(fullPath))
            {
                fileSize = new FileInfo(fullPath).Length;
                hash = CalculateFileHash(fullPath);
            }
        }
        
        private string CalculateFileHash(string filePath)
        {
            try
            {
                using (var md5 = MD5.Create())
                {
                    using (var stream = File.OpenRead(filePath))
                    {
                        var hashBytes = md5.ComputeHash(stream);
                        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
                    }
                }
            }
            catch
            {
                return "hash_error";
            }
        }
    }

    public enum DuplicateType
    {
        ByName,
        ByContent,
        BySize,
        SpritesOnly
    }

    public enum ScanScope
    {
        EntireProject,
        SelectedFolder,
        MaterialsOnly,
        SpritesOnly,
        ScriptsOnly,
        AudioOnly
    }

    // GUI State
    private List<DuplicateGroup> duplicateGroups = new List<DuplicateGroup>();
    private Vector2 scrollPosition;
    private DuplicateType scanType = DuplicateType.ByName;
    private ScanScope scanScope = ScanScope.EntireProject;
    private string folderPath = "Assets";
    private bool includePackages = false;
    private long minFileSize = 0;
    private bool showOnlySprites = false;
    private bool autoExpandGroups = true;
    
    // Stats
    private int totalDuplicates = 0;
    private long totalWastedSpace = 0;
    private bool isScanning = false;

    [MenuItem("Tools/🔍 Duplicate Asset Finder")]
    public static void ShowWindow()
    {
        var window = GetWindow<DuplicateAssetFinder>("Duplicate Asset Finder");
        window.minSize = new Vector2(600, 400);
        window.Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        DrawHeader();
        DrawScanOptions();
        DrawScanButton();
        
        if (duplicateGroups.Count > 0)
        {
            DrawStats();
            DrawActionButtons();
            DrawDuplicatesList();
        }
        
        EditorGUILayout.EndVertical();
    }

    private void DrawHeader()
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("🔍 DUPLICATE ASSET FINDER", EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();
        
        if (GUILayout.Button("📋 Clear Console", GUILayout.Width(120)))
        {
            ClearConsole();
        }
        
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();
    }

    private void DrawScanOptions()
    {
        EditorGUILayout.LabelField("📁 SCAN CONFIGURATION", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        scanType = (DuplicateType)EditorGUILayout.EnumPopup("🔍 Detection Method:", scanType);
        scanScope = (ScanScope)EditorGUILayout.EnumPopup("📂 Scan Scope:", scanScope);
        
        if (scanScope == ScanScope.SelectedFolder)
        {
            EditorGUILayout.BeginHorizontal();
            folderPath = EditorGUILayout.TextField("Folder Path:", folderPath);
            if (GUILayout.Button("📁 Browse", GUILayout.Width(70)))
            {
                string selectedPath = EditorUtility.OpenFolderPanel("Select Folder", "Assets", "");
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    if (selectedPath.Contains(Application.dataPath))
                    {
                        folderPath = "Assets" + selectedPath.Substring(Application.dataPath.Length);
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
        }
        
        includePackages = EditorGUILayout.Toggle("Include Packages:", includePackages);
        minFileSize = EditorGUILayout.LongField("Min File Size (bytes):", minFileSize);
        autoExpandGroups = EditorGUILayout.Toggle("Auto Expand Groups:", autoExpandGroups);
        
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space();
    }

    private void DrawScanButton()
    {
        EditorGUILayout.BeginHorizontal();
        
        GUI.enabled = !isScanning;
        
        Color originalColor = GUI.backgroundColor;
        GUI.backgroundColor = Color.green;
        
        if (GUILayout.Button(isScanning ? "🔄 Scanning..." : "🚀 START SCAN", GUILayout.Height(35)))
        {
            StartScan();
        }
        
        GUI.backgroundColor = originalColor;
        GUI.enabled = true;
        
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();
    }

    private void DrawStats()
    {
        EditorGUILayout.LabelField("📊 SCAN RESULTS", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField($"🔢 Duplicate Groups Found: {duplicateGroups.Count}");
        EditorGUILayout.LabelField($"📁 Total Duplicate Assets: {totalDuplicates}");
        EditorGUILayout.LabelField($"💾 Estimated Wasted Space: {FormatFileSize(totalWastedSpace)}");
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space();
    }

    private void DrawActionButtons()
    {
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("✅ Select All Groups", GUILayout.Height(25)))
        {
            foreach (var group in duplicateGroups)
            {
                group.isSelected = true;
                for (int i = 1; i < group.assets.Count; i++) // Keep first, select rest
                {
                    group.assets[i].isSelected = true;
                }
            }
        }
        
        if (GUILayout.Button("❌ Deselect All", GUILayout.Height(25)))
        {
            foreach (var group in duplicateGroups)
            {
                group.isSelected = false;
                foreach (var asset in group.assets)
                {
                    asset.isSelected = false;
                }
            }
        }
        
        if (GUILayout.Button("📄 Generate Report", GUILayout.Height(25)))
        {
            GenerateReport();
        }
        
        GUI.backgroundColor = Color.red;
        if (GUILayout.Button("🗑️ DELETE SELECTED", GUILayout.Height(25)))
        {
            DeleteSelectedAssets();
        }
        GUI.backgroundColor = Color.white;
        
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();
    }

    private void DrawDuplicatesList()
    {
        EditorGUILayout.LabelField("🗂️ DUPLICATE GROUPS", EditorStyles.boldLabel);
        
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, EditorStyles.helpBox);
        
        foreach (var group in duplicateGroups)
        {
            DrawDuplicateGroup(group);
        }
        
        EditorGUILayout.EndScrollView();
    }

    private void DrawDuplicateGroup(DuplicateGroup group)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        // Group Header
        EditorGUILayout.BeginHorizontal();
        
        group.isExpanded = EditorGUILayout.Foldout(group.isExpanded, $"📦 {group.name} ({group.assets.Count} copies)", true);
        
        GUILayout.FlexibleSpace();
        
        string typeIcon = GetTypeIcon(group.duplicateType);
        EditorGUILayout.LabelField($"{typeIcon} {group.duplicateType}", GUILayout.Width(100));
        
        group.isSelected = EditorGUILayout.Toggle(group.isSelected, GUILayout.Width(20));
        
        EditorGUILayout.EndHorizontal();
        
        // Group Content
        if (group.isExpanded)
        {
            EditorGUI.indentLevel++;
            
            for (int i = 0; i < group.assets.Count; i++)
            {
                DrawAssetInfo(group.assets[i], i == 0); // First is original
            }
            
            EditorGUI.indentLevel--;
        }
        
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(2);
    }

    private void DrawAssetInfo(AssetInfo asset, bool isOriginal)
    {
        EditorGUILayout.BeginHorizontal();
        
        // Selection checkbox (disabled for original)
        GUI.enabled = !isOriginal;
        asset.isSelected = EditorGUILayout.Toggle(asset.isSelected, GUILayout.Width(20));
        GUI.enabled = true;
        
        // Asset icon
        string icon = isOriginal ? "🟢" : "🔴";
        EditorGUILayout.LabelField(icon, GUILayout.Width(25));
        
        // Asset name and type
        EditorGUILayout.LabelField($"{asset.name} ({asset.type})", GUILayout.Width(200));
        
        // File size
        EditorGUILayout.LabelField(FormatFileSize(asset.fileSize), GUILayout.Width(80));
        
        // Asset path
        EditorGUILayout.LabelField(asset.path, EditorStyles.miniLabel);
        
        // Navigate button
        if (GUILayout.Button("🎯", GUILayout.Width(30)))
        {
            PingAsset(asset.path);
        }
        
        EditorGUILayout.EndHorizontal();
    }

    private void StartScan()
    {
        isScanning = true;
        duplicateGroups.Clear();
        totalDuplicates = 0;
        totalWastedSpace = 0;
        
        EditorApplication.update += UpdateScan;
    }

    private void UpdateScan()
    {
        try
        {
            PerformScan();
        }
        finally
        {
            isScanning = false;
            EditorApplication.update -= UpdateScan;
            Repaint();
        }
    }

    private void PerformScan()
    {
        // Get search filter
        string searchFilter = GetSearchFilter();
        string[] searchPaths = GetSearchPaths();
        
        // Find all assets
        string[] guids = AssetDatabase.FindAssets(searchFilter, searchPaths);
        var assets = new List<AssetInfo>();
        
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            
            if (ShouldIncludeAsset(path))
            {
                assets.Add(new AssetInfo(path));
            }
            
            // Update progress
            if (i % 50 == 0)
            {
                EditorUtility.DisplayProgressBar("Scanning Assets", $"Processing {i + 1}/{guids.Length}", (float)i / guids.Length);
            }
        }
        
        EditorUtility.ClearProgressBar();
        
        // Group duplicates
        GroupDuplicates(assets);
        
        // Calculate stats
        CalculateStats();
        
        Debug.Log($"🔍 Duplicate Asset Scan Complete! Found {duplicateGroups.Count} groups with {totalDuplicates} duplicates.");
    }

    private void GroupDuplicates(List<AssetInfo> assets)
    {
        Dictionary<string, List<AssetInfo>> groups = new Dictionary<string, List<AssetInfo>>();
        
        foreach (var asset in assets)
        {
            string key = GetGroupingKey(asset);
            
            if (!groups.ContainsKey(key))
                groups[key] = new List<AssetInfo>();
            
            groups[key].Add(asset);
        }
        
        // Create duplicate groups (only groups with more than 1 asset)
        foreach (var kvp in groups.Where(g => g.Value.Count > 1))
        {
            var group = new DuplicateGroup
            {
                name = kvp.Key,
                assets = kvp.Value.OrderBy(a => a.path).ToList(),
                duplicateType = scanType,
                isExpanded = autoExpandGroups
            };
            
            duplicateGroups.Add(group);
        }
        
        duplicateGroups = duplicateGroups.OrderByDescending(g => g.assets.Count).ToList();
    }

    private string GetGroupingKey(AssetInfo asset)
    {
        switch (scanType)
        {
            case DuplicateType.ByName:
                return asset.name.ToLower();
            case DuplicateType.ByContent:
                return asset.hash;
            case DuplicateType.BySize:
                return $"{asset.type}_{asset.fileSize}";
            case DuplicateType.SpritesOnly:
                return asset.type == "Sprite" ? asset.name.ToLower() : Guid.NewGuid().ToString();
            default:
                return asset.name.ToLower();
        }
    }

    private string GetSearchFilter()
    {
        switch (scanScope)
        {
            case ScanScope.MaterialsOnly:
                return "t:Material";
            case ScanScope.SpritesOnly:
                return "t:Sprite";
            case ScanScope.ScriptsOnly:
                return "t:MonoScript";
            case ScanScope.AudioOnly:
                return "t:AudioClip";
            default:
                return "";
        }
    }

    private string[] GetSearchPaths()
    {
        var paths = new List<string>();
        
        switch (scanScope)
        {
            case ScanScope.SelectedFolder:
                paths.Add(folderPath);
                break;
            default:
                paths.Add("Assets");
                break;
        }
        
        if (includePackages)
        {
            paths.Add("Packages");
        }
        
        return paths.ToArray();
    }

    private bool ShouldIncludeAsset(string path)
    {
        if (string.IsNullOrEmpty(path))
            return false;
        
        // Skip directories
        if (AssetDatabase.IsValidFolder(path))
            return false;
        
        // Skip packages if not included
        if (!includePackages && path.StartsWith("Packages/"))
            return false;
        
        // Check file size
        if (minFileSize > 0)
        {
            string fullPath = Application.dataPath.Replace("Assets", "") + path;
            if (File.Exists(fullPath))
            {
                var fileInfo = new FileInfo(fullPath);
                if (fileInfo.Length < minFileSize)
                    return false;
            }
        }
        
        return true;
    }

    private void CalculateStats()
    {
        totalDuplicates = 0;
        totalWastedSpace = 0;
        
        foreach (var group in duplicateGroups)
        {
            totalDuplicates += group.assets.Count - 1; // Exclude original
            
            for (int i = 1; i < group.assets.Count; i++)
            {
                totalWastedSpace += group.assets[i].fileSize;
            }
        }
    }

    private void DeleteSelectedAssets()
    {
        var assetsToDelete = new List<string>();
        
        foreach (var group in duplicateGroups)
        {
            foreach (var asset in group.assets)
            {
                if (asset.isSelected)
                {
                    assetsToDelete.Add(asset.path);
                }
            }
        }
        
        if (assetsToDelete.Count == 0)
        {
            EditorUtility.DisplayDialog("No Selection", "No assets selected for deletion.", "OK");
            return;
        }
        
        if (EditorUtility.DisplayDialog("Confirm Deletion", 
            $"Are you sure you want to delete {assetsToDelete.Count} assets?\n\nThis action cannot be undone!", 
            "Delete", "Cancel"))
        {
            foreach (string path in assetsToDelete)
            {
                AssetDatabase.DeleteAsset(path);
            }
            
            AssetDatabase.Refresh();
            
            Debug.Log($"🗑️ Deleted {assetsToDelete.Count} duplicate assets.");
            
            // Refresh scan
            StartScan();
        }
    }

    private void GenerateReport()
    {
        string reportPath = EditorUtility.SaveFilePanel("Save Duplicate Assets Report", "", "DuplicateAssetsReport", "txt");
        
        if (string.IsNullOrEmpty(reportPath))
            return;
        
        var report = new System.Text.StringBuilder();
        report.AppendLine("📋 DUPLICATE ASSETS REPORT");
        report.AppendLine($"Generated: {System.DateTime.Now}");
        report.AppendLine($"Scan Type: {scanType}");
        report.AppendLine($"Scan Scope: {scanScope}");
        report.AppendLine($"Total Groups: {duplicateGroups.Count}");
        report.AppendLine($"Total Duplicates: {totalDuplicates}");
        report.AppendLine($"Wasted Space: {FormatFileSize(totalWastedSpace)}");
        report.AppendLine();
        
        foreach (var group in duplicateGroups)
        {
            report.AppendLine($"GROUP: {group.name} ({group.assets.Count} copies)");
            foreach (var asset in group.assets)
            {
                report.AppendLine($"  - {asset.path} ({FormatFileSize(asset.fileSize)})");
            }
            report.AppendLine();
        }
        
        File.WriteAllText(reportPath, report.ToString());
        
        EditorUtility.DisplayDialog("Report Generated", $"Report saved to:\n{reportPath}", "OK");
    }

    private void PingAsset(string path)
    {
        var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
        if (asset != null)
        {
            EditorGUIUtility.PingObject(asset);
            Selection.activeObject = asset;
        }
    }

    private void ClearConsole()
    {
        var assembly = System.Reflection.Assembly.GetAssembly(typeof(SceneView));
        var type = assembly.GetType("UnityEditor.LogEntries");
        var method = type.GetMethod("Clear");
        method.Invoke(new object(), null);
    }

    private string GetTypeIcon(DuplicateType type)
    {
        switch (type)
        {
            case DuplicateType.ByName: return "🏷️";
            case DuplicateType.ByContent: return "🔗";
            case DuplicateType.BySize: return "📏";
            case DuplicateType.SpritesOnly: return "🖼️";
            default: return "❓";
        }
    }

    private string FormatFileSize(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024:F1} KB";
        if (bytes < 1024 * 1024 * 1024) return $"{bytes / (1024 * 1024):F1} MB";
        return $"{bytes / (1024 * 1024 * 1024):F1} GB";
    }
}
#endif
