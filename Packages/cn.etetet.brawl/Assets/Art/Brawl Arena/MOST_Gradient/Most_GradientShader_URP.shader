// dev by Solo Player.
// This Shader is a part of Main MOST IN ONE Asset
// Check https://assetstore.unity.com/packages/slug/295013

Shader "MOST/MOST_Gradient_URP"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color1 ("Color A", Color) = (1,1,1,1)
        _Color2 ("Color B", Color) = (1,1,1,1)
        [Toggle] _IsGlobal ("Global Y Start Point", Float) = 1.0
        [Toggle] _ScaleIndependent ("Scale Independent", Float) = 1.0
        _Offset("Offset", Float) = 0.0
        _Scale ("Gradient Area Length", Float) = 1.0
        _Angle ("Gradient Angle", Range(0, 360)) = 0.0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "RenderPipeline"="UniversalPipeline"
        }

        LOD 100
        ZWrite On
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Back

        Pass // Main pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float2 uv      : TEXCOORD0;
                float4 pos     : SV_POSITION;
                float3 worldPos: TEXCOORD1;
                float3 localPos: TEXCOORD2;
                half   fogCoord: TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_ST;

            CBUFFER_START(UnityPerMaterial)
                float4 _Color1;
                float4 _Color2;
                float  _IsGlobal;
                float  _ScaleIndependent;
                float  _Offset;
                float  _Scale;
                float  _Angle;
            CBUFFER_END

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                
                o.pos      = TransformObjectToHClip(v.vertex.xyz);
                o.uv       = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldPos = TransformObjectToWorld(v.vertex.xyz);
                o.localPos = v.vertex.xyz;
                o.fogCoord = ComputeFogFactor(o.pos.z);
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                
                float angleRad = radians(_Angle);
                float2x2 rotMatrix = float2x2(
                    cos(angleRad), -sin(angleRad),
                    sin(angleRad),  cos(angleRad)
                );
                
                float3 objectScale = float3(
                    length(float3(unity_ObjectToWorld._m00, unity_ObjectToWorld._m10, unity_ObjectToWorld._m20)),
                    length(float3(unity_ObjectToWorld._m01, unity_ObjectToWorld._m11, unity_ObjectToWorld._m21)),
                    length(float3(unity_ObjectToWorld._m02, unity_ObjectToWorld._m12, unity_ObjectToWorld._m22))
                );
                
                float scale = _ScaleIndependent ? _Scale : _Scale * objectScale.y;

                float2 pos;
                if (_IsGlobal)
                {
                    pos = i.worldPos.xy - float2(0, _Offset);
                }
                else
                {
                    pos = i.localPos.xy * objectScale.y - float2(0, _Offset);
                }

                float2 rotatedPos = mul(rotMatrix, pos);
                float gradientFactor = saturate((rotatedPos.y - scale * 0.5) / -scale);

                half4 gradient = lerp(_Color1, _Color2, gradientFactor);
                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv) * gradient;

                col.rgb = MixFog(col.rgb, i.fogCoord);
                return col;
            }
            ENDHLSL
        }

        Pass // Shadow casting pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float2 uv  : TEXCOORD0;
                float4 pos : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_ST;

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                o.pos = TransformObjectToHClip(v.vertex.xyz);
                o.uv  = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                return 0;
            }
            ENDHLSL
        }
    }
    Fallback Off
}
