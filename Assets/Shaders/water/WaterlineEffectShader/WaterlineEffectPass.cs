using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RenderingSystems.PP
{
    public class WaterlineEffectPass : ScriptableRenderPass
    {
        private Material waterlineMaterial;
        private RenderTargetIdentifier cameraColorTargetIdentifier;
        private RenderTargetHandle tempRenderTarget;
        private string waterPlaneTag;
        private float effectIntensity;
        private float distortionAmount;
        private float distortionSpeed;
        private Color underwaterColor;
        private float fogDensity;
        private float fogStart;
        private float fogEnd;
        private string profilerTag;
        
        public WaterlineEffectPass(string tag, WaterlineSettings settings)
        {
            profilerTag = tag;
            renderPassEvent = settings.Event;
            waterlineMaterial = settings.waterlineEffectMaterial;
            waterPlaneTag = settings.waterPlaneTag;
            effectIntensity = settings.underwaterEffectIntensity;
            distortionAmount = settings.waterDistorsionAmount;
            distortionSpeed = settings.waterDistorsionSpeed;
            underwaterColor = settings.underwaterColor;
            fogDensity = settings.fogDensity;
            fogStart = settings.fogStart;
            fogEnd = settings.fogEnd;
            tempRenderTarget.Init("_TempWaterlineTarget");
        }
        
        public void Setup(RenderTargetIdentifier cameraColorTarget)
        {
            cameraColorTargetIdentifier = cameraColorTarget;
        }
        
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get(profilerTag);
            
            // Store water plane position in screen space
            Camera camera = renderingData.cameraData.camera;
            GameObject waterPlane = GameObject.FindGameObjectWithTag(waterPlaneTag);
            
            if (waterPlane == null)
            {
                CommandBufferPool.Release(cmd);
                return;
            }

            cameraColorTargetIdentifier = renderingData.cameraData.renderer.cameraColorTarget;
            
            // Calculate water surface position in screen space
            Vector3 waterPosition = waterPlane.transform.position;
            Vector3 waterScreenPos = camera.WorldToScreenPoint(waterPosition);
            
            waterlineMaterial.SetFloat("_WaterlineY", waterScreenPos.y / camera.pixelHeight);
            waterlineMaterial.SetFloat("_CameraY", camera.transform.position.y);
            waterlineMaterial.SetFloat("_EffectIntensity", effectIntensity);
            waterlineMaterial.SetColor("_UnderwaterColor", underwaterColor);
            waterlineMaterial.SetFloat("_DistortionAmount", distortionAmount);
            waterlineMaterial.SetFloat("_DistortionSpeed", distortionSpeed);
            waterlineMaterial.SetFloat("_FogDensity", fogDensity);
            waterlineMaterial.SetFloat("_FogStart", fogStart);
            waterlineMaterial.SetFloat("_FogEnd", fogEnd);

            if (renderingData.cameraData.requiresDepthTexture)
            {
                ConfigureInput(ScriptableRenderPassInput.Depth);
            }
            
            RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
            descriptor.depthBufferBits = 0;
            
            cmd.GetTemporaryRT(tempRenderTarget.id, descriptor, FilterMode.Bilinear);
            
            // Blit to temp RT with the waterline effect material
            cmd.Blit(cameraColorTargetIdentifier, tempRenderTarget.Identifier(), waterlineMaterial);
            cmd.Blit(tempRenderTarget.Identifier(), cameraColorTargetIdentifier);
            
            cmd.ReleaseTemporaryRT(tempRenderTarget.id);
            
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}