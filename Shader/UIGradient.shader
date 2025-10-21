Shader "Custom/UIGradient"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        [Header(Gradient Settings)]
        [KeywordEnum(Linear, Radial, Corner)] _GradientType ("Gradient Type", Float) = 0
        _GradientColor1 ("Color 1 (Start)", Color) = (1,1,1,1)
        _GradientColor2 ("Color 2 (End)", Color) = (0,0,0,1)
        
        [Header(Linear Gradient)]
        _GradientAngle ("Angle (Degrees)", Range(0, 360)) = 0
        _GradientOffset ("Offset", Range(-1, 1)) = 0
        
        [Header(Radial Gradient)]
        _GradientCenter ("Center (X, Y)", Vector) = (0.5, 0.5, 0, 0)
        _GradientRadius ("Radius", Range(0, 2)) = 1
        
        [Header(Advanced)]
        [Toggle] _UseTexture ("Use Texture Color", Float) = 1
        [Toggle] _MultiplyTexture ("Multiply with Texture", Float) = 0
        
        // Unity UI required properties
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;
            
            float _GradientType;
            fixed4 _GradientColor1;
            fixed4 _GradientColor2;
            float _GradientAngle;
            float _GradientOffset;
            float4 _GradientCenter;
            float _GradientRadius;
            float _UseTexture;
            float _MultiplyTexture;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                OUT.color = v.color * _Color;
                return OUT;
            }

            // 线性渐变
            float linearGradient(float2 uv, float angle, float offset)
            {
                // 将角度转换为弧度
                float rad = radians(angle);
                
                // 计算方向向量
                float2 direction = float2(cos(rad), sin(rad));
                
                // 将UV从[0,1]转换到[-1,1]
                float2 centeredUV = (uv - 0.5) * 2.0;
                
                // 计算沿方向的投影
                float projection = dot(centeredUV, direction);
                
                // 归一化到[0,1]并应用偏移
                float t = (projection + 1.0) * 0.5 + offset;
                
                return saturate(t);
            }

            // 径向渐变
            float radialGradient(float2 uv, float2 center, float radius)
            {
                float dist = distance(uv, center);
                float t = dist / radius;
                return saturate(t);
            }

            // 四角渐变
            float cornerGradient(float2 uv)
            {
                // 从四个角向中心渐变
                float2 centered = abs(uv - 0.5) * 2.0;
                float t = max(centered.x, centered.y);
                return saturate(t);
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                // 采样纹理
                half4 texColor = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd);
                
                // 计算渐变值
                float gradientValue;
                
                if (_GradientType < 0.5) // Linear
                {
                    gradientValue = linearGradient(IN.texcoord, _GradientAngle, _GradientOffset);
                }
                else if (_GradientType < 1.5) // Radial
                {
                    gradientValue = radialGradient(IN.texcoord, _GradientCenter.xy, _GradientRadius);
                }
                else // Corner
                {
                    gradientValue = cornerGradient(IN.texcoord);
                }
                
                // 混合渐变颜色
                fixed4 gradientColor = lerp(_GradientColor1, _GradientColor2, gradientValue);
                
                // 应用渐变到最终颜色
                half4 color;
                
                if (_UseTexture > 0.5)
                {
                    if (_MultiplyTexture > 0.5)
                    {
                        // 渐变色与纹理相乘
                        color = texColor * gradientColor * IN.color;
                    }
                    else
                    {
                        // 渐变色叠加到纹理上（保持纹理细节）
                        color = texColor * IN.color;
                        color.rgb *= gradientColor.rgb;
                        color.a *= gradientColor.a;
                    }
                }
                else
                {
                    // 只使用渐变色（忽略纹理颜色，保留alpha）
                    color = gradientColor * IN.color;
                    color.a *= texColor.a;
                }

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip (color.a - 0.001);
                #endif

                return color;
            }
            ENDCG
        }
    }
}

