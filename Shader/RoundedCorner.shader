Shader "Custom/RoundedCorner"
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
        
        [Header(Advanced)]
        [Toggle] _IgnoreImageColor ("Ignore Image Color", Float) = 1
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
            
            float _CornerRadius;
            float _CornerSmoothing;
            float4 _Resolution;
            float _UseIndividualCorners;
            float4 _CornerRadii; // x=TopLeft, y=TopRight, z=BottomLeft, w=BottomRight
            float _IgnoreImageColor;

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

            // 计算圆角的SDF（有符号距离场）- 统一圆角
            float roundedBoxSDF(float2 centerPos, float2 size, float radius)
            {
                return length(max(abs(centerPos) - size + radius, 0.0)) - radius;
            }
            
            // 计算圆角的SDF - 支持四个角独立半径
            float roundedBoxSDFIndividual(float2 centerPos, float2 size, float4 radii)
            {
                // 根据象限选择对应的圆角半径
                // radii: x=TopLeft, y=TopRight, z=BottomLeft, w=BottomRight
                float2 q = abs(centerPos);
                float r;
                
                // 判断在哪个象限
                if (centerPos.x < 0.0) // 左侧
                {
                    if (centerPos.y > 0.0) // 上方 -> 左上角
                        r = radii.x;
                    else // 下方 -> 左下角
                        r = radii.z;
                }
                else // 右侧
                {
                    if (centerPos.y > 0.0) // 上方 -> 右上角
                        r = radii.y;
                    else // 下方 -> 右下角
                        r = radii.w;
                }
                
                return length(max(q - size + r, 0.0)) - r;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                // 采样纹理
                half4 texColor = tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd;
                
                // 根据设置决定是否使用Image颜色
                half4 color;
                if (_IgnoreImageColor > 0.5)
                {
                    // 忽略Image颜色，只使用纹理颜色和Image的alpha
                    color = texColor;
                    color.a *= IN.color.a; // 仍然保留Image的alpha控制
                }
                else
                {
                    // 使用Image颜色（标准行为）
                    color = texColor * IN.color;
                }

                // 计算圆角遮罩
                float2 uv = IN.texcoord;
                float2 size = _Resolution.xy;
                
                // 将UV从[0,1]转换到以中心为原点的坐标系
                float2 pos = (uv - 0.5) * size;
                
                // 计算半尺寸
                float2 halfSize = size * 0.5;
                
                // 计算SDF - 根据模式选择
                float distance;
                if (_UseIndividualCorners > 0.5)
                {
                    distance = roundedBoxSDFIndividual(pos, halfSize, _CornerRadii);
                }
                else
                {
                    distance = roundedBoxSDF(pos, halfSize, _CornerRadius);
                }
                
                // 使用smoothstep创建平滑边缘
                float alpha = 1.0 - smoothstep(-_CornerSmoothing, _CornerSmoothing, distance);
                
                // 应用圆角Alpha
                color.a *= alpha;

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

