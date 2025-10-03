Shader "Unity Shaders Book/Chapter 12/Gaussian Blur" {
    Properties {
        _MainTex ("Base (RGB)", 2D) = "white" {}
        _BlurSize ("Blur Size", Float) = 1.0
    }
    SubShader {
        // CGINCLUDE类似于C++中头文件的功能。由于高斯模糊需要定义两个Pass，但它们使用的片元着色器代码
        // 是完全相同的，使用CGINCLUDE可以避免我们编写两个完全一样的frag函数。
        CGINCLUDE
        
        #include "UnityCG.cginc"
        
        sampler2D _MainTex;  
        half4 _MainTex_TexelSize;
        float _BlurSize;
          
        struct v2f {
            float4 pos : SV_POSITION;
            half2 uv[5]: TEXCOORD0;
        };
          
        v2f vertBlurVertical(appdata_img v) {
            v2f o;
            o.pos = UnityObjectToClipPos(v.vertex);
            
            half2 uv = v.texcoord;
            // 数组的第一个坐标存储了当前的采样纹理，而剩余的四个坐标则是高斯模糊中对邻域采样时使用的纹理坐标。
            // 我们还和属性_BlurSize相乘来控制采样距离。
            o.uv[0] = uv;
            o.uv[1] = uv + float2(0.0, _MainTex_TexelSize.y * 1.0) * _BlurSize;
            o.uv[2] = uv - float2(0.0, _MainTex_TexelSize.y * 1.0) * _BlurSize;
            o.uv[3] = uv + float2(0.0, _MainTex_TexelSize.y * 2.0) * _BlurSize;
            o.uv[4] = uv - float2(0.0, _MainTex_TexelSize.y * 2.0) * _BlurSize;
                     
            return o;
        }
        
        v2f vertBlurHorizontal(appdata_img v) {
            v2f o;
            o.pos = UnityObjectToClipPos(v.vertex);
            
            half2 uv = v.texcoord;
            
            o.uv[0] = uv;
            o.uv[1] = uv + float2(_MainTex_TexelSize.x * 1.0, 0.0) * _BlurSize;
            o.uv[2] = uv - float2(_MainTex_TexelSize.x * 1.0, 0.0) * _BlurSize;
            o.uv[3] = uv + float2(_MainTex_TexelSize.x * 2.0, 0.0) * _BlurSize;
            o.uv[4] = uv - float2(_MainTex_TexelSize.x * 2.0, 0.0) * _BlurSize;
                     
            return o;
        }
        
        fixed4 fragBlur(v2f i) : SV_Target {
            float weight[3] = {0.4026, 0.2442, 0.0545};
            
            fixed3 sum = tex2D(_MainTex, i.uv[0]).rgb * weight[0];
            
            for (int it = 1; it < 3; it++) {
                sum += tex2D(_MainTex, i.uv[it*2-1]).rgb * weight[it];
                sum += tex2D(_MainTex, i.uv[it*2]).rgb * weight[it];
            }
            
            return fixed4(sum, 1.0);
        }
            
        ENDCG
        
        ZTest Always Cull Off ZWrite Off
        
        Pass {
            //定义名字，可以在其他shader中使用该pass
            NAME "GAUSSIAN_BLUR_VERTICAL"
            
            CGPROGRAM
              
            #pragma vertex vertBlurVertical  
            #pragma fragment fragBlur
              
            ENDCG  
        }
        
        Pass {  
            NAME "GAUSSIAN_BLUR_HORIZONTAL"
            
            CGPROGRAM  
            
            #pragma vertex vertBlurHorizontal  
            #pragma fragment fragBlur
            
            ENDCG
        }
    } 
    FallBack "Diffuse"
}


