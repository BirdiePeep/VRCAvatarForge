Shader "Hidden/EasyAvatarSetup/BlitWithMask"
{
    Properties
    {
        _MainTex ("Texture", any) = "" {}
        _MaskTex ("Mask", any) = "white" {}
        _Color("Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _SrcMode ("SrcMode", Float) = 0
	    _DstMode ("DstMode", Float) = 0
        _IsNormalMap ("IsNormalMap", Int) = 0
    }
    SubShader
    {
        Pass
        {
            ZTest Always
            Cull Off
            ZWrite Off
            Blend [_SrcMode] [_DstMode]

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            UNITY_DECLARE_SCREENSPACE_TEXTURE(_MainTex);
            UNITY_DECLARE_SCREENSPACE_TEXTURE(_MaskTex);
            uniform float4 _MainTex_ST;
            uniform float4 _Color;
            uniform int _IsNormalMap;

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = TRANSFORM_TEX(v.texcoord.xy, _MainTex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float alpha = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_MaskTex, i.texcoord).a;
                float4 mainColor = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_MainTex, i.texcoord);
                if (_IsNormalMap == 1)
                {
                    mainColor.rgb = UnpackNormal(mainColor) * 0.5 + 0.5;
                    mainColor.a = alpha;
                }
                else
                    mainColor *= _Color * float4(1, 1, 1, alpha);
                return mainColor;
            }
            ENDCG
        }
    }
Fallback Off
}