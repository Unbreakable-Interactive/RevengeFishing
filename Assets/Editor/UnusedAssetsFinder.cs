using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.IO;

#if UNITY_EDITOR
public class UnusedAssetsFinder : EditorWindow
{
    [Header("Scan Settings")]
    public bool includeTextures = true;
    public bool includeAudio = true;
    public bool includePrefabs = true;
    public bool includeScripts = true;
    public bool includeMaterials = true;
    public bool includeAnimations = true;
    
    [Header("Exclusions")]
    public string[] excludeFolders = { "Editor", "StreamingAssets" };
    public string[] excludeExtensions = { ".cs", ".dll" };
    
    private Vector2 scrollPosition;
    private List<string> unusedAssets = new List<string>();
    private List<string> allAssets = new List<string>();
    private bool isScanning = false;
    private float scanProgress = 0f;
    private int totalAssets = 0;
    private int scannedAssets = 0;
    
    [MenuItem("Tools/Debug/Unused Assets Finder")]
    public static void ShowWindow()
    {
        GetWindow<UnusedAssetsFinder>("Unused Assets Finder");
    }
    
    private void OnGUI()
    {
        GUILayout.Label("Unused Assets Finder", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        // Settings section
        EditorGUILayout.LabelField("Asset Types to Scan:", EditorStyles.boldLabel);
        includeTextures = EditorGUILayout.Toggle("Textures", includeTextures);
        includeAudio = EditorGUILayout.Toggle("Audio Clips", includeAudio);
        includePrefabs = EditorGUILayout.Toggle("Prefabs", includePrefabs);
        includeScripts = EditorGUILayout.Toggle("Scripts", includeScripts);
        includeMaterials = EditorGUILayout.Toggle("Materials", includeMaterials);
        includeAnimations = EditorGUILayout.Toggle("Animations", includeAnimations);
        
        GUILayout.Space(10);
        
        // Scan button
        GUI.enabled = !isScanning;
        if (GUILayout.Button("Scan for Unused Assets", GUILayout.Height(30)))
        {
            ScanForUnusedAssets();
        }
        GUI.enabled = true;
        
        // Progress bar
        if (isScanning)
        {
            EditorGUI.ProgressBar(GUILayoutUtility.GetRect(0, 20), scanProgress, 
                $"Scanning... {scannedAssets}/{totalAssets}");
        }
        
        GUILayout.Space(10);
        
        // Results section
        if (unusedAssets.Count > 0)
        {
            EditorGUILayout.LabelField($"Found {unusedAssets.Count} unused assets:", EditorStyles.boldLabel);
            
            // Action buttons
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Select All in Project"))
            {
                SelectUnusedAssetsInProject();
            }
            if (GUILayout.Button("Export List to File"))
            {
                ExportUnusedAssetsList();
            }
            if (GUILayout.Button("Clear Results"))
            {
                unusedAssets.Clear();
            }
            GUILayout.EndHorizontal();
            
            GUILayout.Space(5);
            
            // Scrollable list of unused assets
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            for (int i = 0; i < unusedAssets.Count; i++)
            {
                GUILayout.BeginHorizontal();
                
                // Asset path
                EditorGUILayout.LabelField(unusedAssets[i], GUILayout.ExpandWidth(true));
                
                // Ping button
                if (GUILayout.Button("Ping", GUILayout.Width(50)))
                {
                    Object asset = AssetDatabase.LoadAssetAtPath<Object>(unusedAssets[i]);
                    if (asset != null)
                    {
                        EditorGUIUtility.PingObject(asset);
                    }
                }
                
                // Delete button
                GUI.backgroundColor = Color.red;
                if (GUILayout.Button("X", GUILayout.Width(25)))
                {
                    if (EditorUtility.DisplayDialog("Delete Asset", 
                        $"Are you sure you want to delete:\n{unusedAssets[i]}", 
                        "Delete", "Cancel"))
                    {
                        AssetDatabase.DeleteAsset(unusedAssets[i]);
                        unusedAssets.RemoveAt(i);
                        i--; // Adjust index after removal
                        AssetDatabase.Refresh();
                    }
                }
                GUI.backgroundColor = Color.white;
                
                GUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndScrollView();
        }
        else if (!isScanning && unusedAssets.Count == 0 && allAssets.Count > 0)
        {
            EditorGUILayout.LabelField("No unused assets found! ✅", EditorStyles.boldLabel);
        }
    }
    
    private void ScanForUnusedAssets()
    {
        isScanning = true;
        scanProgress = 0f;
        unusedAssets.Clear();
        allAssets.Clear();
        
        EditorApplication.update += UpdateScan;
        
        // Get all assets in project
        string[] allAssetGuids = AssetDatabase.FindAssets("", new[] { "Assets" });
        allAssets = allAssetGuids.Select(AssetDatabase.GUIDToAssetPath).ToList();
        
        // Filter assets based on settings
        allAssets = FilterAssetsByType(allAssets);
        
        totalAssets = allAssets.Count;
        scannedAssets = 0;
        
        Debug.Log($"Starting scan of {totalAssets} assets...");
    }
    
    private void UpdateScan()
    {
        if (!isScanning) return;
        
        int assetsToProcessThisFrame = Mathf.Min(10, allAssets.Count - scannedAssets);
        
        for (int i = 0; i < assetsToProcessThisFrame; i++)
        {
            string assetPath = allAssets[scannedAssets + i];
            
            if (IsAssetUnused(assetPath))
            {
                unusedAssets.Add(assetPath);
            }
        }
        
        scannedAssets += assetsToProcessThisFrame;
        scanProgress = (float)scannedAssets / totalAssets;
        
        if (scannedAssets >= totalAssets)
        {
            // Scanning complete
            isScanning = false;
            EditorApplication.update -= UpdateScan;
            
            Debug.Log($"Scan complete! Found {unusedAssets.Count} unused assets out of {totalAssets} total.");
            Repaint();
        }
    }
    
    private List<string> FilterAssetsByType(List<string> assets)
    {
        List<string> filtered = new List<string>();
        
        foreach (string asset in assets)
        {
            // Skip excluded folders
            bool inExcludedFolder = false;
            foreach (string excludeFolder in excludeFolders)
            {
                if (asset.Contains("/" + excludeFolder + "/"))
                {
                    inExcludedFolder = true;
                    break;
                }
            }
            if (inExcludedFolder) continue;
            
            // Skip excluded extensions
            string extension = Path.GetExtension(asset).ToLower();
            if (excludeExtensions.Contains(extension)) continue;
            
            // Check if asset type should be included
            if (includeTextures && IsTextureAsset(asset)) filtered.Add(asset);
            else if (includeAudio && IsAudioAsset(asset)) filtered.Add(asset);
            else if (includePrefabs && asset.EndsWith(".prefab")) filtered.Add(asset);
            else if (includeScripts && asset.EndsWith(".cs")) filtered.Add(asset);
            else if (includeMaterials && asset.EndsWith(".mat")) filtered.Add(asset);
            else if (includeAnimations && IsAnimationAsset(asset)) filtered.Add(asset);
        }
        
        return filtered;
    }
    
    private bool IsTextureAsset(string path)
    {
        string ext = Path.GetExtension(path).ToLower();
        return ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".tga" || ext == ".psd";
    }
    
    private bool IsAudioAsset(string path)
    {
        string ext = Path.GetExtension(path).ToLower();
        return ext == ".wav" || ext == ".mp3" || ext == ".ogg" || ext == ".aiff";
    }
    
    private bool IsAnimationAsset(string path)
    {
        string ext = Path.GetExtension(path).ToLower();
        return ext == ".anim" || ext == ".controller";
    }
    
    private bool IsAssetUnused(string assetPath)
    {
        // Get all dependencies that reference this asset
        string[] dependencies = AssetDatabase.GetDependencies(new[] { assetPath }, false);
        
        // If the asset is only referenced by itself, it's unused
        if (dependencies.Length <= 1)
        {
            // Check if asset is referenced in any scene
            string[] sceneGuids = AssetDatabase.FindAssets("t:Scene");
            foreach (string sceneGuid in sceneGuids)
            {
                string scenePath = AssetDatabase.GUIDToAssetPath(sceneGuid);
                string[] sceneDependencies = AssetDatabase.GetDependencies(scenePath, true);
                
                if (sceneDependencies.Contains(assetPath))
                {
                    return false; // Asset is used in a scene
                }
            }
            
            // Check if asset is referenced by any prefab
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
            foreach (string prefabGuid in prefabGuids)
            {
                string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuid);
                string[] prefabDependencies = AssetDatabase.GetDependencies(prefabPath, true);
                
                if (prefabDependencies.Contains(assetPath))
                {
                    return false; // Asset is used by a prefab
                }
            }
            
            return true; // Asset appears to be unused
        }
        
        return false;
    }
    
    private void SelectUnusedAssetsInProject()
    {
        List<Object> objectsToSelect = new List<Object>();
        
        foreach (string assetPath in unusedAssets)
        {
            Object asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
            if (asset != null)
            {
                objectsToSelect.Add(asset);
            }
        }
        
        Selection.objects = objectsToSelect.ToArray();
        EditorGUIUtility.PingObject(objectsToSelect.FirstOrDefault());
    }
    
    private void ExportUnusedAssetsList()
    {
        string filePath = EditorUtility.SaveFilePanel("Export Unused Assets List", 
            Application.dataPath, "UnusedAssets", "txt");
        
        if (!string.IsNullOrEmpty(filePath))
        {
            string content = "Unused Assets Found:\n\n";
            content += string.Join("\n", unusedAssets);
            content += $"\n\nTotal: {unusedAssets.Count} assets";
            content += $"\nScanned: {System.DateTime.Now}";
            
            File.WriteAllText(filePath, content);
            EditorUtility.DisplayDialog("Export Complete", 
                $"Unused assets list exported to:\n{filePath}", "OK");
        }
    }
}
#endif