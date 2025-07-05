using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

public class TextureOptimizerTool : EditorWindow
{
    [System.Serializable]
    public class PlatformSettings
    {
        public int maxSize = 2048;
        public TextureImporterFormat format = TextureImporterFormat.Automatic;
        public TextureImporterCompression compression = TextureImporterCompression.Compressed;
        public bool useCrunchCompression = false;
        public int compressionQuality = 50;
        public bool overrideETC2Fallback = false;
        public AndroidETC2FallbackOverride etc2FallbackOverride = AndroidETC2FallbackOverride.Quality32Bit;
        public TextureResizeAlgorithm resizeAlgorithm = TextureResizeAlgorithm.Mitchell;
    }

    [System.Serializable]
    public class OptimizationProfile
    {
        public string name = "Default";
        public PlatformSettings pcSettings = new PlatformSettings();
        public PlatformSettings androidSettings = new PlatformSettings();
        public PlatformSettings iosSettings = new PlatformSettings();
        public PlatformSettings webglSettings = new PlatformSettings();
    }

    private List<string> targetPaths = new List<string>();
    private OptimizationProfile currentProfile = new OptimizationProfile();
    private Vector2 scrollPosition;
    private bool showAdvancedSettings = false;
    private bool showPCSettings = true;
    private bool showAndroidSettings = true;
    private bool showIOSSettings = true;
    private bool showWebGLSettings = true;

    [MenuItem("Tools/Texture Optimizer")]
    public static void ShowWindow()
    {
        GetWindow<TextureOptimizerTool>("Texture Optimizer");
    }

    private void OnEnable()
    {
        SetOptimalDefaults();
    }

