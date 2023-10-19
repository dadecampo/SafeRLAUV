Shader "Hidden/ZibraEffects/SmokeAndFire/SmokeShader"
{
    SubShader
    {
        Pass
        {
            Cull Off
            ZWrite Off
            ZTest Always
            
            Blend One OneMinusSrcAlpha

            HLSLPROGRAM

            #pragma multi_compile_local __ HDRP
            #pragma multi_compile_local __ INPUT_2D_ARRAY
            #pragma multi_compile_local __ FLIP_NATIVE_TEXTURES
            #pragma multi_compile_instancing
            #pragma vertex VSMain
            #pragma fragment PSMain

#ifdef HDRP
            Texture2D<float2> _CameraExposureTexture;
#endif

            sampler2D ParticlesTex;
            float2 TextureScale;
            float DownscaleFactor;
            
            #include <RenderingUtils.cginc>
            #include <SmokeVertexShader.cginc>
    
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

                //pixel coordinates for dithering
                int3 pixelCoord = int3(input.position.xy, 0);
                int timeSeed = int(_Time.y * 44328);
                DitherValues = (BlueNoise.Load(uint3(pixelCoord + int3(timeSeed, timeSeed, 0)) % 1024u) - 0.5);

                float3 rayOrigin, rayEnd;
                GetCameraRay(uv, rayOrigin, rayEnd, inverseVP);

                //initialize the ray
                float3 cameraPos = rayOrigin;
                float3 cameraRay = normalize(rayEnd - rayOrigin);

                float deviceDepth = LoadCameraDepth(input.position.xy / DownscaleFactor);
                float3 scenePos = SampleWorldPositionFromDepth(uv, deviceDepth, inverseVP);
           
                RayProperties prop = {float3(1.0, 1.0, 1.0), float3(0.0, 0.0, 0.0)};
                TraceRay(cameraPos, cameraRay, distance(cameraPos, scenePos.xyz), prop);

                color = float4(prop.incoming, 1.0 - Sum(prop.absorption)/3.0);

#ifdef HDRP
                color.xyz *= _CameraExposureTexture[int2(0, 0)].x;
#endif

                // dont render particles with stereo rendering
#ifndef UNITY_STEREO_INSTANCING_ENABLED
                // sample ParticleTex at uv
                color.xyz += tex2D(ParticlesTex, uv * TextureScale).xyz;
#endif

                return color;
            }
            ENDHLSL
        }
    }
}
