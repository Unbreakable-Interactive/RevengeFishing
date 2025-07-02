using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RenderingSystems.PP
{
    public class WaterlineEffectRenderFeature : ScriptableRendererFeature
    {
        public WaterlineSettings settings = new WaterlineSettings();
        private WaterlineEffectPass waterlinePass;
        
        public override void Create()
        {
            waterlinePass = new WaterlineEffectPass("Waterline Effect Pass", settings);
        }
        
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (settings.waterlineEffectMaterial == null)
            {
                Debug.LogWarning("Waterline Effect Material is null. Skipping pass.");
                return;
            }
            
            renderer.EnqueuePass(waterlinePass);
        }
    }

    [System.Serializable]
    public class WaterlineSettings
    {
        public RenderPassEvent Event = RenderPassEvent.AfterRenderingTransparents;
        public Material waterlineEffectMaterial = null;
        public string waterPlaneTag = "WaterSurface";
        [Range(0, 1)] public float underwaterEffectIntensity = 0.5f;
        [Range(0, 10)] public float waterDistorsionSpeed = 2.0f;
        [Range(0, 0.1f)] public float waterDistorsionAmount = 0.02f;
        public Color underwaterColor = new (0, 0.4f, 0.7f, 1);
        [Range(0, 1)] public float fogDensity = 0.1f;
        public float fogStart = 0;
        public float fogEnd = 50;
    }
}