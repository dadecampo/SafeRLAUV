Shader "Hidden/ZibraEffects/Liquid/HeightmapBlit"
{
    Properties
    {
        _MainTex("Texture", any) = "" {} 
    } 

    SubShader
    {
        Pass
        {
            ZTest Always
            Cull Off
            ZWrite Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            UNITY_DECLARE_SCREENSPACE_TEXTURE(_MainTex);
            uniform float4 _MainTex_ST;
            uniform float4 _HeightmapScaling;
            uniform int _ComponentToBlit;

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 texcoord : TEXCOORD0;
            };

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = TRANSFORM_TEX(v.texcoord.xy, _MainTex);
                float2 screenPos = o.vertex.xy / o.vertex.w;
                o.vertex.xy = ((screenPos + 1.0 + 2.0*float2( _HeightmapScaling.x, _HeightmapScaling.w - 1 - _HeightmapScaling.y)) / _HeightmapScaling.zw - 1.0) * o.vertex.w;
                return o;
            }

            fixed4 frag(v2f i): SV_Target
            {
                return UNITY_SAMPLE_SCREENSPACE_TEXTURE(_MainTex, i.texcoord)[_ComponentToBlit];
            }
            ENDCG
        }
    }
    Fallback Off
}