using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class UnderwaterEffectFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class UnderwaterSettings
    {
        public Material underwaterMaterial;
        public LayerMask waterLayer;
        [Range(0.0f, 1.0f)]
        public float effectIntensity = 0.5f;
        public Color underwaterColor = new Color(0.2f, 0.4f, 0.8f, 0.5f);
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
    }

    public UnderwaterSettings settings = new UnderwaterSettings();
    private UnderwaterRenderPass underwaterPass;
    private RenderTargetHandle waterMaskTexture;

    public override void Create()
    {
        underwaterPass = new UnderwaterRenderPass(settings);
        waterMaskTexture.Init("_WaterMaskTexture");
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        underwaterPass.Setup(waterMaskTexture);
        renderer.EnqueuePass(underwaterPass);
    }

    class UnderwaterRenderPass : ScriptableRenderPass
    {
        private UnderwaterSettings settings;
        private RenderTargetIdentifier colorTarget;
        private RenderTargetHandle waterMaskHandle;
        private Material underwaterMaterial;
        private static readonly string profilerTag = "UnderwaterEffect";

        public UnderwaterRenderPass(UnderwaterSettings settings)
        {
            this.settings = settings;
            renderPassEvent = settings.renderPassEvent;
        }

        public void Setup( RenderTargetHandle waterMaskHandle)
        {
            this.waterMaskHandle = waterMaskHandle;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            // Configure water mask texture
            RenderTextureDescriptor waterMaskDescriptor = cameraTextureDescriptor;
            waterMaskDescriptor.colorFormat = RenderTextureFormat.Depth;
            cmd.GetTemporaryRT(waterMaskHandle.id, waterMaskDescriptor);
        }

        // In the UnderwaterRenderPass class, modify the Execute method:
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (settings.underwaterMaterial == null)
                return;

            CommandBuffer cmd = CommandBufferPool.Get(profilerTag);
            
            colorTarget = renderingData.cameraData.renderer.cameraColorTargetHandle;

            // Create and setup material from the settings
            if (underwaterMaterial == null)
            {
                underwaterMaterial = new Material(settings.underwaterMaterial);
            }
            
            // Set the material properties
            underwaterMaterial.SetFloat("_EffectIntensity", settings.effectIntensity);
            underwaterMaterial.SetColor("_UnderwaterColor", settings.underwaterColor);

            // First render the water objects to a depth texture (containing just the water surface)
            var waterDrawSettings = CreateDrawingSettings(
                new ShaderTagId("UniversalForward"), 
                ref renderingData, 
                SortingCriteria.CommonTransparent
            );
            
            var waterFilterSettings = new FilteringSettings(
                RenderQueueRange.transparent, 
                settings.waterLayer
            );
            
            // Render water to the water mask texture
            cmd.SetRenderTarget(waterMaskHandle.Identifier());
            cmd.ClearRenderTarget(true, true, Color.clear);
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            
            context.DrawRenderers(renderingData.cullResults, ref waterDrawSettings, ref waterFilterSettings);
            
            // FIXED: Use SetGlobalTexture instead of SetTexture
            cmd.SetGlobalTexture("_WaterMaskTex", waterMaskHandle.Identifier());
            
            // Draw underwater effect as a fullscreen pass
            cmd.Blit(colorTarget, colorTarget, underwaterMaterial);
            
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void FrameCleanup(CommandBuffer cmd)
        {
            cmd.ReleaseTemporaryRT(waterMaskHandle.id);
        }
    }
}