Shader "Hidden/ZibraEffects/SmokeAndFire/SmokeShadowProjectionShader"
{
    SubShader
    {
        Pass
        {
            Cull Off
            ZWrite Off
            ZTest Always
            
            //multiplicative blending for fake shadows
            Blend DstColor Zero

            HLSLPROGRAM
            #pragma vertex VSMain
            #pragma fragment PSMain
            #pragma multi_compile_local __ HDRP
            #pragma multi_compile_local __ INPUT_2D_ARRAY
            #pragma multi_compile_local __ TRICUBIC
            #pragma multi_compile_local __ FLIP_NATIVE_TEXTURES
            #pragma multi_compile_instancing
            #include <RenderingUtils.cginc>
            #include <SmokeVertexShader.cginc>
       
            float3 SampleShadows(float3 position)
            {
                float simulationScale =
                    (1.0f / 3.0f) * (ContainerScale.x + ContainerScale.y + ContainerScale.z);
             
                float3 lightmapShadow = (IlluminationShadows == 0) ? 1.0 : GetLightmapShadow(position);
#ifdef TRICUBIC
                float shadowSample = SampleShadowmapSmooth(position, simulationScale); //way smoother, but also 8 times slower
#else
                float shadowSample = SampleShadowmap(position, simulationScale);
#endif
                float3 primaryShadow = (PrimaryShadows == 0) ? 1.0 : exp(-ShadowColor * shadowSample);
                float3 shadow = lightmapShadow * primaryShadow;
                return shadow;
            }

            float4 PSMain(VSOut input) : SV_Target
            {
                float4 color;

                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

#ifdef USING_STEREO_MATRICES
                float4x4 inverseVP = input.inverse_vp;
#else
                float4x4 inverseVP = ViewProjectionInverse;
#endif

                float2 uv = input.uv;

                float deviceDepth = LoadCameraDepth(input.position.xy); 
                float3 scenePos = SampleWorldPositionFromDepth(uv, deviceDepth, inverseVP);

                float3 shadow = SampleShadows(scenePos);
                
                return float4(lerp(1.0, shadow, FakeShadows), 1.0);
            }
            ENDHLSL
        }
    }
}
