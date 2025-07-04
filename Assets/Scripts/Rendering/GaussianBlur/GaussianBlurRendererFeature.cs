using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Visuals.PostProcessing
{
    public class GaussianBlurRendererFeature : ScriptableRendererFeature
    {
        GaussianBlurRenderPass gaussianBlurRenderPass;
        bool isSetup;
        
        public override void Create()
        {
            // Create the render pass
            gaussianBlurRenderPass = new GaussianBlurRenderPass();
            name = "Gaussian Blur";
        }

        public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
        {
            isSetup = gaussianBlurRenderPass.Setup(renderer);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            // Handles everything to do with inserting passes into URP loop
            // This gets called before the configure and execute method on the render pass
            // Setup the pass
            if (isSetup)
            {
                renderer.EnqueuePass(gaussianBlurRenderPass);
            }
        }
    }
}
