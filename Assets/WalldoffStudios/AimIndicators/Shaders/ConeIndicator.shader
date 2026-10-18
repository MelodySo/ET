Shader "WalldoffStudios/ConeIndicator"
{
    Properties
    {
        [Enum(UnityEngine.Rendering.CompareFunction)] _StencilComp("Stencil Comparison", Float) = 8
        [Enum(UnityEngine.Rendering.StencilOp)] _StencilOp("Stencil Operation", Float) = 0
        [Enum(Off,0,On,1)]_ZWrite ("ZWrite", Float) = 1.0
    }
    SubShader
    {
        Tags 
        {
            "RenderType"="Transparent"
            "Queue"="Transparent"
        }
        
        Stencil
		{
			Ref 1
			Comp [_StencilComp]
            Pass [_StencilOp]
		    fail keep
		}			
        
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite [_ZWrite]
        Cull back

        Pass
        {
            CGPROGRAM
            
            #pragma vertex vert
            #pragma fragment frag

            #pragma shader_feature EDGE_ON

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
                float4 settings : TEXCOORD1;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 offsetUV : TEXCOORD1;
            };
            
            #if EDGE_ON
            sampler2D _EdgeTex;
            #endif
            
            sampler2D _MainTex;
            float4 _MainColor;
            float4 _FillColor;
            float _Fill;
            float _Brightness;
            float _Range;
            float _Lengths [361];
            float _DistortionAmount;

            v2f vert (appdata v) 
            {
                v2f o;
                float3 localSpacePos = v.vertex.xyz + v.normal.xyz * _Lengths[v.settings.w] * _Range;
                o.vertex = UnityObjectToClipPos(float4(localSpacePos.xyz, v.vertex.w));
                o.uv = v.uv;
                o.offsetUV = float2(0.0, (v.uv.y + _DistortionAmount) * _Lengths[v.settings.w]);
                
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float4 indicator = tex2D(_MainTex, i.offsetUV);
                                
                const float fillDelta = saturate(sign(_Fill + _DistortionAmount - i.offsetUV.y));
                
                #if EDGE_ON
                float4 edgeAndIndicator = saturate(tex2D(_EdgeTex, i.uv) + indicator);
                float4 filled = edgeAndIndicator * _FillColor;
                float4 unFilled = edgeAndIndicator * _MainColor;
                
                float4 lerpWithEdge = lerp(unFilled, filled, fillDelta) * _Brightness;
                return lerpWithEdge;
                
                #else
                float4 fillColor = (_FillColor * fillDelta) * indicator;
                float4 mainColor = indicator * _MainColor;
                float4 lerpedColor = lerp(mainColor, fillColor, fillDelta)  * _Brightness;
                return lerpedColor;
                #endif
            }
            ENDCG
        }
    }
}
