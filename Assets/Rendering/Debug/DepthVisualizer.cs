using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class DepthVisualizer : MonoBehaviour
{
    [System.Serializable]
    public enum VisualizationMode
    {
        Off,
        LinearDepth,
        RawDepth,
        Rainbow,
        HeatMap
    }

    public VisualizationMode visualizationMode = VisualizationMode.Rainbow;
    [Range(0.01f, 1.0f)]
    public float depthScale = 0.1f;
    [Range(0.0f, 1.0f)]
    public float contrastEnhancement = 0.5f;
    
    public bool showUI = true;
    public KeyCode toggleKey = KeyCode.F3;

    public Material depthMaterial;
    public RenderTexture depthTexture;
    public Camera mainCamera;
    private DepthVisualizerPass depthPass;
    
    // Shader property IDs
    private int depthTexID;
    private int depthScaleID;
    private int visualizationModeID;
    private int contrastEnhancementID;

    private void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        depthMaterial = new Material(Shader.Find("Hidden/DepthVisualizer"));
        
        // Cache shader property IDs for efficiency
        depthTexID = Shader.PropertyToID("_DepthTex");
        depthScaleID = Shader.PropertyToID("_DepthScale");
        visualizationModeID = Shader.PropertyToID("_VisualizationMode");
        contrastEnhancementID = Shader.PropertyToID("_ContrastEnhancement");
        
        // Create render texture for depth
        CreateDepthTexture();
        
        // Add to camera command buffer
        SetupCommandBuffer();
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            visualizationMode = (VisualizationMode)(((int)visualizationMode + 1) % 5);
        }
    }

    private void OnDisable()
    {
        mainCamera?.RemoveCommandBuffer(CameraEvent.AfterForwardOpaque, depthPass?.commandBuffer);
    }

    private void OnDestroy()
    {
        mainCamera?.RemoveCommandBuffer(CameraEvent.AfterForwardOpaque, depthPass?.commandBuffer);
        if (depthTexture != null)
            depthTexture.Release();
        if (depthMaterial != null)
            Destroy(depthMaterial);
    }
    
    private void CreateDepthTexture()
    {
        if (depthTexture != null)
            depthTexture.Release();
            
        depthTexture = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.RFloat);
        depthTexture.filterMode = FilterMode.Point;
        depthTexture.wrapMode = TextureWrapMode.Clamp;
    }

    private void SetupCommandBuffer()
    {
        if (depthPass != null)
        {
            mainCamera.RemoveCommandBuffer(CameraEvent.AfterForwardOpaque, depthPass.commandBuffer);
        }
        
        depthPass = new DepthVisualizerPass(depthMaterial, depthTexture);
        mainCamera.AddCommandBuffer(CameraEvent.AfterForwardOpaque, depthPass.commandBuffer);
    }

    private void OnGUI()
    {
        if (!showUI) return;
        
        GUILayout.BeginArea(new Rect(10, 10, 300, 100));
        GUILayout.Label($"Depth Visualization: {visualizationMode} (Press {toggleKey} to toggle)");
        GUILayout.Label($"Depth Scale: {depthScale:F2}");
        depthScale = GUILayout.HorizontalSlider(depthScale, 0.01f, 1.0f);
        GUILayout.Label($"Contrast: {contrastEnhancement:F2}");
        contrastEnhancement = GUILayout.HorizontalSlider(contrastEnhancement, 0.0f, 1.0f);
        GUILayout.EndArea();
        
        // Update shader parameters
        depthMaterial.SetFloat(depthScaleID, depthScale);
        depthMaterial.SetInt(visualizationModeID, (int)visualizationMode);
        depthMaterial.SetFloat(contrastEnhancementID, contrastEnhancement);
    }

    private class DepthVisualizerPass
    {
        public CommandBuffer commandBuffer;
        private Material depthMaterial;
        private RenderTexture depthTexture;

        public DepthVisualizerPass(Material material, RenderTexture depthTex)
        {
            depthMaterial = material;
            depthTexture = depthTex;
            
            commandBuffer = new CommandBuffer();
            commandBuffer.name = "Depth Visualizer";
            
            // Set up the depth visualization
            int depthTexID = Shader.PropertyToID("_DepthTex");
            commandBuffer.SetGlobalTexture(depthTexID, depthTexture);
            commandBuffer.Blit(BuiltinRenderTextureType.CameraTarget, depthTexture, depthMaterial, 0);
            commandBuffer.Blit(depthTexture, BuiltinRenderTextureType.CameraTarget, depthMaterial, 1);
        }
    }
}