    private void SetOptimalDefaults()
    {
        // PC (Standalone) - High quality, larger sizes
        currentProfile.pcSettings.maxSize = 2048;
        currentProfile.pcSettings.format = TextureImporterFormat.Automatic;
        currentProfile.pcSettings.compression = TextureImporterCompression.Compressed;
        currentProfile.pcSettings.useCrunchCompression = false;
        currentProfile.pcSettings.compressionQuality = 50;
        currentProfile.pcSettings.resizeAlgorithm = TextureResizeAlgorithm.Mitchell;

        // Android - Optimized for mobile, ETC2 support
        currentProfile.androidSettings.maxSize = 1024;
        currentProfile.androidSettings.format = TextureImporterFormat.ETC2_RGBA8;
        currentProfile.androidSettings.compression = TextureImporterCompression.Compressed;
        currentProfile.androidSettings.useCrunchCompression = true;
        currentProfile.androidSettings.compressionQuality = 50;
        currentProfile.androidSettings.overrideETC2Fallback = true;
        currentProfile.androidSettings.etc2FallbackOverride = AndroidETC2FallbackOverride.Quality32Bit;
        currentProfile.androidSettings.resizeAlgorithm = TextureResizeAlgorithm.Mitchell;

        // iOS - Optimized for mobile, ASTC support
        currentProfile.iosSettings.maxSize = 1024;
        currentProfile.iosSettings.format = TextureImporterFormat.ASTC_6x6;
        currentProfile.iosSettings.compression = TextureImporterCompression.Compressed;
        currentProfile.iosSettings.useCrunchCompression = false;
        currentProfile.iosSettings.compressionQuality = 50;
        currentProfile.iosSettings.resizeAlgorithm = TextureResizeAlgorithm.Mitchell;

        // WebGL - Smaller sizes for web delivery
        currentProfile.webglSettings.maxSize = 512;
        currentProfile.webglSettings.format = TextureImporterFormat.DXT5;
        currentProfile.webglSettings.compression = TextureImporterCompression.Compressed;
        currentProfile.webglSettings.useCrunchCompression = true;
        currentProfile.webglSettings.compressionQuality = 75;
        currentProfile.webglSettings.resizeAlgorithm = TextureResizeAlgorithm.Mitchell;
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        EditorGUILayout.LabelField("Texture Optimization Tool", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        DrawPathSelection();
        EditorGUILayout.Space();

        DrawProfileSettings();
        EditorGUILayout.Space();

        DrawPlatformSettings();
        EditorGUILayout.Space();

        DrawActionButtons();

        EditorGUILayout.EndScrollView();
    }

    private void DrawPathSelection()
    {
        EditorGUILayout.LabelField("Target Paths", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Add Assets Folder", GUILayout.Width(120)))
        {
            string path = EditorUtility.OpenFolderPanel("Select Folder", "Assets", "");
            if (!string.IsNullOrEmpty(path) && path.Contains(Application.dataPath))
            {
                path = "Assets" + path.Substring(Application.dataPath.Length);
                if (!targetPaths.Contains(path))
                    targetPaths.Add(path);
            }
        }
        
        if (GUILayout.Button("Add Current Selection", GUILayout.Width(150)))
        {
            foreach (Object obj in Selection.objects)
            {
                string path = AssetDatabase.GetAssetPath(obj);
                if (Directory.Exists(path) && !targetPaths.Contains(path))
                    targetPaths.Add(path);
            }
        }
        
        if (GUILayout.Button("Clear All", GUILayout.Width(80)))
        {
            targetPaths.Clear();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
        
        if (targetPaths.Count == 0)
        {
            EditorGUILayout.HelpBox("No paths selected. Add folders to optimize textures within them.", MessageType.Info);
        }
        else
        {
            EditorGUILayout.LabelField("Selected Paths:");
            for (int i = 0; i < targetPaths.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(targetPaths[i]);
                if (GUILayout.Button("Remove", GUILayout.Width(60)))
                {
                    targetPaths.RemoveAt(i);
                    i--;
                }
                EditorGUILayout.EndHorizontal();
            }
        }
    }

    private void DrawProfileSettings()
    {
        EditorGUILayout.LabelField("Optimization Profile", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        currentProfile.name = EditorGUILayout.TextField("Profile Name", currentProfile.name);
        
        if (GUILayout.Button("Reset to Optimal", GUILayout.Width(120)))
        {
            SetOptimalDefaults();
        }
        EditorGUILayout.EndHorizontal();
        
        showAdvancedSettings = EditorGUILayout.Toggle("Show Advanced Settings", showAdvancedSettings);
    }

    private void DrawPlatformSettings()
    {
        EditorGUILayout.LabelField("Platform Settings", EditorStyles.boldLabel);

        // PC Settings
        showPCSettings = EditorGUILayout.Foldout(showPCSettings, "PC (Standalone) Settings");
        if (showPCSettings)
        {
            EditorGUI.indentLevel++;
            DrawPlatformSettingsFields(currentProfile.pcSettings, "PC");
            EditorGUI.indentLevel--;
        }

        // Android Settings  
        showAndroidSettings = EditorGUILayout.Foldout(showAndroidSettings, "Android Settings");
        if (showAndroidSettings)
        {
            EditorGUI.indentLevel++;
            DrawPlatformSettingsFields(currentProfile.androidSettings, "Android");
            EditorGUI.indentLevel--;
        }

        // iOS Settings
        showIOSSettings = EditorGUILayout.Foldout(showIOSSettings, "iOS Settings");
        if (showIOSSettings)
        {
            EditorGUI.indentLevel++;
            DrawPlatformSettingsFields(currentProfile.iosSettings, "iOS");
            EditorGUI.indentLevel--;
        }

        // WebGL Settings
        showWebGLSettings = EditorGUILayout.Foldout(showWebGLSettings, "WebGL Settings");
        if (showWebGLSettings)
        {
            EditorGUI.indentLevel++;
            DrawPlatformSettingsFields(currentProfile.webglSettings, "WebGL");
            EditorGUI.indentLevel--;
        }
    }

    private void DrawPlatformSettingsFields(PlatformSettings settings, string platformName)
    {
        settings.maxSize = EditorGUILayout.IntPopup("Max Size", settings.maxSize, 
            new string[] { "32", "64", "128", "256", "512", "1024", "2048", "4096", "8192" },
            new int[] { 32, 64, 128, 256, 512, 1024, 2048, 4096, 8192 });

        if (showAdvancedSettings)
        {
            settings.format = (TextureImporterFormat)EditorGUILayout.EnumPopup("Format", settings.format);
            settings.compression = (TextureImporterCompression)EditorGUILayout.EnumPopup("Compression", settings.compression);
            settings.resizeAlgorithm = (TextureResizeAlgorithm)EditorGUILayout.EnumPopup("Resize Algorithm", settings.resizeAlgorithm);
            
            settings.useCrunchCompression = EditorGUILayout.Toggle("Use Crunch Compression", settings.useCrunchCompression);
            
            if (settings.useCrunchCompression || settings.compression != TextureImporterCompression.Uncompressed)
            {
                settings.compressionQuality = EditorGUILayout.IntSlider("Compression Quality", settings.compressionQuality, 0, 100);
            }

            if (platformName == "Android")
            {
                settings.overrideETC2Fallback = EditorGUILayout.Toggle("Override ETC2 Fallback", settings.overrideETC2Fallback);
                if (settings.overrideETC2Fallback)
                {
                    settings.etc2FallbackOverride = (AndroidETC2FallbackOverride)EditorGUILayout.EnumPopup("ETC2 Fallback", settings.etc2FallbackOverride);
                }
            }
        }

        EditorGUILayout.Space();
    }

    private void DrawActionButtons()
    {
        EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);
        
        GUI.enabled = targetPaths.Count > 0;
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Optimize Selected Paths", GUILayout.Height(30)))
        {
            OptimizeTextures();
        }
        
        if (GUILayout.Button("Preview Changes", GUILayout.Height(30)))
        {
            PreviewChanges();
        }
        EditorGUILayout.EndHorizontal();
        
        GUI.enabled = true;
        
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("This tool will apply platform overrides and optimize texture settings. Always backup your project before running optimization.", MessageType.Warning);
    }

    private void OptimizeTextures()
    {
        if (EditorUtility.DisplayDialog("Optimize Textures", 
            $"This will optimize all textures in {targetPaths.Count} selected path(s). This action cannot be undone. Continue?", 
            "Optimize", "Cancel"))
        {
            int processed = 0;
            int total = CountTextures();
            
            foreach (string path in targetPaths)
            {
                string[] textureGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { path });
                
                foreach (string guid in textureGuids)
                {
                    string texturePath = AssetDatabase.GUIDToAssetPath(guid);
                    TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
                    
                    if (importer != null)
                    {
                        EditorUtility.DisplayProgressBar("Optimizing Textures", 
                            $"Processing: {Path.GetFileName(texturePath)}", 
                            (float)processed / total);
                        
                        ApplyOptimizationToTexture(importer);
                        processed++;
                    }
                }
            }
            
            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();
            
            EditorUtility.DisplayDialog("Optimization Complete", 
                $"Successfully optimized {processed} textures!", "OK");
        }
    }

    private void ApplyOptimizationToTexture(TextureImporter importer)
    {
        // Apply PC settings
        var pcPlatform = importer.GetPlatformTextureSettings("Standalone");
        ApplyPlatformSettings(pcPlatform, currentProfile.pcSettings);
        pcPlatform.overridden = true;
        importer.SetPlatformTextureSettings(pcPlatform);

        // Apply Android settings
        var androidPlatform = importer.GetPlatformTextureSettings("Android");
        ApplyPlatformSettings(androidPlatform, currentProfile.androidSettings);
        androidPlatform.overridden = true;
        if (currentProfile.androidSettings.overrideETC2Fallback)
        {
            androidPlatform.androidETC2FallbackOverride = currentProfile.androidSettings.etc2FallbackOverride;
        }
        importer.SetPlatformTextureSettings(androidPlatform);

        // Apply iOS settings
        var iosPlatform = importer.GetPlatformTextureSettings("iPhone");
        ApplyPlatformSettings(iosPlatform, currentProfile.iosSettings);
        iosPlatform.overridden = true;
        importer.SetPlatformTextureSettings(iosPlatform);

        // Apply WebGL settings
        var webglPlatform = importer.GetPlatformTextureSettings("WebGL");
        ApplyPlatformSettings(webglPlatform, currentProfile.webglSettings);
        webglPlatform.overridden = true;
        importer.SetPlatformTextureSettings(webglPlatform);

        // Apply general settings
        importer.textureCompression = currentProfile.pcSettings.compression;
        importer.crunchedCompression = currentProfile.pcSettings.useCrunchCompression;
        
        AssetDatabase.ImportAsset(importer.assetPath, ImportAssetOptions.ForceUpdate);
    }

    private void ApplyPlatformSettings(TextureImporterPlatformSettings platform, PlatformSettings settings)
    {
        platform.maxTextureSize = settings.maxSize;
        platform.format = settings.format;
        platform.textureCompression = settings.compression;
        platform.crunchedCompression = settings.useCrunchCompression;
        platform.compressionQuality = settings.compressionQuality;
        platform.resizeAlgorithm = settings.resizeAlgorithm;
    }

    private void PreviewChanges()
    {
        int textureCount = CountTextures();
        string message = $"Found {textureCount} textures in selected paths.\n\n";
        message += "Optimization will apply these settings:\n";
        message += $"• PC: {currentProfile.pcSettings.maxSize}px, {currentProfile.pcSettings.format}\n";
        message += $"• Android: {currentProfile.androidSettings.maxSize}px, {currentProfile.androidSettings.format}\n";
        message += $"• iOS: {currentProfile.iosSettings.maxSize}px, {currentProfile.iosSettings.format}\n";
        message += $"• WebGL: {currentProfile.webglSettings.maxSize}px, {currentProfile.webglSettings.format}";
        
        EditorUtility.DisplayDialog("Preview Changes", message, "OK");
    }

    private int CountTextures()
    {
        int count = 0;
        foreach (string path in targetPaths)
        {
            string[] textureGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { path });
            count += textureGuids.Length;
        }
        return count;
    }
}