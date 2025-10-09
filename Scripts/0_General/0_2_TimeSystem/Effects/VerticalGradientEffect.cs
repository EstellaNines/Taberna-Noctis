using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 为任意 UI Graphic 提供垂直方向的顶/底颜色渐变（不需要自定义 Shader）。
/// 将该组件挂在含有 Graphic（Image、RawImage、TextMeshProUGUI 的父 Graphic） 的对象上。
/// </summary>
[RequireComponent(typeof(Graphic))]
public class VerticalGradientEffect : BaseMeshEffect
{
    [SerializeField] private Color topColor = new Color(1.000f, 0.976f, 0.659f, 1.000f);
    [SerializeField] private Color bottomColor = new Color(0.635f, 0.957f, 1.000f, 1.000f);
    [SerializeField, Range(0f, 1f)] private float fill = 0f;       // 覆盖进度：0 顶色不覆盖；1 完全覆盖
    [SerializeField, Range(0f, 1f)] private float softness = 0.15f; // 过渡柔和度：0 硬边；1 非常柔

    public Color TopColor
    {
        get => topColor;
        set { topColor = value; graphic?.SetVerticesDirty(); }
    }

    public Color BottomColor
    {
        get => bottomColor;
        set { bottomColor = value; graphic?.SetVerticesDirty(); }
    }

    public float Fill
    {
        get => fill;
        set { fill = Mathf.Clamp01(value); graphic?.SetVerticesDirty(); }
    }

    public float Softness
    {
        get => softness;
        set { softness = Mathf.Clamp01(value); graphic?.SetVerticesDirty(); }
    }

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive() || vh.currentVertCount == 0) return;

        // 计算本地 y 范围
        float minY = float.MaxValue;
        float maxY = float.MinValue;
        UIVertex v = default;
        for (int i = 0; i < vh.currentVertCount; i++)
        {
            vh.PopulateUIVertex(ref v, i);
            minY = Mathf.Min(minY, v.position.y);
            maxY = Mathf.Max(maxY, v.position.y);
        }
        float height = Mathf.Max(0.0001f, maxY - minY);

        // 顶点着色：基于可调“覆盖进度”与“柔化带”进行从上到下的覆盖
        // 规范化坐标：0(bottom) → 1(top)
        // 边界位置：edge = 1 - fill（fill 增大，边界向下移动）
        float f = Mathf.Clamp01(fill);
        float edge = 1f - f;
        float s = Mathf.Clamp01(softness);

        // 极值优化：完全蓝或完全黄，避免起始或结束出现细条色带
        if (f <= 0.0001f)
        {
            TintAll(vh, bottomColor);
            return;
        }
        if (f >= 0.9999f)
        {
            TintAll(vh, topColor);
            return;
        }
        float sLo = Mathf.Clamp01(edge - s * 0.5f);
        float sHi = Mathf.Clamp01(edge + s * 0.5f);

        for (int i = 0; i < vh.currentVertCount; i++)
        {
            vh.PopulateUIVertex(ref v, i);
            float t = (v.position.y - minY) / height; // 0(bottom) → 1(top)
            float k;
            if (s <= 0.0001f)
            {
                // 硬边：边界之上为黄，之下为蓝
                k = t >= edge ? 1f : 0f;
            }
            else
            {
                // 平滑：使用 smoothstep 在 [sLo, sHi] 内过渡
                k = Mathf.InverseLerp(sLo, sHi, t);
                k = k * k * (3f - 2f * k);
            }
            v.color = Color.Lerp(bottomColor, topColor, k);
            vh.SetUIVertex(v, i);
        }
    }

    private static void TintAll(VertexHelper vh, Color c)
    {
        UIVertex v = default;
        for (int i = 0; i < vh.currentVertCount; i++)
        {
            vh.PopulateUIVertex(ref v, i);
            v.color = c;
            vh.SetUIVertex(v, i);
        }
    }
}


