using UnityEngine;
using UnityEditor;
using UnityEngine.U2D;
using UnityEditor.U2D;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class AtlasOptimizerTool : EditorWindow
{
    [System.Serializable]
    public class AtlasPlatformSettings
    {
        public int maxAtlasSize = 2048;
        public TextureImporterFormat textureFormat = TextureImporterFormat.Automatic;
        public TextureImporterCompression compressionSettings = TextureImporterCompression.Compressed;
        public bool crunchedCompression = false;
        public int compressionQuality = 50;
        public bool allowsAlphaSplitting = false;
    }

    [System.Serializable]
    public class AtlasOptimizationProfile
    {
        public string name = "Default";
        public AtlasPlatformSettings pcSettings = new AtlasPlatformSettings();
        public AtlasPlatformSettings androidSettings = new AtlasPlatformSettings();
        public AtlasPlatformSettings iosSettings = new AtlasPlatformSettings();
        public AtlasPlatformSettings webglSettings = new AtlasPlatformSettings();
    }

    private List<SpriteAtlas> selectedAtlases = new List<SpriteAtlas>();
    private AtlasOptimizationProfile currentProfile = new AtlasOptimizationProfile();
    private Vector2 scrollPosition;
    private bool showAdvancedSettings = false;
    private bool showPCSettings = true;
    private bool showAndroidSettings = true;
    private bool showIOSSettings = true;
    private bool showWebGLSettings = true;

    [MenuItem("Tools/Optimization/Atlas Optimizer")]
    public static void ShowWindow()
    {
        GetWindow<AtlasOptimizerTool>("Atlas Optimizer");
    }

    private void OnEnable()
    {
        SetOptimalDefaults();
        RefreshAtlasList();
    }

    private void SetOptimalDefaults()
    {
        // PC (Standalone) - High quality, larger sizes
        currentProfile.pcSettings.maxAtlasSize = 2048;
        currentProfile.pcSettings.textureFormat = TextureImporterFormat.Automatic;
        currentProfile.pcSettings.compressionSettings = TextureImporterCompression.Compressed;
        currentProfile.pcSettings.crunchedCompression = false;
        currentProfile.pcSettings.compressionQuality = 50;
        currentProfile.pcSettings.allowsAlphaSplitting = false;

        // Android - Optimized for mobile, ETC2 support
        currentProfile.androidSettings.maxAtlasSize = 2048;
        currentProfile.androidSettings.textureFormat = TextureImporterFormat.ETC2_RGBA8;
        currentProfile.androidSettings.compressionSettings = TextureImporterCompression.Compressed;
        currentProfile.androidSettings.crunchedCompression = true;
        currentProfile.androidSettings.compressionQuality = 50;
        currentProfile.androidSettings.allowsAlphaSplitting = true;

        // iOS - Optimized for mobile, ASTC support
        currentProfile.iosSettings.maxAtlasSize = 2048;
        currentProfile.iosSettings.textureFormat = TextureImporterFormat.ASTC_6x6;
        currentProfile.iosSettings.compressionSettings = TextureImporterCompression.Compressed;
        currentProfile.iosSettings.crunchedCompression = false;
        currentProfile.iosSettings.compressionQuality = 50;
        currentProfile.iosSettings.allowsAlphaSplitting = false;

        // WebGL - Smaller sizes for web delivery
        currentProfile.webglSettings.maxAtlasSize = 1024;
        currentProfile.webglSettings.textureFormat = TextureImporterFormat.DXT5;
        currentProfile.webglSettings.compressionSettings = TextureImporterCompression.Compressed;
        currentProfile.webglSettings.crunchedCompression = true;
        currentProfile.webglSettings.compressionQuality = 75;
        currentProfile.webglSettings.allowsAlphaSplitting = false;
    }

    private void RefreshAtlasList()
    {
        selectedAtlases.Clear();
        string[] atlasGuids = AssetDatabase.FindAssets("t:SpriteAtlas");
        
        foreach (string guid in atlasGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            SpriteAtlas atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(path);
            if (atlas != null)
            {
                selectedAtlases.Add(atlas);
            }
        }
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        EditorGUILayout.LabelField("Atlas Optimization Tool", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        DrawAtlasSelection();
        EditorGUILayout.Space();

        DrawProfileSettings();
        EditorGUILayout.Space();

        DrawPlatformSettings();
        EditorGUILayout.Space();

        DrawActionButtons();

        EditorGUILayout.EndScrollView();
    }

    private void DrawAtlasSelection()
    {
        EditorGUILayout.LabelField("Sprite Atlases", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Refresh Atlas List", GUILayout.Width(120)))
        {
            RefreshAtlasList();
        }
        
        if (GUILayout.Button("Clear Selection", GUILayout.Width(100)))
        {
            for (int i = 0; i < selectedAtlases.Count; i++)
            {
                // This is a simplified clear - you might want more sophisticated selection
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
        
        if (selectedAtlases.Count == 0)
        {
            EditorGUILayout.HelpBox("No Sprite Atlases found in the project.", MessageType.Info);
        }
        else
        {
            EditorGUILayout.LabelField($"Found {selectedAtlases.Count} Sprite Atlases:");
            
            for (int i = 0; i < selectedAtlases.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.ObjectField(selectedAtlases[i], typeof(SpriteAtlas), false);
                
                // Show some basic info about the atlas
                if (selectedAtlases[i] != null)
                {
                    var so = new SerializedObject(selectedAtlases[i]);
                    var packingSettings = so.FindProperty("m_PackingSettings");
                    if (packingSettings != null)
                    {
                        var maxTextureSizeProp = packingSettings.FindPropertyRelative("maxTextureSizePC");
                        if (maxTextureSizeProp != null)
                        {
                            EditorGUILayout.LabelField($"Size: {maxTextureSizeProp.intValue}", GUILayout.Width(80));
                        }
                    }
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
            DrawAtlasPlatformSettingsFields(currentProfile.pcSettings, "PC");
            EditorGUI.indentLevel--;
        }

        // Android Settings  
        showAndroidSettings = EditorGUILayout.Foldout(showAndroidSettings, "Android Settings");
        if (showAndroidSettings)
        {
            EditorGUI.indentLevel++;
            DrawAtlasPlatformSettingsFields(currentProfile.androidSettings, "Android");
            EditorGUI.indentLevel--;
        }

        // iOS Settings
        showIOSSettings = EditorGUILayout.Foldout(showIOSSettings, "iOS Settings");
        if (showIOSSettings)
        {
            EditorGUI.indentLevel++;
            DrawAtlasPlatformSettingsFields(currentProfile.iosSettings, "iOS");
            EditorGUI.indentLevel--;
        }

        // WebGL Settings
        showWebGLSettings = EditorGUILayout.Foldout(showWebGLSettings, "WebGL Settings");
        if (showWebGLSettings)
        {
            EditorGUI.indentLevel++;
            DrawAtlasPlatformSettingsFields(currentProfile.webglSettings, "WebGL");
            EditorGUI.indentLevel--;
        }
    }

    private void DrawAtlasPlatformSettingsFields(AtlasPlatformSettings settings, string platformName)
    {
        settings.maxAtlasSize = EditorGUILayout.IntPopup("Max Atlas Size", settings.maxAtlasSize,
            new string[] { "32", "64", "128", "256", "512", "1024", "2048", "4096", "8192" },
            new int[] { 32, 64, 128, 256, 512, 1024, 2048, 4096, 8192 });

        if (showAdvancedSettings)
        {
            settings.textureFormat = (TextureImporterFormat)EditorGUILayout.EnumPopup("Texture Format", settings.textureFormat);
            settings.compressionSettings = (TextureImporterCompression)EditorGUILayout.EnumPopup("Compression", settings.compressionSettings);
            
            settings.crunchedCompression = EditorGUILayout.Toggle("Crunch Compression", settings.crunchedCompression);
            
            if (settings.crunchedCompression || settings.compressionSettings != TextureImporterCompression.Uncompressed)
            {
                settings.compressionQuality = EditorGUILayout.IntSlider("Compression Quality", settings.compressionQuality, 0, 100);
            }

            if (platformName == "Android")
            {
                settings.allowsAlphaSplitting = EditorGUILayout.Toggle("Allow Alpha Splitting", settings.allowsAlphaSplitting);
            }
        }

        EditorGUILayout.Space();
    }

    private void DrawActionButtons()
    {
        EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);
        
        GUI.enabled = selectedAtlases.Count > 0;
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Optimize All Atlases", GUILayout.Height(30)))
        {
            OptimizeAtlases();
        }
        
        if (GUILayout.Button("Preview Changes", GUILayout.Height(30)))
        {
            PreviewChanges();
        }
        EditorGUILayout.EndHorizontal();
        
        if (GUILayout.Button("Pack All Atlases", GUILayout.Height(25)))
        {
            PackAllAtlases();
        }
        
        GUI.enabled = true;
        
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("This tool will optimize all Sprite Atlases with platform-specific settings. Always backup your project before running optimization.", MessageType.Warning);
    }

    private void OptimizeAtlases()
    {
        if (EditorUtility.DisplayDialog("Optimize Sprite Atlases", 
            $"This will optimize all {selectedAtlases.Count} Sprite Atlases. This action cannot be undone. Continue?", 
            "Optimize", "Cancel"))
        {
            int processed = 0;
            int total = selectedAtlases.Count;
            
            foreach (SpriteAtlas atlas in selectedAtlases)
            {
                if (atlas != null)
                {
                    EditorUtility.DisplayProgressBar("Optimizing Sprite Atlases", 
                        $"Processing: {atlas.name}", 
                        (float)processed / total);
                    
                    ApplyOptimizationToAtlas(atlas);
                    processed++;
                }
            }
            
            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();
            
            EditorUtility.DisplayDialog("Optimization Complete", 
                $"Successfully optimized {processed} Sprite Atlases!", "OK");
        }
    }

    private void ApplyOptimizationToAtlas(SpriteAtlas atlas)
    {
        var so = new SerializedObject(atlas);
        
        // PC Platform Settings
        ApplyAtlasPlatformSettings(atlas, "DefaultTexturePlatform", currentProfile.pcSettings);
        
        // Android Platform Settings
        ApplyAtlasPlatformSettings(atlas, "Android", currentProfile.androidSettings);
        
        // iOS Platform Settings  
        ApplyAtlasPlatformSettings(atlas, "iPhone", currentProfile.iosSettings);
        
        // WebGL Platform Settings
        ApplyAtlasPlatformSettings(atlas, "WebGL", currentProfile.webglSettings);
        
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(atlas);
    }

    private void ApplyAtlasPlatformSettings(SpriteAtlas atlas, string platform, AtlasPlatformSettings settings)
    {
        var textureSettings = atlas.GetPlatformSettings(platform);
        
        textureSettings.maxTextureSize = settings.maxAtlasSize;
        textureSettings.format = settings.textureFormat;
        textureSettings.compressionQuality = settings.compressionQuality;
        textureSettings.crunchedCompression = settings.crunchedCompression;
        textureSettings.allowsAlphaSplitting = settings.allowsAlphaSplitting;
        textureSettings.overridden = true;
        
        atlas.SetPlatformSettings(textureSettings);
    }

    private void PreviewChanges()
    {
        string message = $"Found {selectedAtlases.Count} Sprite Atlases.\\n\\n";
        message += "Optimization will apply these settings:\\n";
        message += $"• PC: {currentProfile.pcSettings.maxAtlasSize}px, {currentProfile.pcSettings.textureFormat}\\n";
        message += $"• Android: {currentProfile.androidSettings.maxAtlasSize}px, {currentProfile.androidSettings.textureFormat}\\n";
        message += $"• iOS: {currentProfile.iosSettings.maxAtlasSize}px, {currentProfile.iosSettings.textureFormat}\\n";
        message += $"• WebGL: {currentProfile.webglSettings.maxAtlasSize}px, {currentProfile.webglSettings.textureFormat}";
        
        EditorUtility.DisplayDialog("Preview Changes", message, "OK");
    }

    private void PackAllAtlases()
    {
        if (EditorUtility.DisplayDialog("Pack All Sprite Atlases", 
            "This will repack all Sprite Atlases. This may take some time. Continue?", 
            "Pack", "Cancel"))
        {
            SpriteAtlasUtility.PackAllAtlases(EditorUserBuildSettings.activeBuildTarget, false);
            EditorUtility.DisplayDialog("Packing Complete", "All Sprite Atlases have been packed!", "OK");
        }
    }

    [MenuItem("Tools/Optimization/Pack All Atlases")]
    public static void PackAllAtlasesQuick()
    {
        SpriteAtlasUtility.PackAllAtlases(EditorUserBuildSettings.activeBuildTarget, false);
        Debug.Log("All Sprite Atlases packed!");
    }
}