Shader "Custom/RoundedCornerGradient"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        [Header(Rounded Corner Settings)]
        _CornerRadius ("Corner Radius", Range(0, 500)) = 50
        _CornerSmoothing ("Edge Smoothing", Range(0, 10)) = 1
        
        [Header(Individual Corner Control)]
        [Toggle] _UseIndividualCorners ("Enable Individual Corners", Float) = 0
        _CornerRadii ("Corner Radii (TL TR BL BR)", Vector) = (50, 50, 50, 50)
        
        [Header(Gradient Settings)]
        [Toggle] _UseGradient ("Enable Gradient", Float) = 0
        [KeywordEnum(Linear, Radial, Corner)] _GradientType ("Gradient Type", Float) = 0
        _GradientColor1 ("Gradient Color 1", Color) = (1,1,1,1)
        _GradientColor2 ("Gradient Color 2", Color) = (0,0,0,1)
        
        [Header(Linear Gradient)]
        _GradientAngle ("Angle (Degrees)", Range(0, 360)) = 0
        _GradientOffset ("Offset", Range(-1, 1)) = 0
        
        [Header(Radial Gradient)]
        _GradientCenter ("Center (X, Y)", Vector) = (0.5, 0.5, 0, 0)
        _GradientRadius ("Radius", Range(0, 2)) = 1
        
        [Header(Advanced)]
        [Toggle] _IgnoreImageColor ("Ignore Image Color", Float) = 1
        [KeywordEnum(Replace, Multiply, Overlay)] _GradientBlendMode ("Gradient Blend", Float) = 2
        _Resolution ("Resolution (Width, Height)", Vector) = (512, 512, 0, 0)
        
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
        
        // Unity UI required properties
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
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
            
            // 圆角参数
            float _CornerRadius;
            float _CornerSmoothing;
            float _UseIndividualCorners;
            float4 _CornerRadii;
            
            // 渐变参数
            float _UseGradient;
            float _GradientType;
            fixed4 _GradientColor1;
            fixed4 _GradientColor2;
            float _GradientAngle;
            float _GradientOffset;
            float4 _GradientCenter;
            float _GradientRadius;
            float _GradientBlendMode;
            
            // 通用参数
            float _IgnoreImageColor;
            float4 _Resolution;

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

            // === 圆角SDF函数 ===
            float roundedBoxSDF(float2 centerPos, float2 size, float radius)
            {
                return length(max(abs(centerPos) - size + radius, 0.0)) - radius;
            }
            
            float roundedBoxSDFIndividual(float2 centerPos, float2 size, float4 radii)
            {
                float2 q = abs(centerPos);
                float r;
                
                if (centerPos.x < 0.0)
                {
                    if (centerPos.y > 0.0)
                        r = radii.x; // TopLeft
                    else
                        r = radii.z; // BottomLeft
                }
                else
                {
                    if (centerPos.y > 0.0)
                        r = radii.y; // TopRight
                    else
                        r = radii.w; // BottomRight
                }
                
                return length(max(q - size + r, 0.0)) - r;
            }

            // === 渐变函数 ===
            float linearGradient(float2 uv, float angle, float offset)
            {
                float rad = radians(angle);
                float2 direction = float2(cos(rad), sin(rad));
                float2 centeredUV = (uv - 0.5) * 2.0;
                float projection = dot(centeredUV, direction);
                float t = (projection + 1.0) * 0.5 + offset;
                return saturate(t);
            }

            float radialGradient(float2 uv, float2 center, float radius)
            {
                float dist = distance(uv, center);
                float t = dist / radius;
                return saturate(t);
            }

            float cornerGradient(float2 uv)
            {
                float2 centered = abs(uv - 0.5) * 2.0;
                float t = max(centered.x, centered.y);
                return saturate(t);
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                // 采样纹理
                half4 texColor = tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd;
                
                // === 处理Image颜色 ===
                half4 color;
                if (_IgnoreImageColor > 0.5)
                {
                    color = texColor;
                    color.a *= IN.color.a;
                }
                else
                {
                    color = texColor * IN.color;
                }

                // === 应用渐变 ===
                if (_UseGradient > 0.5)
                {
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
                    
                    // 根据混合模式应用渐变
                    if (_GradientBlendMode < 0.5) // Replace
                    {
                        color.rgb = gradientColor.rgb;
                        color.a *= gradientColor.a;
                    }
                    else if (_GradientBlendMode < 1.5) // Multiply
                    {
                        color *= gradientColor;
                    }
                    else // Overlay
                    {
                        color.rgb *= gradientColor.rgb;
                        color.a *= gradientColor.a;
                    }
                }

                // === 应用圆角 ===
                float2 uv = IN.texcoord;
                float2 size = _Resolution.xy;
                float2 pos = (uv - 0.5) * size;
                float2 halfSize = size * 0.5;
                
                // 计算SDF
                float distance;
                if (_UseIndividualCorners > 0.5)
                {
                    distance = roundedBoxSDFIndividual(pos, halfSize, _CornerRadii);
                }
                else
                {
                    distance = roundedBoxSDF(pos, halfSize, _CornerRadius);
                }
                
                // 应用圆角Alpha
                float cornerAlpha = 1.0 - smoothstep(-_CornerSmoothing, _CornerSmoothing, distance);
                color.a *= cornerAlpha;

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

