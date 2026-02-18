Shader "Custom/2D/TreeWindAtlas"
{
    Properties
    {
        [PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
        [MaterialToggle] _ZWrite("ZWrite", Float) = 0
        _Color("Tint", Color) = (1,1,1,1)

        _WindAmplitude("Wind Amplitude", Range(0, 0.5)) = 0.04
        _WindSpeed("Wind Speed", Range(0, 10)) = 2
        _WindFrequency("Wind Frequency", Range(0, 20)) = 6
        _WindDirection("Wind Direction XY", Vector) = (1, 0, 0, 0)
        _WindStartY("Wind Start Y (Local)", Float) = -0.5
        _WindEndY("Wind End Y (Local)", Float) = 0.5
        _TopExponent("Top Weight Exponent", Range(0.1, 4)) = 1.8
        _ObjectPhaseStrength("Object Random Phase", Range(0, 6.28319)) = 3.14159

        [Toggle] _UseAtlasRect("Use Atlas Rect", Float) = 0
        _AtlasRect("Atlas Rect (X,Y,W,H)", Vector) = (0, 0, 1, 1)

        // Legacy properties for graceful fallback compatibility.
        [HideInInspector] PixelSnap("Pixel snap", Float) = 0
        [HideInInspector] _RendererColor("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _AlphaTex("External Alpha", 2D) = "white" {}
        [HideInInspector] _EnableExternalAlpha("Enable External Alpha", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "CanUseSpriteAtlas" = "True"
        }

        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off
        ZWrite [_ZWrite]

        Pass
        {
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            #pragma vertex UnlitVertex
            #pragma fragment UnlitFragment
            #pragma multi_compile_instancing
            #pragma multi_compile _ DEBUG_DISPLAY SKINNED_SPRITE

            struct Attributes
            {
                COMMON_2D_INPUTS
                half4 color : COLOR;
                UNITY_SKINNED_VERTEX_INPUTS
            };

            struct Varyings
            {
                COMMON_2D_OUTPUTS
                half4 color : COLOR;
            };

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/2DCommon.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _WindAmplitude;
                float _WindSpeed;
                float _WindFrequency;
                float4 _WindDirection;
                float _WindStartY;
                float _WindEndY;
                float _TopExponent;
                float _ObjectPhaseStrength;
                float _UseAtlasRect;
                float4 _AtlasRect;
            CBUFFER_END

            float Hash12(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            Varyings UnlitVertex(Attributes input)
            {
                UNITY_SKINNED_VERTEX_COMPUTE(input);
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);

                float heightRange = max(_WindEndY - _WindStartY, 1e-4);
                float top01 = saturate((input.positionOS.y - _WindStartY) / heightRange);
                float topWeight = pow(top01, _TopExponent);

                float2 dir = _WindDirection.xy;
                float dirLen = max(length(dir), 1e-4);
                dir /= dirLen;

                float2 objectXY = float2(unity_ObjectToWorld[0][3], unity_ObjectToWorld[1][3]);
                float objectPhase = Hash12(objectXY) * _ObjectPhaseStrength;
                float wave = sin(_Time.y * _WindSpeed + input.positionOS.y * _WindFrequency + objectPhase);
                float wave2 = sin(_Time.y * (_WindSpeed * 1.37) + input.positionOS.y * (_WindFrequency * 0.61) + objectPhase * 1.73);
                float sway = (wave + wave2 * 0.35) * _WindAmplitude * topWeight;

                input.positionOS.xy += dir * sway;

                Varyings o = CommonUnlitVertex(input);
                o.color = input.color * _Color * unity_SpriteColor;

                if (_UseAtlasRect > 0.5)
                {
                    o.uv = _AtlasRect.xy + input.uv * _AtlasRect.zw;
                }

                return o;
            }

            half4 UnlitFragment(Varyings input) : SV_Target
            {
                return CommonUnlitFragment(input, input.color);
            }
            ENDHLSL
        }
    }
}
