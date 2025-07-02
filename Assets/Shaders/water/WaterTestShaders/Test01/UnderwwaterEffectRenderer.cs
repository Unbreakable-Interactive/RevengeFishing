using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(Camera))]
public class UnderwaterEffectRenderer : MonoBehaviour
{
    [Header("Water Settings")]
    public LayerMask waterLayer;
    
    [Header("Effect Settings")]
    public Material underwaterEffectMaterial;
    
    // Private references
    private Camera mainCamera;
    private Camera waterDepthCamera;
    private RenderTexture waterDepthRT;
    private RenderTexture sceneColorRT;
    private CommandBuffer afterRenderingCmd;
    
    private void Start()
    {
        mainCamera = GetComponent<Camera>();
        InitializeRenderTextures();
        SetupWaterDepthCamera();
        SetupCommandBuffer();
    }
    
    private void InitializeRenderTextures()
    {
        int width = mainCamera.pixelWidth;
        int height = mainCamera.pixelHeight;
        
        // Create a render texture for water depth
        waterDepthRT = new RenderTexture(width, height, 24, RenderTextureFormat.Depth);
        waterDepthRT.name = "WaterDepthTexture";
        
        // Create a render texture for scene color
        sceneColorRT = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
        sceneColorRT.name = "SceneColorTexture";
    }
    
    private void SetupWaterDepthCamera()
    {
        // Create a new GameObject for the water depth camera
        GameObject waterCameraGO = new GameObject("WaterDepthCamera");
        waterCameraGO.transform.parent = transform;
        
        // Add camera component
        waterDepthCamera = waterCameraGO.AddComponent<Camera>();
        waterDepthCamera.CopyFrom(mainCamera);
        
        // Configure camera for depth only
        waterDepthCamera.clearFlags = CameraClearFlags.SolidColor;
        waterDepthCamera.backgroundColor = Color.black;
        waterDepthCamera.cullingMask = waterLayer;
        waterDepthCamera.targetTexture = waterDepthRT;
        waterDepthCamera.enabled = false; // We'll render manually
    }
    
    private void SetupCommandBuffer()
    {
        afterRenderingCmd = new CommandBuffer();
        afterRenderingCmd.name = "UnderwaterPostProcess";
        
        // Capture the rendered scene to the scene color texture
        afterRenderingCmd.Blit(BuiltinRenderTextureType.CameraTarget, sceneColorRT);
        
        // Set textures for the underwater effect shader
        afterRenderingCmd.SetGlobalTexture("_WaterDepthTexture", waterDepthRT);
        afterRenderingCmd.SetGlobalTexture("_MainTex", sceneColorRT);
        
        // Apply the underwater effect
        afterRenderingCmd.Blit(sceneColorRT, BuiltinRenderTextureType.CameraTarget, underwaterEffectMaterial);
        
        // Add command buffer to camera
        mainCamera.AddCommandBuffer(CameraEvent.AfterImageEffects, afterRenderingCmd);
    }
    
    private void OnPreRender()
    {
        // Ensure the water depth texture is updated before the main camera renders
        if (waterDepthCamera != null)
        {
            // Make sure the water camera matches the main camera's transform
            waterDepthCamera.transform.position = mainCamera.transform.position;
            waterDepthCamera.transform.rotation = mainCamera.transform.rotation;
            
            // Render the water to the depth texture
            waterDepthCamera.Render();
        }
    }
    
    private void OnDestroy()
    {
        if (afterRenderingCmd != null)
        {
            mainCamera.RemoveCommandBuffer(CameraEvent.AfterImageEffects, afterRenderingCmd);
            afterRenderingCmd.Dispose();
        }
        
        if (waterDepthRT != null)
            waterDepthRT.Release();
            
        if (sceneColorRT != null)
            sceneColorRT.Release();
            
        if (waterDepthCamera != null)
            Destroy(waterDepthCamera.gameObject);
    }
    
    private void OnValidate()
    {
        // Update when settings change
        if (Application.isPlaying && afterRenderingCmd != null)
        {
            mainCamera.RemoveCommandBuffer(CameraEvent.AfterImageEffects, afterRenderingCmd);
            SetupCommandBuffer();
        }
    }
}