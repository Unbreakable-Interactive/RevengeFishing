using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

public class AudioOptimizerTool : EditorWindow
{
    [System.Serializable]
    public class AudioPlatformSettings
    {
        public AudioClipLoadType loadType = AudioClipLoadType.DecompressOnLoad;
        public AudioCompressionFormat compressionFormat = AudioCompressionFormat.Vorbis;
        public float quality = 0.7f;
        public AudioSampleRateSetting sampleRateSetting = AudioSampleRateSetting.OptimizeSampleRate;
        public uint sampleRateOverride = 44100;
        public bool forceToMono = false;
        public bool loadInBackground = false;
    }

    [System.Serializable]
    public class AudioOptimizationProfile
    {
        public string name = "Default";
        public AudioPlatformSettings pcSettings = new AudioPlatformSettings();
        public AudioPlatformSettings androidSettings = new AudioPlatformSettings();
        public AudioPlatformSettings iosSettings = new AudioPlatformSettings();
        public AudioPlatformSettings webglSettings = new AudioPlatformSettings();
    }

    private List<string> targetPaths = new List<string>();
    private AudioOptimizationProfile currentProfile = new AudioOptimizationProfile();
    private Vector2 scrollPosition;
    private bool showAdvancedSettings = false;
    private bool showPCSettings = true;
    private bool showAndroidSettings = true;
    private bool showIOSSettings = true;
    private bool showWebGLSettings = true;

    [MenuItem("Tools/Optimization/Audio Optimizer")]
    public static void ShowWindow()
    {
        GetWindow<AudioOptimizerTool>("Audio Optimizer");
    }

    private void OnEnable()
    {
        SetOptimalDefaults();
    }

    private void SetOptimalDefaults()
    {
        // PC (Standalone) - High quality, can afford larger files
        currentProfile.pcSettings.loadType = AudioClipLoadType.DecompressOnLoad;
        currentProfile.pcSettings.compressionFormat = AudioCompressionFormat.Vorbis;
        currentProfile.pcSettings.quality = 0.8f;
        currentProfile.pcSettings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
        currentProfile.pcSettings.sampleRateOverride = 44100;
        currentProfile.pcSettings.forceToMono = false;
        currentProfile.pcSettings.loadInBackground = false;

        // Android - Optimized for mobile, compressed
        currentProfile.androidSettings.loadType = AudioClipLoadType.CompressedInMemory;
        currentProfile.androidSettings.compressionFormat = AudioCompressionFormat.Vorbis;
        currentProfile.androidSettings.quality = 0.5f;
        currentProfile.androidSettings.sampleRateSetting = AudioSampleRateSetting.OptimizeSampleRate;
        currentProfile.androidSettings.sampleRateOverride = 22050;
        currentProfile.androidSettings.forceToMono = false;
        currentProfile.androidSettings.loadInBackground = false;

        // iOS - Optimized for mobile, MP3 support
        currentProfile.iosSettings.loadType = AudioClipLoadType.CompressedInMemory;
        currentProfile.iosSettings.compressionFormat = AudioCompressionFormat.MP3;
        currentProfile.iosSettings.quality = 0.6f;
        currentProfile.iosSettings.sampleRateSetting = AudioSampleRateSetting.OptimizeSampleRate;
        currentProfile.iosSettings.sampleRateOverride = 22050;
        currentProfile.iosSettings.forceToMono = false;
        currentProfile.iosSettings.loadInBackground = false;

        // WebGL - Compressed for web delivery
        currentProfile.webglSettings.loadType = AudioClipLoadType.CompressedInMemory;
        currentProfile.webglSettings.compressionFormat = AudioCompressionFormat.Vorbis;
        currentProfile.webglSettings.quality = 0.4f;
        currentProfile.webglSettings.sampleRateSetting = AudioSampleRateSetting.OverrideSampleRate;
        currentProfile.webglSettings.sampleRateOverride = 22050;
        currentProfile.webglSettings.forceToMono = true;
        currentProfile.webglSettings.loadInBackground = false;
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        EditorGUILayout.LabelField("Audio Optimization Tool", EditorStyles.boldLabel);
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
            EditorGUILayout.HelpBox("No paths selected. Add folders to optimize audio files within them.", MessageType.Info);
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
            DrawAudioPlatformSettingsFields(currentProfile.pcSettings, "PC");
            EditorGUI.indentLevel--;
        }

