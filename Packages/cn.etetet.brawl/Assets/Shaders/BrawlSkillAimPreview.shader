Shader "Brawl/SkillAimPreview"
{
    Properties
    {
        _Color ("Color", Color) = (0.8,0.95,1,1)
        _Shape ("Shape: Rectangle 1 / Sector 2 / Circle 3", Float) = 2
        _Range ("Range / Radius (m)", Float) = 1
        _Width ("Rectangle Width (m)", Float) = 1
        _HalfAngle ("Sector Half Angle (rad)", Float) = 0.2617994
        _BorderWidth ("Border Width (m)", Float) = 0.06
        _FillAlpha ("Fill Alpha", Range(0,1)) = 0.14
        _BorderAlpha ("Border Alpha", Range(0,1)) = 0.85
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent+10" }
        Pass
        {
            Name "SkillAim"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Off
            Offset -1, -1
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _Shape, _Range, _Width, _HalfAngle;
                float _BorderWidth, _FillAlpha, _BorderAlpha;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 plane : TEXCOORD0; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.plane = input.positionOS.xz;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float2 p = input.plane;
                float radius = length(p);
                float distanceToEdge;
                if (_Shape < 1.5)
                    distanceToEdge = min(_Width * 0.5 - abs(p.x), min(p.y, _Range - p.y));
                else
                {
                    distanceToEdge = _Range - radius;
                    if (_Shape < 2.5 && _HalfAngle < 3.141592)
                    {
                        float angle = abs(atan2(p.x, p.y));
                        // 带符号角距离负责裁剪，近边缘换算成米，保持边线宽度。
                        float sideDistance = (_HalfAngle - angle) * max(radius, 0.0001);
                        distanceToEdge = min(distanceToEdge, sideDistance);
                    }
                }
                float aa = max(fwidth(distanceToEdge), 0.001);
                float coverage = smoothstep(-aa, aa, distanceToEdge);
                clip(coverage - 0.001);
                float border = 1 - smoothstep(_BorderWidth - aa, _BorderWidth + aa, distanceToEdge);
                return half4(_Color.rgb, _Color.a * lerp(_FillAlpha, _BorderAlpha, border) * coverage);
            }
            ENDHLSL
        }
    }
}
