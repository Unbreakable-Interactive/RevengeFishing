using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Visuals.PostProcessing
{
    public class GaussianBlurRenderPass : ScriptableRenderPass
    {
        private Material material;
        private GaussianBlurSettings blurSettings;

        private RenderTargetIdentifier source; // Basically screen texture before applying shader
        private RenderTargetHandle blurTex; // Intermediate tex that holds the result of the first blur pass
        private int blurTexID;

        public bool Setup(ScriptableRenderer renderer)
        {
            // Camera output texture
            source = renderer.cameraColorTarget;
            blurSettings = VolumeManager.instance.stack.GetComponent<GaussianBlurSettings>();
            // When it applies the effect
            renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;

            if (blurSettings != null && blurSettings.IsActive())
            {
                material = new Material(Shader.Find("PostProcessing/GaussianBlur"));
                return true;
            }

            return false;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            // Get calls each frame before applying effect to setup temp resources
            // cmd holds a list of instruction for the gpu to carry out
            // the renderTexDesc describe the size of and sort of info stored in the texture
            if (blurSettings == null || !blurSettings.IsActive())
            {
                return;
            }

            blurTexID = Shader.PropertyToID("_GaussianBlurTex");
            blurTex = new ();
            blurTex.id = blurTexID;
            cmd.GetTemporaryRT(blurTex.id, cameraTextureDescriptor);

            base.Configure(cmd, cameraTextureDescriptor);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            // core of the code, runs once per frame and it applies the effect
            // context passes the data to the renderer
            if (blurSettings == null || !blurSettings.IsActive())
            {
                return;
            }

            // Check that the camera is not base camera
            /* if (renderingData.cameraData.renderType == CameraRenderType.Base)
            {
                return;
            }
            var source2 = renderingData.cameraData.renderer.cameraColorTarget; */
            CommandBuffer cmd = CommandBufferPool.Get("Gaussian Blur");

            int gridSize = Mathf.CeilToInt(blurSettings.strength.value * 6f);

            if (gridSize % 2 == 0)
            {
                gridSize++;
            }

            material.SetInteger("_GridSize", gridSize);
            material.SetFloat("_Spread", blurSettings.strength.value);

            // Execute effect using effect mat with two passes
            cmd.Blit(source, blurTex.id, material, 0);
            cmd.Blit(blurTex.id, source, material, 1);
            context.ExecuteCommandBuffer(cmd);

            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }

        public override void FrameCleanup(CommandBuffer cmd)
        {
            // Called at the end of the frame to clean up temp resources
            cmd.ReleaseTemporaryRT(blurTexID);
            
            base.FrameCleanup(cmd);
        }
    }
}
/*
namespace Visuals.PostProcessing
{
    public class MotionBlurRenderPass : ScriptableRenderPass
    {
        private Material material;
        private MotionBlurSettings blurSettings;

        private RenderTargetIdentifier source; // Basically screen texture before applying shader
        private RTHandle blurTex; // Intermediate tex that holds the result of the first blur pass
        private int blurTexID;

        public bool Setup(ScriptableRenderer renderer)
        {
            // Camera output texture
            source = renderer.cameraColorTargetHandle;
            blurSettings = VolumeManager.instance.stack.GetComponent<MotionBlurSettings>();
            // When it applies the effect
            renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;

            if (blurSettings != null && blurSettings.IsActive())
            {
                material = new Material(Shader.Find("PostProcessing/MotionBlur"));
                return true;
            }

            return false;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            // Get calls each frame before applying effect to setup temp resources
            // cmd holds a list of instruction for the gpu to carry out
            // the renderTexDesc describe the size of and sort of info stored in the texture
            if (blurSettings == null || !blurSettings.IsActive())
            {
                return;
            }

            blurTexID = Shader.PropertyToID("_MotionBlurTex");
            blurTex = RTHandles.Alloc(cameraTextureDescriptor.width, cameraTextureDescriptor.height);
            cmd.GetTemporaryRT(blurTex.GetInstanceID(), cameraTextureDescriptor);

            base.Configure(cmd, cameraTextureDescriptor);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            // core of the code, runs once per frame and it applies the effect
            // context passes the data to the renderer
            if (blurSettings == null || !blurSettings.IsActive())
            {
                return;
            }

            CommandBuffer cmd = CommandBufferPool.Get("Motion Blur");

            int gridSize = Mathf.CeilToInt(blurSettings.strength.value * 6f);

            if (gridSize % 2 == 0)
            {
                gridSize++;
            }

            material.SetInteger("_GridSize", gridSize);
            material.SetFloat("_Spread", blurSettings.strength.value);

            // Execute effect using effect mat with two passes
            cmd.Blit(source, blurTex.GetInstanceID(), material, 0);
            cmd.Blit(blurTex.GetInstanceID(), source, material, 1);
            context.ExecuteCommandBuffer(cmd);

            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }

        public override void FrameCleanup(CommandBuffer cmd)
        {
            // Called at the end of the frame to clean up temp resources
            cmd.ReleaseTemporaryRT(blurTexID);
            
            base.FrameCleanup(cmd);
        }
    }
}
*/