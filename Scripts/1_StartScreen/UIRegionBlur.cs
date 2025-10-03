using System.Collections;
using UnityEngine;
using UnityEngine.UI;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

/// <summary>
/// UI 局部模糊：对指定 RawImage 的矩形区域进行屏幕截取并高斯模糊，再回贴到 RawImage。
/// - 兼容 Canvas RenderMode: Screen Space - Overlay / Camera / World
/// - 纯运行时实现，不依赖 AssetDatabase
/// - 使用项目内已存在的高斯模糊 Shader："Unity Shaders Book/Chapter 12/Gaussian Blur"
/// </summary>
public class UIRegionBlur : MonoBehaviour
{
#if ODIN_INSPECTOR
    [BoxGroup("引用"), LabelText("目标RawImage"), Required]
#endif
    [SerializeField] private RawImage targetRawImage;

#if ODIN_INSPECTOR
    [BoxGroup("引用"), LabelText("根Canvas(可选)")]
#endif
    [SerializeField] private Canvas uiRootCanvas;

#if ODIN_INSPECTOR
    [BoxGroup("模糊参数"), LabelText("迭代次数"), MinValue(1)]
#endif
    [SerializeField] private int iterations = 3;

#if ODIN_INSPECTOR
    [BoxGroup("模糊参数"), LabelText("扩散强度"), MinValue(0f)]
#endif
    [SerializeField] private float blurSpread = 2f;

#if ODIN_INSPECTOR
    [BoxGroup("模糊参数"), LabelText("降采样"), MinValue(1)]
#endif
    [SerializeField] private int downSample = 2;

#if ODIN_INSPECTOR
    [BoxGroup("运行"), LabelText("启用时自动执行")]
#endif
    [SerializeField] private bool runOnEnable;

    private Material _mat;               // 高斯模糊材质
    private RenderTexture _lastRT;       // 上一次的结果，用于释放
    private Coroutine _running;

    private void OnEnable()
    {
        if (runOnEnable)
        {
            TriggerBlur();
        }
    }

    private void OnDisable()
    {
        ReleaseLast();
        if (_mat != null)
        {
            DestroyImmediate(_mat);
            _mat = null;
        }
    }

#if ODIN_INSPECTOR
    [Button("执行局部模糊"), PropertySpace(6)]
#endif
    public void TriggerBlur()
    {
        if (_running != null) StopCoroutine(_running);
        _running = StartCoroutine(CaptureAndBlur());
    }

    private IEnumerator CaptureAndBlur()
    {
        if (targetRawImage == null) yield break;

        // 暂时隐藏 RawImage 以免影响截图
        var col = targetRawImage.color;
        float oldA = col.a; col.a = 0f; targetRawImage.color = col;

        // 等待当前帧绘制完成再截屏
        yield return new WaitForEndOfFrame();

        // 计算 RawImage 的屏幕矩形
        Rect screenRect = GetRawImageScreenRect(targetRawImage, uiRootCanvas);
        int width = Mathf.Max(1, Mathf.RoundToInt(screenRect.width));
        int height = Mathf.Max(1, Mathf.RoundToInt(screenRect.height));

        // 截图至纹理
        Texture2D src = new Texture2D(width, height, TextureFormat.RGB24, false);
        src.ReadPixels(new Rect(screenRect.x, screenRect.y, width, height), 0, 0);
        src.Apply(false, true);

        // 初始化材质
        if (_mat == null)
        {
            Shader sh = Shader.Find("Unity Shaders Book/Chapter 12/Gaussian Blur");
            if (sh == null) { Debug.LogError("[UIRegionBlur] 未找到高斯模糊Shader"); yield break; }
            _mat = new Material(sh);
        }

        // 模糊处理（双 Pass 迭代）
        int rtW = Mathf.Max(1, width / downSample);
        int rtH = Mathf.Max(1, height / downSample);

        RenderTexture buffer0 = RenderTexture.GetTemporary(rtW, rtH, 0);
        buffer0.filterMode = FilterMode.Bilinear;
        Graphics.Blit(src, buffer0);

        for (int i = 0; i < iterations; i++)
        {
            _mat.SetFloat("_BlurSize", 1.0f + i * blurSpread);

            var buffer1 = RenderTexture.GetTemporary(rtW, rtH, 0);
            // 垂直
            Graphics.Blit(buffer0, buffer1, _mat, 0);
            RenderTexture.ReleaseTemporary(buffer0);
            buffer0 = buffer1;

            buffer1 = RenderTexture.GetTemporary(rtW, rtH, 0);
            // 水平
            Graphics.Blit(buffer0, buffer1, _mat, 1);
            RenderTexture.ReleaseTemporary(buffer0);
            buffer0 = buffer1;
        }

        // 将结果复制到一个持久的 RenderTexture 并赋给 RawImage
        var result = new RenderTexture(buffer0.descriptor);
        Graphics.Blit(buffer0, result);
        RenderTexture.ReleaseTemporary(buffer0);

        ReleaseLast();
        _lastRT = result;
        targetRawImage.texture = _lastRT;

        // 恢复 RawImage 透明度
        col.a = oldA; targetRawImage.color = col;

        // 清理临时资源
        DestroyImmediate(src);
        _running = null;
    }

    private void ReleaseLast()
    {
        if (_lastRT != null)
        {
            _lastRT.Release();
            DestroyImmediate(_lastRT);
            _lastRT = null;
        }
    }

    private static Rect GetRawImageScreenRect(RawImage raw, Canvas root)
    {
        RectTransform rt = raw.rectTransform;
        Vector3[] corners = new Vector3[4];
        rt.GetWorldCorners(corners);

        Camera cam = null;
        if (root != null && root.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            cam = root.worldCamera;
        }

        Vector2 bl = RectTransformUtility.WorldToScreenPoint(cam, corners[0]);
        Vector2 tr = RectTransformUtility.WorldToScreenPoint(cam, corners[2]);
        float x = bl.x;
        float y = bl.y;
        float w = tr.x - bl.x;
        float h = tr.y - bl.y;
        return new Rect(x, y, w, h);
    }
}


