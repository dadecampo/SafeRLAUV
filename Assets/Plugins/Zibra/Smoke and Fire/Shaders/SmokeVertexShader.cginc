#include "UnityCG.cginc"

struct VSIn
{
    float4 position : POSITION;
    float2 uv : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct VSOut
{
    float4 position : POSITION;
    float2 uv : TEXCOORD0;
#ifdef USING_STEREO_MATRICES
    nointerpolation float4x4 inverse_vp : TEXCOORD1; //no way to get this from unity in stereo mode, so we compute it in the vertex shader
#endif
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

#if defined(INPUT_2D_ARRAY) || defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
Texture2DArray _CameraDepthTexture;
#else
Texture2D _CameraDepthTexture;
#endif
float4 _CameraDepthTexture_TexelSize;

float LoadCameraDepth(float2 pos)
{
#ifdef FLIP_NATIVE_TEXTURES
    pos.y = _CameraDepthTexture_TexelSize.w - pos.y;
#endif
#if defined(INPUT_2D_ARRAY) || defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
    float sceneDepth = _CameraDepthTexture.Load(int4(pos, unity_StereoEyeIndex, 0)).x;
#else
    float sceneDepth = _CameraDepthTexture.Load(int3(pos, 0)).x;
#endif
#if !defined(UNITY_REVERSED_Z)
    sceneDepth = 1.0 - sceneDepth;
#endif
    return sceneDepth;
}

float4 ComputeClipSpacePosition(float2 positionNDC, float deviceDepth)
{
    float4 positionCS = float4(positionNDC * 2.0 - 1.0, deviceDepth, 1.0);
#if UNITY_UV_STARTS_AT_TOP
    positionCS.y = -positionCS.y;
#endif
    return positionCS;
}

float3 SampleWorldPositionFromDepth(float2 uv, float depth, float4x4 inverseVP)
{
    return ClipToWorld(ComputeClipSpacePosition(uv, depth), inverseVP);
}

VSOut VSMain(VSIn input)
{
    VSOut output;

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    float4 position = 2 * input.position;
    output.position = float4(position.xy, 0.5, 1);
    output.uv = float2(input.uv.x, 1 - input.uv.y);
#ifdef FLIP_NATIVE_TEXTURES
    output.uv.y = 1. - output.uv.y;
#endif
    
#ifdef UNITY_STEREO_INSTANCING_ENABLED
    output.inverse_vp = inverse(UNITY_MATRIX_VP);
#endif

    return output;
}