        // Android Settings  
        showAndroidSettings = EditorGUILayout.Foldout(showAndroidSettings, "Android Settings");
        if (showAndroidSettings)
        {
            EditorGUI.indentLevel++;
            DrawAudioPlatformSettingsFields(currentProfile.androidSettings, "Android");
            EditorGUI.indentLevel--;
        }

        // iOS Settings
        showIOSSettings = EditorGUILayout.Foldout(showIOSSettings, "iOS Settings");
        if (showIOSSettings)
        {
            EditorGUI.indentLevel++;
            DrawAudioPlatformSettingsFields(currentProfile.iosSettings, "iOS");
            EditorGUI.indentLevel--;
        }

        // WebGL Settings
        showWebGLSettings = EditorGUILayout.Foldout(showWebGLSettings, "WebGL Settings");
        if (showWebGLSettings)
        {
            EditorGUI.indentLevel++;
            DrawAudioPlatformSettingsFields(currentProfile.webglSettings, "WebGL");
            EditorGUI.indentLevel--;
        }
    }

    private void DrawAudioPlatformSettingsFields(AudioPlatformSettings settings, string platformName)
    {
        settings.loadType = (AudioClipLoadType)EditorGUILayout.EnumPopup("Load Type", settings.loadType);
        settings.compressionFormat = (AudioCompressionFormat)EditorGUILayout.EnumPopup("Compression Format", settings.compressionFormat);

        if (showAdvancedSettings)
        {
            if (settings.compressionFormat == AudioCompressionFormat.Vorbis || 
                settings.compressionFormat == AudioCompressionFormat.MP3)
            {
                settings.quality = EditorGUILayout.Slider("Quality", settings.quality, 0.01f, 1.0f);
            }

            settings.sampleRateSetting = (AudioSampleRateSetting)EditorGUILayout.EnumPopup("Sample Rate Setting", settings.sampleRateSetting);
            
            if (settings.sampleRateSetting == AudioSampleRateSetting.OverrideSampleRate)
            {
                settings.sampleRateOverride = (uint)EditorGUILayout.IntPopup("Sample Rate Override", (int)settings.sampleRateOverride,
                    new string[] { "8000 Hz", "11025 Hz", "22050 Hz", "44100 Hz", "48000 Hz", "96000 Hz" },
                    new int[] { 8000, 11025, 22050, 44100, 48000, 96000 });
            }

            settings.forceToMono = EditorGUILayout.Toggle("Force To Mono", settings.forceToMono);
            settings.loadInBackground = EditorGUILayout.Toggle("Load In Background", settings.loadInBackground);
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
            OptimizeAudioClips();
        }
        
        if (GUILayout.Button("Preview Changes", GUILayout.Height(30)))
        {
            PreviewChanges();
        }
        EditorGUILayout.EndHorizontal();
        
        GUI.enabled = true;
        
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("This tool will apply platform overrides and optimize audio settings. Always backup your project before running optimization.", MessageType.Warning);
    }

    private void OptimizeAudioClips()
    {
        if (EditorUtility.DisplayDialog("Optimize Audio Clips", 
            $"This will optimize all audio clips in {targetPaths.Count} selected path(s). This action cannot be undone. Continue?", 
            "Optimize", "Cancel"))
        {
            int processed = 0;
            int total = CountAudioClips();
            
            foreach (string path in targetPaths)
            {
                string[] audioGuids = AssetDatabase.FindAssets("t:AudioClip", new[] { path });
                
                foreach (string guid in audioGuids)
                {
                    string audioPath = AssetDatabase.GUIDToAssetPath(guid);
                    AudioImporter importer = AssetImporter.GetAtPath(audioPath) as AudioImporter;
                    
                    if (importer != null)
                    {
                        EditorUtility.DisplayProgressBar("Optimizing Audio Clips", 
                            $"Processing: {Path.GetFileName(audioPath)}", 
                            (float)processed / total);
                        
                        ApplyOptimizationToAudio(importer);
                        processed++;
                    }
                }
            }
            
            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();
            
            EditorUtility.DisplayDialog("Optimization Complete", 
                $"Successfully optimized {processed} audio clips!", "OK");
        }
    }

    private void ApplyOptimizationToAudio(AudioImporter importer)
    {
        // Apply general settings first
        importer.forceToMono = currentProfile.pcSettings.forceToMono;
        importer.loadInBackground = currentProfile.pcSettings.loadInBackground;
        
        // Apply default sample settings (this is the main/fallback setting)
        var defaultSettings = importer.defaultSampleSettings;
        defaultSettings.loadType = currentProfile.pcSettings.loadType;
        defaultSettings.compressionFormat = currentProfile.pcSettings.compressionFormat;
        defaultSettings.quality = currentProfile.pcSettings.quality;
        defaultSettings.sampleRateSetting = currentProfile.pcSettings.sampleRateSetting;
        defaultSettings.sampleRateOverride = currentProfile.pcSettings.sampleRateOverride;
        importer.defaultSampleSettings = defaultSettings;

        // Apply platform-specific overrides using correct Unity API
        // Each platform needs to be set with the proper BuildTargetGroup
        
        // PC (Standalone) settings
        var pcSettings = new AudioImporterSampleSettings();
        ApplyAudioPlatformSettings(pcSettings, currentProfile.pcSettings);
        importer.SetOverrideSampleSettings("Standalone", pcSettings);

        // Android settings
        var androidSettings = new AudioImporterSampleSettings();
        ApplyAudioPlatformSettings(androidSettings, currentProfile.androidSettings);
        importer.SetOverrideSampleSettings("Android", androidSettings);

        // iOS settings  
        var iosSettings = new AudioImporterSampleSettings();
        ApplyAudioPlatformSettings(iosSettings, currentProfile.iosSettings);
        importer.SetOverrideSampleSettings("iOS", iosSettings);

        // WebGL settings
        var webglSettings = new AudioImporterSampleSettings();
        ApplyAudioPlatformSettings(webglSettings, currentProfile.webglSettings);
        importer.SetOverrideSampleSettings("WebGL", webglSettings);
        
        AssetDatabase.ImportAsset(importer.assetPath, ImportAssetOptions.ForceUpdate);
    }

    private void ApplyAudioPlatformSettings(AudioImporterSampleSettings platformSettings, AudioPlatformSettings settings)
    {
        platformSettings.loadType = settings.loadType;
        platformSettings.compressionFormat = settings.compressionFormat;
        platformSettings.quality = settings.quality;
        platformSettings.sampleRateSetting = settings.sampleRateSetting;
        platformSettings.sampleRateOverride = settings.sampleRateOverride;
    }

    private void PreviewChanges()
    {
        int audioCount = CountAudioClips();
        string message = $"Found {audioCount} audio clips in selected paths.\n\n";
        message += "Optimization will apply these settings:\n";
        message += $"• PC: {currentProfile.pcSettings.loadType}, {currentProfile.pcSettings.compressionFormat}\n";
        message += $"• Android: {currentProfile.androidSettings.loadType}, {currentProfile.androidSettings.compressionFormat}\n";
        message += $"• iOS: {currentProfile.iosSettings.loadType}, {currentProfile.iosSettings.compressionFormat}\n";
        message += $"• WebGL: {currentProfile.webglSettings.loadType}, {currentProfile.webglSettings.compressionFormat}";
        
        EditorUtility.DisplayDialog("Preview Changes", message, "OK");
    }

    private int CountAudioClips()
    {
        int count = 0;
        foreach (string path in targetPaths)
        {
            string[] audioGuids = AssetDatabase.FindAssets("t:AudioClip", new[] { path });
            count += audioGuids.Length;
        }
        return count;
    }
}