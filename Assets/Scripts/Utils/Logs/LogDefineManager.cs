#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class LogDefineManager
{
    private const string DEBUG_LOGS_DEFINE = "ENABLE_DEBUG_LOGS";
    private const string VERBOSE_LOGS_DEFINE = "ENABLE_VERBOSE_LOGS";
    
    [MenuItem("Tools/Log System/Enable All Logs")]
    public static void EnableAllLogs()
    {
        SetDefineSymbols(true, true);
        Debug.Log("All logs enabled!");
    }
    
    [MenuItem("Tools/Log System/Enable Basic Logs Only")]
    public static void EnableBasicLogs()
    {
        SetDefineSymbols(true, false);
        Debug.Log("Basic logs enabled, verbose disabled");
    }
    
    [MenuItem("Tools/Log System/Disable All Logs")]
    public static void DisableAllLogs()
    {
        SetDefineSymbols(false, false);
        Debug.Log("All logs disabled for performance");
    }
    
    private static void SetDefineSymbols(bool enableDebug, bool enableVerbose)
    {
        BuildTargetGroup buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
        string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);
        
        // Remove existing defines
        defines = defines.Replace(DEBUG_LOGS_DEFINE, "").Replace(VERBOSE_LOGS_DEFINE, "");
        defines = defines.Replace(";;", ";").Trim(';');
        
        // Add requested defines
        if (enableDebug)
            defines += ";" + DEBUG_LOGS_DEFINE;
        if (enableVerbose)
            defines += ";" + VERBOSE_LOGS_DEFINE;
            
        defines = defines.Trim(';');
        
        PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, defines);
        AssetDatabase.Refresh();
    }
}
#endif