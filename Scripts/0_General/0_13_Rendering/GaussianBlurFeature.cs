using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace TN.Rendering
{
    // URP RendererFeature：在 AfterRenderingTransparents 注入的双 Pass 高斯模糊
    public class GaussianBlurFeature : ScriptableRendererFeature
    {
        [System.Serializable]
        public class Settings
        {
            public bool enabled = true;
            [Tooltip("模糊材质（使用 S_Chapter12-GaussianBlur.shader，Pass0=Vertical, Pass1=Horizontal）")]
            public Material blurMaterial;
            [Range(0.5f, 4f)] public float blurSize = 1.0f;
            [Range(1, 8)] public int iterations = 2;
            [Range(1, 4)] public int downsample = 1;
            public RenderPassEvent injectionPoint = RenderPassEvent.AfterRenderingTransparents;
        }

        public Settings settings = new Settings();

        GaussianBlurPass _pass;

        public override void Create()
        {
            _pass = new GaussianBlurPass(settings)
            {
                renderPassEvent = settings.injectionPoint
            };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (!settings.enabled) return;
            if (settings.blurMaterial == null) return;
            if (!renderingData.cameraData.postProcessEnabled) { /* 可选：仅在开启后处理时执行 */ }

            _pass.Setup(renderer);
            renderer.EnqueuePass(_pass);
        }

        class GaussianBlurPass : ScriptableRenderPass
        {
            static readonly int _MainTexId = Shader.PropertyToID("_MainTex");
            static readonly int _MainTex_TexelSizeId = Shader.PropertyToID("_MainTex_TexelSize");

            readonly Settings _settings;
            ScriptableRenderer _renderer;

            RTHandle _tempA;
            RTHandle _tempB;

            public GaussianBlurPass(Settings settings)
            {
                _settings = settings;
            }

            public void Setup(ScriptableRenderer renderer)
            {
                _renderer = renderer;
            }

            public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
            {
                var desc = renderingData.cameraData.cameraTargetDescriptor;
                desc.msaaSamples = 1;
                desc.depthBufferBits = 0;
                desc.width /= Mathf.Max(1, _settings.downsample);
                desc.height /= Mathf.Max(1, _settings.downsample);

                RenderingUtils.ReAllocateIfNeeded(ref _tempA, desc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_GaussianBlurTempA");
                RenderingUtils.ReAllocateIfNeeded(ref _tempB, desc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_GaussianBlurTempB");
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                if (_settings.blurMaterial == null) return;

                var cmd = CommandBufferPool.Get("GaussianBlurPass");
                try
                {
                    var cameraColor = _renderer.cameraColorTargetHandle;

                    // 初始：cameraColor -> _tempA（垂直）
                    SetSourceOnMaterial(cmd, _settings.blurMaterial, cameraColor);
                    _settings.blurMaterial.SetFloat("_BlurSize", _settings.blurSize);
                    Blitter.BlitCameraTexture(cmd, cameraColor, _tempA, _settings.blurMaterial, 0); // Pass0 垂直

                    // 迭代：多次水平/垂直 ping-pong
                    for (int i = 0; i < _settings.iterations; i++)
                    {
                        // A -> B（水平）
                        SetSourceOnMaterial(cmd, _settings.blurMaterial, _tempA);
                        _settings.blurMaterial.SetFloat("_BlurSize", _settings.blurSize);
                        Blitter.BlitCameraTexture(cmd, _tempA, _tempB, _settings.blurMaterial, 1); // Pass1 水平

                        // B -> A（垂直）
                        SetSourceOnMaterial(cmd, _settings.blurMaterial, _tempB);
                        _settings.blurMaterial.SetFloat("_BlurSize", _settings.blurSize);
                        Blitter.BlitCameraTexture(cmd, _tempB, _tempA, _settings.blurMaterial, 0); // Pass0 垂直
                    }

                    // 输出回相机颜色
                    SetSourceOnMaterial(cmd, _settings.blurMaterial, _tempA);
                    Blitter.BlitCameraTexture(cmd, _tempA, cameraColor);

                    context.ExecuteCommandBuffer(cmd);
                }
                finally
                {
                    CommandBufferPool.Release(cmd);
                }
            }

            public override void OnCameraCleanup(CommandBuffer cmd)
            {
                // 使用 RTHandle 无需在此释放；由管线统一回收
            }

            static void SetSourceOnMaterial(CommandBuffer cmd, Material mat, RTHandle source)
            {
                // 显式设置 _MainTex 以及 _MainTex_TexelSize，避免 Blit 纹理名差异导致采样不到
                cmd.SetGlobalTexture(_MainTexId, source);
                if (source.rt != null)
                {
                    mat.SetTexture(_MainTexId, source.rt);
                }

                int w = source.rt != null ? source.rt.width : 1;
                int h = source.rt != null ? source.rt.height : 1;
                var texel = new Vector4(1f / Mathf.Max(1, w), 1f / Mathf.Max(1, h), w, h);
                cmd.SetGlobalVector(_MainTex_TexelSizeId, texel);
            }
        }
    }
}


