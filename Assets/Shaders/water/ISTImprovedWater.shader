Shader "Custom/URP/ISTImprovedsWater"
{
    Properties
    {
        _ShallowColor ("Shallow Color", Color) = (0.3, 0.65, 0.75, 0.2)
        _DeepColor ("Deep Color", Color) = (0.086, 0.407, 0.7, 0.7)
        _WaterlineColor ("Waterline Color", Color) = (1, 1, 1, 0.9)
        _WaterlineThickness ("Waterline Thickness", Range(0.001, 0.1)) = 0.02
        _WaterlineSharpness ("Waterline Sharpness", Range(1, 100)) = 20
        _WaveSpeed ("Wave Speed", Range(0, 2)) = 0.5
        _WaveAmplitude ("Wave Amplitude", Range(0, 10)) = 0.2
        _WaveFrequency ("Wave Frequency", Range(0, 10)) = 2
        _WaveSmoothness ("Wave Smoothness", Range(0, 1)) = 0.7
        _WaveTurbulence ("Wave Turbulence", Range(0, 1)) = 0.3
        _DepthFade ("Depth Fade", Range(0, 10)) = 3
        _Smoothness ("Smoothness", Range(0, 1)) = 0.8
        _Refraction ("Refraction", Range(0, 1)) = 0.1
        [Normal] _NormalMap ("Normal Map", 2D) = "bump" {}
        _NormalStrength ("Normal Strength", Range(0, 1)) = 0.5
        _NormalTiling ("Normal Tiling", Float) = 1.0
        
        // Foam effect for high amplitude waves
        [Toggle] _EnableStormFoam ("Enable Storm Foam", Float) = 1
        _StormFoamThreshold ("Storm Foam Threshold", Range(0, 10)) = 3.0
        _StormFoamIntensity ("Storm Foam Intensity", Range(0, 5)) = 1.5
        
        // Sprite texture properties
        _SpriteTexture ("Sprite Texture", 2D) = "white" {}
        _SpriteTiling ("Sprite Tiling", Float) = 1.0
        _SpriteSpeed ("Sprite Animation Speed", Range(0, 2)) = 0.3
        _SpriteOpacity ("Sprite Opacity", Range(0, 1)) = 0.8
        _SpriteDistortion ("Sprite Wave Distortion", Range(0, 1)) = 0.5
        
        _InteractiveWaveStrength ("Interactive Wave Strength", Range(0, 1)) = 0.7
        _InteractiveWaveSpeed ("Interactive Wave Speed", Range(0.1, 10.0)) = 3.0
        [HideInInspector] _WaveParticleCount ("Wave Particle Count", Int) = 0
    }
    
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        
        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }
            
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float4 screenPos : TEXCOORD2;
                float3 normalWS : NORMAL;
                float3 tangentWS : TEXCOORD3;
                float3 bitangentWS : TEXCOORD4;
                float fogFactor : TEXCOORD5;
                float3 viewDirWS : TEXCOORD6;
                float2 baseUV : TEXCOORD7;
                float2 spriteUV : TEXCOORD8;  // New UV for sprite texture
            };
            
            TEXTURE2D(_NormalMap);
            SAMPLER(sampler_NormalMap);
            TEXTURE2D(_SpriteTexture);  // Sprite texture declaration
            SAMPLER(sampler_SpriteTexture);
            
            struct WaveParticle
            {
                float2 position;
                float amplitude;
                float wavelength;
            };
            
            StructuredBuffer<float4> _WaveParticles;
            int _WaveParticleCount;
            
            CBUFFER_START(UnityPerMaterial)
                float4 _ShallowColor;
                float4 _DeepColor;
                float4 _WaterlineColor;
                float _WaterlineThickness;
                float _WaterlineSharpness;
                float _WaveSpeed;
                float _WaveAmplitude;
                float _WaveFrequency;
                float _WaveSmoothness;
                float _WaveTurbulence;
                float _DepthFade;
                float _Smoothness;
                float _Refraction;
                float4 _NormalMap_ST;
                float _NormalStrength;
                float _NormalTiling;
                float _EnableStormFoam;
                float _StormFoamThreshold;
                float _StormFoamIntensity;
                float4 _SpriteTexture_ST;  // Sprite texture properties
                float _SpriteTiling;
                float _SpriteSpeed;
                float _SpriteOpacity;
                float _SpriteDistortion;
                float _InteractiveWaveStrength;
                float _InteractiveWaveSpeed;
            CBUFFER_END
            
            float2 seamlessTiling(float2 uv, float scale, float2 offset)
            {
                return frac(uv * scale + offset);
            }
            
            float3 GerstnerWave(float3 worldPosition, float steepness, float wavelength, float speed, float direction)
            {
                direction = direction * 3.14159 / 180;
                float2 d = float2(cos(direction), sin(direction));
                float k = 2 * 3.14159 / wavelength;
                float f = k * (dot(d, worldPosition.xz) - speed * _Time.y);
                
                // Adjust steepness based on amplitude to prevent sharp peaks
                // Higher amplitude waves will have reduced steepness automatically
                float amplitudeScale = 1.0 / (1.0 + _WaveAmplitude * 0.15);
                float a = steepness * amplitudeScale / k;
                
                // Add smoothing coefficient to reduce triangulation at high amplitudes
                float smoothCoeff = saturate(1.0 - (_WaveAmplitude - 2.0) * 0.1);
                smoothCoeff = max(smoothCoeff, 0.4); // Don't let it go below 0.4 to maintain some wave shape
                
                // Apply smoothing to the wave function
                float sinVal = sin(f) * smoothCoeff + sin(f * 0.5) * (1.0 - smoothCoeff);
                float cosVal = cos(f) * smoothCoeff + cos(f * 0.5) * (1.0 - smoothCoeff);
                
                return float3(
                    d.x * a * cosVal,
                    a * sinVal,
                    d.y * a * cosVal
                );
            }
            
            float3 CalculateDynamicWaves(float3 worldPosition)
            {
                float3 displacement = float3(0, 0, 0);
                float2 pos = worldPosition.xz;
                
                for (int i = 0; i < _WaveParticleCount; i++)
                {
                    float2 particlePos = _WaveParticles[i].xy;
                    float amplitude = _WaveParticles[i].z;
                    float wavelength = _WaveParticles[i].w;
                    
                    if (amplitude <= 0.0001 || wavelength <= 0.0001)
                        continue;
                    
                    float dist = distance(pos, particlePos);
                    float maxDist = wavelength * 2;
                    
                    if (dist < maxDist)
                    {
                        float k = 2.0 * 3.14159 / wavelength;
                        float heightVal = amplitude * sin(dist * k - _Time.y * _InteractiveWaveSpeed) * exp(-dist / wavelength);
                        
                        displacement.y += heightVal * _InteractiveWaveStrength;
                        
                        float2 dir = normalize(pos - particlePos);
                        displacement.xz += dir * heightVal * 0.1 * _InteractiveWaveStrength;
                    }
                }
                
                return displacement;
            }
            
            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                
                float3 worldPos = TransformObjectToWorld(input.positionOS.xyz);
                
                float3 worldOriginalPos = worldPos;
                
                float3 waveOffset = 0;
                
                // Create multi-layered waves with improved distribution for high amplitudes
                waveOffset += GerstnerWave(worldPos, _WaveAmplitude * 0.5, 2/_WaveFrequency, _WaveSpeed * 0.8, 0);
                waveOffset += GerstnerWave(worldPos, _WaveAmplitude * 0.25, 4/_WaveFrequency, _WaveSpeed, 30);
                waveOffset += GerstnerWave(worldPos, _WaveAmplitude * 0.125, 8/_WaveFrequency, _WaveSpeed * 1.2, 60);
                
                // Add more high-frequency detail waves that become more prominent in storm conditions
                float stormFactor = saturate((_WaveAmplitude - 1.0) / 9.0); // 0 at amplitude 1, 1 at amplitude 10
                if (stormFactor > 0)
                {
                    // Add more chaotic small waves during stormy conditions
                    waveOffset += GerstnerWave(worldPos, _WaveAmplitude * 0.06 * stormFactor, 16/_WaveFrequency, _WaveSpeed * 0.7, 15) * stormFactor;
                    waveOffset += GerstnerWave(worldPos, _WaveAmplitude * 0.04 * stormFactor, 12/_WaveFrequency, _WaveSpeed * 1.3, 45) * stormFactor;
                    waveOffset += GerstnerWave(worldPos, _WaveAmplitude * 0.03 * stormFactor, 20/_WaveFrequency, _WaveSpeed * 0.9, 75) * stormFactor;
                    
                    // Apply gentle smoothing to avoid sharp triangulation
                    waveOffset.y = waveOffset.y * (0.9 + 0.1 * sin(worldPos.x * 0.1) * sin(worldPos.z * 0.1));
                }
                
                waveOffset += CalculateDynamicWaves(worldPos);
                
                worldPos += waveOffset;
                
                float3 objectPos = TransformWorldToObject(worldPos);
                
                VertexPositionInputs positionInputs = GetVertexPositionInputs(objectPos);
                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.screenPos = ComputeScreenPos(output.positionCS);
                
                output.baseUV = worldOriginalPos.xz * 0.1 * _NormalTiling;
                
                // Calculate sprite UV with distortion based on wave height
                output.spriteUV = worldOriginalPos.xz * 0.1 * _SpriteTiling;
                // Add subtle distortion to sprite based on wave height
                output.spriteUV += waveOffset.xz * _SpriteDistortion;
                
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);
                output.normalWS = normalInputs.normalWS;
                output.tangentWS = normalInputs.tangentWS;
                output.bitangentWS = normalInputs.bitangentWS;
                
                output.uv = TRANSFORM_TEX(input.uv, _NormalMap);
                
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                
                output.viewDirWS = GetWorldSpaceViewDir(output.positionWS);
                
                return output;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                float2 baseUV = input.baseUV;
                
                float2 uv1 = seamlessTiling(baseUV, 1.0, _Time.y * float2(0.02, 0.03) * _WaveSpeed);
                float2 uv2 = seamlessTiling(baseUV, 1.5, _Time.y * float2(-0.03, -0.01) * _WaveSpeed);
                
                float3 normalTS1 = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uv1));
                float3 normalTS2 = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uv2));
                float3 normalTS = normalize(normalTS1 + normalTS2) * float3(_NormalStrength, _NormalStrength, 1);
                
                float3x3 tangentToWorld = float3x3(input.tangentWS, input.bitangentWS, input.normalWS);
                float3 normalWS = mul(normalTS, tangentToWorld);
                normalWS = normalize(normalWS);
                
                float2 screenUV = input.screenPos.xy / input.screenPos.w;
                
                float2 refractionOffset = normalTS.xy * _Refraction * 0.01;
                float2 refractedUV = screenUV + refractionOffset;
                
                float sceneDepth = SampleSceneDepth(refractedUV);
                float linearEyeDepth = LinearEyeDepth(sceneDepth, _ZBufferParams);
                float depthDifference = linearEyeDepth - input.screenPos.w;
                
                float depthFade = saturate(depthDifference / _DepthFade);
                float waterlineEdge = 1 - saturate(pow(abs(depthDifference) / _WaterlineThickness, _WaterlineSharpness));
                
                float4 waterColor = lerp(_ShallowColor, _DeepColor, depthFade);
                
                waterColor = lerp(waterColor, _WaterlineColor, waterlineEdge);
                
                // Calculate storm conditions based on wave amplitude
                float stormIntensity = saturate((_WaveAmplitude - _StormFoamThreshold) / (10.0 - _StormFoamThreshold));
                
                // Sample sprite texture with animation
                float2 spriteUV1 = seamlessTiling(input.spriteUV, 1.0, _Time.y * float2(0.01, 0.02) * _SpriteSpeed);
                float2 spriteUV2 = seamlessTiling(input.spriteUV, 0.7, _Time.y * float2(-0.015, 0.01) * _SpriteSpeed);
                
                half4 spriteColor1 = SAMPLE_TEXTURE2D(_SpriteTexture, sampler_SpriteTexture, spriteUV1);
                half4 spriteColor2 = SAMPLE_TEXTURE2D(_SpriteTexture, sampler_SpriteTexture, spriteUV2);
                half4 spriteColor = (spriteColor1 + spriteColor2) * 0.5;
                
                // Apply turbulence-based foam during stormy conditions
                float foamMask = 0;
                if (_EnableStormFoam > 0.5 && stormIntensity > 0)
                {
                    // Generate foam patterns based on wave height and turbulence
                    float2 foamUV = input.positionWS.xz * 0.2;
                    float noise1 = sin(foamUV.x * 8.3 + _Time.y * 2.0) * cos(foamUV.y * 7.9 + _Time.y * 1.7) * 0.5 + 0.5;
                    float noise2 = sin(foamUV.x * 12.5 - _Time.y * 1.8) * cos(foamUV.y * 10.7 - _Time.y * 2.3) * 0.5 + 0.5;
                    
                    // Calculate wave height contribution
                    float waveContribution = saturate(length(input.positionWS.y - TransformObjectToWorld(float3(0,0,0)).y) * 2.0);
                    
                    // Create dynamic foam pattern
                    foamMask = saturate((noise1 * noise2 * 1.5 - (1.0 - stormIntensity)) + waveContribution * stormIntensity);
                    foamMask = pow(foamMask, 1.0 + (1.0 - stormIntensity) * 2.0) * _StormFoamIntensity;
                    
                    // Add foam to water color (white foam)
                    waterColor.rgb = lerp(waterColor.rgb, float3(1.0, 1.0, 1.0), foamMask * stormIntensity);
                }

                // Apply sprite texture blending to water color (after foam)
                float spriteBlendFactor = spriteColor.a * _SpriteOpacity * (1.0 - foamMask * 0.7);
                waterColor.rgb = lerp(waterColor.rgb, spriteColor.rgb, spriteBlendFactor);
                
                Light mainLight = GetMainLight();
                float3 viewDir = normalize(input.viewDirWS);
                float3 halfDir = normalize(viewDir + mainLight.direction);
                float specularPower = pow(max(dot(normalWS, halfDir), 0.0), 32.0 * _Smoothness);
                float3 specular = mainLight.color * specularPower * _Smoothness;
                
                float fresnel = pow(1.0 - saturate(dot(viewDir, normalWS)), 4);
                waterColor.rgb += specular;
                waterColor.a = saturate(waterColor.a + fresnel * 0.5);
                
                float4 finalColor = waterColor;
                finalColor.rgb = MixFog(finalColor.rgb, input.fogFactor);
                
                return finalColor;
            }
            ENDHLSL
        }
    }
}