Shader "Hidden/ZibraEffects/Liquid/LiquidMeshShader"
{
    SubShader
    {
        Pass
        {
            Cull Off
            ZTest Always
            ZWrite Off

            HLSLPROGRAM

            #pragma multi_compile_local __ HDRP
            #pragma multi_compile_local __ CUSTOM_REFLECTION_PROBE
            #pragma multi_compile_local __ USE_CUBEMAP_REFRACTION
            #pragma multi_compile_local __ FLIP_NATIVE_TEXTURES FLIP_BACKGROUND_TEXTURE
            #pragma multi_compile_local __ FLIP_PARTICLES_TEXTURE
            #pragma multi_compile_local __ UNDERWATER_RENDER
            #pragma multi_compile_local __ RAYMARCH_DISABLED
            #pragma multi_compile_local __ FOAM_DISABLED
            #pragma vertex VSMain
            #pragma fragment PSMain
            #pragma target 3.5
            #include "UnityCG.cginc"
            #include "UnityStandardBRDF.cginc"
            #include "UnityImageBasedLighting.cginc"
            
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

            // Fluid material parameters, see SetMaterialParams()
            float4x4 ProjectionInverse;
            float4x4 ViewProjectionInverse;
            float4x4 EyeRayCameraCoeficients;
            float Roughness;
            float AbsorptionAmount;
            float ScatteringAmount;
            float RefractionDistortion;
            float4 RefractionColor;
            float4 ReflectionColor;
            float4 EmissiveColor;
            float Metalness;
            float3 GridSize;
            float3 ContainerScale;
            float3 ContainerPosition;
            float LiquidIOR;

#ifdef HDRP
            float3 LightColor; 
            float3 LightDirection;
#endif

            // Light and reflection params
            UNITY_DECLARE_TEXCUBE(ReflectionProbe);
            float4 ReflectionProbe_BoxMax;
            float4 ReflectionProbe_BoxMin;
            float4 ReflectionProbe_ProbePosition;
            float4 ReflectionProbe_HDR;
            float4 WorldSpaceLightPos;
            
            UNITY_DECLARE_TEXCUBE(RefractionProbe);
            
            // Camera params
            float2 TextureScale;
            float RenderPipelineScale;

            float4 Background_TexelSize;
            float2 GetFlippedUV(float2 uv)
            {
                if (_ProjectionParams.x > 0)
                    return float2(uv.x, 1 - uv.y);
                return uv;
            }

            float2 GetFlippedUVBackground(float2 uv)
            {
                uv = GetFlippedUV(uv);
#ifdef FLIP_BACKGROUND_TEXTURE
                // Temporary fix for flipped reflection on iOS
                uv.y = 1 - uv.y;
#else
                if (Background_TexelSize.y < 0)
                {
                    uv.y = 1 - uv.y;
                }
#endif
                return uv;
            }

            float2 GetFlippedEffectParticlesUV(float2 uv)
            {
#ifdef FLIP_PARTICLES_TEXTURE
                uv.y = 1 - uv.y;
#endif
                return uv;
            }

            // Input resources
            Texture2D<float4> Background;
            SamplerState samplerBackground;
            float4 FetchBackground(float2 uv)
            {
                return Background.Sample(samplerBackground, GetFlippedUVBackground(uv));
            }

            StructuredBuffer<int> Quads;
            StructuredBuffer<int> VertexIDGrid;
            StructuredBuffer<float4> Vertices;

            // built-in Unity sampler name - do not change
            Texture2D<float4> _CameraDepthTexture;
            SamplerState sampler_CameraDepthTexture;
            float FetchCameraDepth(float2 uv)
            {
#ifdef HDRP
                float depth = _CameraDepthTexture.Load(uint3(GetFlippedUV(uv) * RenderPipelineScale * _ScreenParams.xy, 0)).r;
#else
                float depth = _CameraDepthTexture.Sample(sampler_CameraDepthTexture, GetFlippedUV(uv)).x;
#endif
#if !defined(UNITY_REVERSED_Z)
                depth = 1.0 - depth;
#endif
                return depth;
            }

            float4 ComputeClipSpacePosition(float2 positionNDC, float deviceDepth)
            {
                #if !UNITY_REVERSED_Z
                deviceDepth = 1 - deviceDepth;
                deviceDepth = lerp(UNITY_NEAR_CLIP_VALUE, 1, deviceDepth);
                #endif


                float4 positionCS = float4(positionNDC * 2.0 - 1.0, deviceDepth, 1.0);

                positionCS.y = -positionCS.y;

                return positionCS;
            }

            float3 ComputeWorldSpacePosition(float2 positionNDC, float deviceDepth, float4x4 invViewProjMatrix)
            {
                float4 positionCS  = ComputeClipSpacePosition(positionNDC, deviceDepth);
                float4 hpositionWS = mul(invViewProjMatrix, positionCS);
                return hpositionWS.xyz / hpositionWS.w;
            }

            float3 DepthToWorld(float2 uv, float depth)
            {
                return ComputeWorldSpacePosition(uv, depth, ViewProjectionInverse);
            }

            float4 GetDepthAndPos(float2 uv)
            {
                float depth = FetchCameraDepth(uv);
                float3 pos = DepthToWorld(uv, depth);
                return float4(pos, depth);
            }

            float PositionToDepth(float3 pos)
            {
                float4 clipPos = mul(UNITY_MATRIX_VP, float4(pos, 1));
                return (1.0 / clipPos.w - _ZBufferParams.w) / _ZBufferParams.z; //inverse of linearEyeDepth
            }

            float3 PositionToScreen(float3 pos)
            {

                float4 clipPos = mul(UNITY_MATRIX_VP, float4(pos, 1));

                #if !UNITY_UV_STARTS_AT_TOP
                clipPos.y = -clipPos.y;
                #endif

                clipPos = ComputeScreenPos(clipPos);
                return float3(clipPos.xy/clipPos.w, (1.0 / clipPos.w - _ZBufferParams.w) / _ZBufferParams.z); 
            }

            float3 BoxProjection(float3 rayOrigin, float3 rayDir, float3 cubemapPosition, float3 boxMin, float3 boxMax)
            {
                float3 tMin = (boxMin - rayOrigin) / rayDir;
                float3 tMax = (boxMax - rayOrigin) / rayDir;
                float3 t1 = min(tMin, tMax);
                float3 t2 = max(tMin, tMax);
                float tFar = min(min(t2.x, t2.y), t2.z);
                return normalize(rayOrigin + rayDir*tFar - cubemapPosition);
            };
            
            float3 SampleCubemap(float3 pos, float3 ray, float roughness)
            {
                Unity_GlossyEnvironmentData g;
                g.roughness = roughness;

#if defined(CUSTOM_REFLECTION_PROBE) || defined(HDRP)
                g.reflUVW = BoxProjection(pos, ray,
                    ReflectionProbe_ProbePosition,
                    ReflectionProbe_BoxMin, ReflectionProbe_BoxMax
                );
                float3 reflection = Unity_GlossyEnvironment(UNITY_PASS_TEXCUBE(ReflectionProbe), ReflectionProbe_HDR, g);
#else
                g.reflUVW = ray;
                g.reflUVW.y = g.reflUVW.y; //don't render the bottom part of the cubemap
                float3 reflection = Unity_GlossyEnvironment(UNITY_PASS_TEXCUBE(unity_SpecCube0), unity_SpecCube0_HDR, g);
#endif

                return reflection;
            }

            float3 ComputeMaterial(float3 cameraPos, float3 cameraRay, float3 normal, float3 lightDirection, float3 lightColor, float roughness = 0.05)
            {
                float3 worldView = -cameraRay;
                float4 reflColor = ReflectionColor;
                float3 H = normalize(lightDirection + worldView);
                float NH = BlinnTerm(normal, H);
                float NL = DotClamped(normal, lightDirection);
                float NV  = abs(dot(normal, worldView)); 
                half V = SmithBeckmannVisibilityTerm(NL, NV, roughness);
                half D = NDFBlinnPhongNormalizedTerm(NH, RoughnessToSpecPower(roughness));
                float3 spec = reflColor.xyz * (V * D) * (UNITY_PI / 4);
                return lightColor * max(0, spec * NL);
            }

            float Average(float3 x)
            {
                return (x.x + x.y + x.z) / 3.0;
            }

            float RefractionMinimumDepth;
            float RefractionDepthBias;

            float3 RefractSample(float3 pos, float3 ray, float2 uv)
            {
                #ifdef USE_CUBEMAP_REFRACTION
                return SampleCubemap(pos, ray, 0.05);
                #endif

                
                #ifdef FLIP_NATIVE_TEXTURES
                uv.y = 1 - uv.y;
                #endif
                float3 uvz = PositionToScreen(pos);
                if (any(uvz.xy < 0.0f) || any(uvz.xy > 1.0f))
                {
                    uvz.xy = uv;
                }
                return FetchBackground(uvz.xy).xyz;
            }

            float3 ReflectSample(float3 pos, float3 ray, float roughness = 0.05)
            {
                return Average(ReflectionColor.xyz) * SampleCubemap(pos, ray, roughness);
            }

            #define SHADING

            #include <RenderingUtils.cginc>

            // See Raytracing Gems 1 20.3.2.1 EYE RAY SETUP
            float3 GetCameraRay(float2 uv)
            {
#ifdef FLIP_NATIVE_TEXTURES
                uv.y = 1 - uv.y;
#elif !UNITY_UV_STARTS_AT_TOP
                uv.y = 1 - uv.y;
#endif

                float2 c = float2(2.0f * uv.x - 1.0f, -2.0f * uv.y + 1.0f);

                float3 r = EyeRayCameraCoeficients[0].xyz;
                float3 u = EyeRayCameraCoeficients[1].xyz;
                float3 v = EyeRayCameraCoeficients[2].xyz;

                float3 direction = c.x * r + c.y * u + v;
                return normalize(direction);
            }

            float2 Resolution;

            uint getHashPixelID(uint2 pix)
            {
                pix = clamp(pix, 0, uint2(Resolution.xy - 1));
                return pix.x + uint(Resolution.x) * pix.y;
            }

#ifndef FOAM_DISABLED
            Texture2D<float4> ParticlesTex;
            SamplerState sampler_ParticlesTex;
#endif

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

                output.position = float4(2 * uv.x - 1, 1 - 2 * uv.y, 0.5, 1.0);
                output.uv = uv;
                output.raydir = GetCameraRay(uv);
                
                return output;
            }

            float4 MeshRenderData_TexelSize;
            Texture2D<float4> MeshRenderData;
            float4 FetchMeshRenderData(int2 cords)
            {
#if UNITY_UV_STARTS_AT_TOP
                if (_ProjectionParams.x > 0)
                {
                    cords.y = MeshRenderData_TexelSize.w - cords.y;
                }
#endif
                return MeshRenderData.Load(int3(cords, 0));
            }

            Texture2D<float> MeshDepth;
            float FetchMeshDepth(int2 cords)
            {
#if UNITY_UV_STARTS_AT_TOP
                if (_ProjectionParams.x > 0)
                {
                    cords.y = MeshRenderData_TexelSize.w - cords.y;
                }
#endif
                float depth = MeshDepth.Load(int3(cords, 0));
#if !defined(UNITY_REVERSED_Z)
                depth = 1.0 - depth;
#endif
                return depth;
            }
            
            float RayMarchResolutionDownscale;

#ifndef RAYMARCH_DISABLED
            Texture2D<float4> RayMarchData;
            float4 RayMarchData_TexelSize;

            float4 FetchRayMarchData(int2 cords)
            {
#ifdef FLIP_NATIVE_TEXTURES
                cords.y = RayMarchData_TexelSize.w * RayMarchResolutionDownscale  - cords.y;
#else
                if (RayMarchData_TexelSize.y < 0)
                {
                    cords.y = RayMarchData_TexelSize.w * RayMarchResolutionDownscale - cords.y;
                }
#endif
                return RayMarchData.Load(int3(cords, 0));
            }

            Texture2D<float4> MaterialData;
            SamplerState samplerMaterialData;
            float4 MaterialData_TexelSize;

            float4 FetchMaterialData(float2 uv)
            {
#ifdef FLIP_NATIVE_TEXTURES
                uv.y = RayMarchResolutionDownscale - uv.y;
#else
                if (MaterialData_TexelSize.y < 0)
                {
                    uv.y = RayMarchResolutionDownscale - uv.y;
                }
#endif
                return MaterialData.Sample(samplerMaterialData, uv);
            }

            //required to work with custom depth formats
            LightPath GetLiquidDepth(float2 pixelCoord)
            {
                float2 rayMarchPixel = pixelCoord * RayMarchResolutionDownscale;

                LightPath path;

                //custom bilinear interpolation
                float2 intPixel = floor(rayMarchPixel);
                float2 fracPixel = frac(rayMarchPixel);

                float3 data00 = FetchRayMarchData(int3(intPixel, 0) + int3(0,0,0)).xyz;
                float3 data10 = FetchRayMarchData(int3(intPixel, 0) + int3(1,0,0)).xyz;
                data00 = lerp(data00, data10, fracPixel.x);

                float3 data01 = FetchRayMarchData(int3(intPixel, 0) + int3(0,1,0)).xyz;
                float3 data11 = FetchRayMarchData(int3(intPixel, 0) + int3(1,1,0)).xyz;
                data01 = lerp(data01, data11, fracPixel.x);

                path.depth = lerp(data00, data01, fracPixel.y);
                path.material = FetchMaterialData(rayMarchPixel/MeshRenderData_TexelSize.zw).xyz;

                return path;
            }
#endif
            
            PSOut PSMain(VSOut input)
            {
                PSOut output;

                float3 cameraPos = _WorldSpaceCameraPos;
                float3 cameraRay = normalize(input.raydir);
                int3 pixelCoord = int3(input.position.xy, 0);
                float4 data = FetchMeshRenderData(pixelCoord.xy);
                uint encodedNormal = asuint(data.w);

                float liquidDepth = FetchMeshDepth(pixelCoord.xy);
                float sceneDepth = FetchCameraDepth(input.uv);

#ifdef FOAM_DISABLED
                float3 foamParticles = 0.;
#else
                float3 foamParticles =
                    ParticlesTex
                        .Sample(sampler_ParticlesTex, GetFlippedEffectParticlesUV(input.uv * TextureScale))
                        .x;
#endif          
                bool hasParticles = any(foamParticles > 0.0);

                float3 incomingLight = foamParticles;
                bool hasFluid = true;
                if (!any(data.xyz) && !encodedNormal)
                {
                    hasFluid = false;
                    if (!hasParticles)
                        discard;
                }
#ifndef UNDERWATER_RENDER
                if (liquidDepth < sceneDepth)
                {
                    hasFluid = false;
                    if (!hasParticles)
                        discard;
                }
#endif
#ifndef RAYMARCH_DISABLED
                LightPath path = GetLiquidDepth(pixelCoord.xy);
                RayDepths = path.depth;
                Material = path.material;
#endif
                Depths = RayDepths;
                float3 normal = DecodeDirection(asuint(encodedNormal));
                #ifdef FLIP_NATIVE_TEXTURES
                float3 worldPos = DepthToWorld(float2(input.uv.x, 1 - input.uv.y), liquidDepth);
                #else
                float3 worldPos = DepthToWorld(input.uv, liquidDepth);
                #endif

                float4 weights = MaterialWeights(Material.xyz);

                float3 materialEmission = Material1Emission* weights.x + Material2Emission * weights.y +
                                          Material3Emission * weights.z + EmissiveColor.rgb * weights.w;

                float materialMetalness = Sum(float4(MatMetalness, Metalness) * weights);

                float materialRoughness = Sum(float4(MatRoughness, Roughness) * weights);

                
                float ndotv = dot(normal, -cameraRay);
                float NV = abs(ndotv); 
                float fresnel = FresnelTermLiquid(materialMetalness, NV);
#ifdef HDRP
                float3 lightColor = LightColor;
                float3 lightDirWorld = LightDirection;
#else
                float3 lightColor = _LightColor0;
                float3 lightDirWorld = normalize(_WorldSpaceLightPos0.xyz);
#endif

#ifdef UNDERWATER_RENDER
                float CameraDensity = 0.0f;
                if(insideGrid(cameraPos))
                CameraDensity = SampleDensity(cameraPos);
                bool isUnderwater = (step(ndotv, 0.0)) && (CameraDensity > 0.0);
                if (!isUnderwater && liquidDepth < sceneDepth)
                {
                    hasFluid = false;
                    if (!hasParticles)
                        discard;
                }
#endif

                if (hasFluid)
                {
#ifdef UNDERWATER_RENDER
                    if (isUnderwater)
                    {
                        float3 background_color = 0.0f;
                        float opticalDensity = 0.0f;
                        if (liquidDepth < sceneDepth)
                        {
                            background_color = FetchBackground(input.uv).xyz;
                            liquidDepth = sceneDepth;
                            worldPos = DepthToWorld(input.uv, liquidDepth);
                        }
                        else
                        {
#ifndef RAYMARCH_DISABLED
                            background_color =
                                RefractionRay(worldPos, cameraRay, -normal, input.uv, true);
#else
                            background_color = RefractionColor;
#endif
                        }

                        float liquidWorldSpaceDepth = length(cameraPos - worldPos);
                        opticalDensity += liquidWorldSpaceDepth;

#ifdef HDRP
                        float3 lightColor = LightColor;
#else
                        float3 lightColor = _LightColor0;
#endif

                        incomingLight = IntegrateAbsorptionScattering(opticalDensity,
                                                                      background_color, lightColor);
                    }
                    else
#endif
                {
                    ////
                    ////compute reflected color
                    ////

                    float3 ReflectRay = reflect(cameraRay, normal);
                    float3 ReflectedColor = ReflectSample(worldPos, ReflectRay, materialRoughness);
                    incomingLight += ReflectionColor.xyz * fresnel * ReflectedColor / Average(ReflectionColor.xyz);

                    ////
                    ////compute light from light sources
                    ////

                    incomingLight += fresnel*ComputeMaterial(cameraPos, cameraRay, normal, lightDirWorld, lightColor, materialRoughness);
                    incomingLight += materialEmission;

                    ////
                    ////compute refracted color
                    ////
#ifndef RAYMARCH_DISABLED
                        incomingLight += (1.0 - fresnel) * RefractionRay(worldPos, cameraRay,
                                                                         normal, input.uv, false);
#else
                        incomingLight += (1.0 - fresnel) * RefractionColor;
#endif
                    }
                }
                else
                {
                    incomingLight += FetchBackground(input.uv).xyz;
                }


                output.color = float4(clamp(incomingLight , 0., 10000.0), 1.0);

                return output;
            }
            ENDHLSL
        }
    }
}
