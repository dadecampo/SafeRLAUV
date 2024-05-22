Shader "Hidden/ZibraEffects/Liquid/VisualizeSDF"
{
    SubShader
    {
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off

            HLSLPROGRAM
            #pragma multi_compile_local __ HDRP
            #pragma vertex VSMain
            #pragma fragment PSMain
            #pragma target 3.5

            #include "UnityCG.cginc"
            #include "UnityStandardBRDF.cginc"
            #include "UnityImageBasedLighting.cginc"

            #define MAX_VIEWING_DISTANCE 1e8

#ifdef HDRP
            float3 LightDirection;
#endif

            struct VSIn
            {
                uint vertexID : SV_VertexID;
            };

            struct VSOut
            {
                float4 position : POSITION;
                float3 raydir : TEXCOORD1;
                float2 uv : TEXCOORD0;
            };

            struct PSOut
            {
                float4 color : COLOR;
            };

            float4x4 EyeRayCameraCoeficients;
            float2 TextureScale;

            sampler2D SDFRender;

            // built-in Unity sampler name - do not change
            sampler2D _CameraDepthTexture;

            float2 GetFlippedUV(float2 uv)
            {
                if (_ProjectionParams.x > 0)
                    return float2(uv.x, 1 - uv.y);
                return uv;
            }

            // See Raytracing Gems 1 chapter 20.3.2.1
            float3 GetCameraRay(float2 uv)
            {
                float2 c = float2(2.0f * uv.x - 1.0f, -2.0f * uv.y + 1.0f);

                float3 r = EyeRayCameraCoeficients[0].xyz;
                float3 u = EyeRayCameraCoeficients[1].xyz;
                float3 v = EyeRayCameraCoeficients[2].xyz;

                float3 direction = c.x * r + c.y * u + v;
                return normalize(direction);
            }

            VSOut VSMain(VSIn input)
            {
                VSOut output;

                float2 vertexBuffer[4] = {
                    float2(0.0f, 0.0f),
                    float2(0.0f, 1.0f),
                    float2(1.0f, 0.0f),
                    float2(1.0f, 1.0f),
                };
                uint indexBuffer[6] = { 0, 1, 2, 2, 1, 3 };
                uint indexID = indexBuffer[input.vertexID];

                float2 uv = vertexBuffer[indexID];
                float2 flippedUV = GetFlippedUV(uv);

                output.position = float4(2 * flippedUV.x - 1, 1 - 2 * flippedUV.y, 0.5, 1.0);
                output.uv = flippedUV;
                output.raydir = GetCameraRay(uv);

                return output;
            }

            float PositionToDepth(float3 pos)
            {
                float4 clipPos = mul(UNITY_MATRIX_VP, float4(pos, 1));
                return (1.0 / clipPos.w - _ZBufferParams.w) / _ZBufferParams.z; //inverse of linearEyeDepth
            }

            float4 RenderSDFSurface(float3 cameraPos, float3 cameraRay, float2 uv)
            {
                float4 sdfout = tex2D(SDFRender, GetFlippedUV(uv * TextureScale));
                float3 sdfPos = cameraPos + cameraRay * sdfout.w;
                float sdfDepth =  PositionToDepth(sdfPos);

                if (sdfout.w < MAX_VIEWING_DISTANCE)
                {
                       // lighting vectors:
                    float3 worldView = -cameraRay;
            #ifdef HDRP
                    float3 lightDirWorld = LightDirection;
            #else
                    float3 lightDirWorld = normalize(_WorldSpaceLightPos0.xyz);
            #endif

                    float3 normal = sdfout.xyz;
                    half3 h = normalize(lightDirWorld + worldView);
                    float nh = BlinnTerm(normal, h);
                    float nl = DotClamped(normal, lightDirWorld);
                    float nv = dot(normal, worldView); 
                    float rough = 0.55;
                    half V = SmithBeckmannVisibilityTerm(nl, nv, rough);
                    half D = NDFBlinnPhongNormalizedTerm(nh, RoughnessToSpecPower(rough));
                    float spec = (V * D) * (UNITY_PI / 4);
                    spec = max(0, spec * nl);

                    return float4(spec + (normal*0.5 + 0.5)*(dot(lightDirWorld, normal)*0.5 + 0.5), sdfDepth);
                }

                return 0.0;
            }
          
            float4 MinIntersection(float4 a, float4 b)
            {
                return (a.w > b.w) ? a : b;
            }

            PSOut PSMain(VSOut input)
            {
                PSOut output;

                float sceneDepth = tex2D(_CameraDepthTexture, input.uv).x;
#if !defined(UNITY_REVERSED_Z)
                sceneDepth = 1.0 - sceneDepth;
#endif

                float3 cameraPos = _WorldSpaceCameraPos;
                float3 cameraRay = normalize(input.raydir);
                
                float4 intersection = RenderSDFSurface(cameraPos, cameraRay, input.uv);

                if (intersection.w == 0.0)
                {
                    //didn't hit anything
                    discard;
                }

                // Move visualization tiny bit closer to the camera
                // To reduce Z fighting
                const float depthOffset = 0.0001f;

                if (intersection.w + depthOffset < sceneDepth)
                {
                    discard;
                }

                output.color = float4(intersection.rgb, 0.75);
                return output;
            }
            ENDHLSL
        }
    }
}
