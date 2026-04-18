Shader "UI/RadialGradient"
{
    Properties
    {
        _CenterColor ("Center Color", Color) = (0.239, 0.102, 0.431, 1)
        _EdgeColor   ("Edge Color",   Color) = (0.051, 0.020, 0.129, 1)
        _Radius      ("Radius",   Range(0.1, 2.0)) = 0.7
        _Softness    ("Softness", Range(0.01, 2.0)) = 0.5

        // Unity UI internals — keep for Canvas Image compatibility
        [HideInInspector] _MainTex          ("Texture",           2D)    = "white" {}
        [HideInInspector] _StencilComp      ("Stencil Comp",      Float) = 8
        [HideInInspector] _Stencil          ("Stencil ID",        Float) = 0
        [HideInInspector] _StencilOp        ("Stencil Op",        Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask",Float) = 255
        [HideInInspector] _StencilReadMask  ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask        ("Color Mask",        Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue"          = "Background"
            "RenderType"     = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest LEqual
        Blend Off

        Pass
        {
            Name "RadialGradient"
            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma target   2.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _CenterColor;
                half4 _EdgeColor;
                half  _Radius;
                half  _Softness;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv          = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Remap UV 0→1 to -1→1 so centre of quad = (0,0)
                float2 centred = IN.uv * 2.0 - 1.0;
                float  dist    = length(centred);
                float  t       = smoothstep(_Radius - _Softness, _Radius + _Softness, dist);
                return lerp(_CenterColor, _EdgeColor, t);
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/InternalErrorShader"
}
