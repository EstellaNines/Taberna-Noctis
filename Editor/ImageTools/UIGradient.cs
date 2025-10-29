using UnityEngine;
using UnityEngine.UI;

namespace TN.UI
{
    /// <summary>
    /// UI渐变色组件 - 为Image添加渐变色效果
    /// </summary>
    [RequireComponent(typeof(Image))]
    [ExecuteInEditMode]
    public class UIGradient : MonoBehaviour
    {
        #region 枚举定义

        public enum GradientType
        {
            Linear,     // 线性渐变
            Radial,     // 径向渐变
            Corner      // 四角渐变
        }

        public enum BlendMode
        {
            Replace,    // 替换纹理颜色
            Multiply,   // 与纹理相乘
            Overlay     // 叠加到纹理上
        }

        #endregion

        #region 序列化字段

        [Header("渐变类型")]
        [SerializeField]
        [Tooltip("渐变类型")]
        private GradientType gradientType = GradientType.Linear;

        [Header("渐变颜色")]
        [SerializeField]
        [Tooltip("起始颜色")]
        private Color startColor = Color.white;

        [SerializeField]
        [Tooltip("结束颜色")]
        private Color endColor = Color.black;

        [Header("线性渐变设置")]
        [SerializeField]
        [Tooltip("渐变角度（度）")]
        [Range(0f, 360f)]
        private float angle = 0f;

        [SerializeField]
        [Tooltip("渐变偏移")]
        [Range(-1f, 1f)]
        private float offset = 0f;

        [Header("径向渐变设置")]
        [SerializeField]
        [Tooltip("渐变中心点")]
        private Vector2 center = new Vector2(0.5f, 0.5f);

        [SerializeField]
        [Tooltip("渐变半径")]
        [Range(0f, 2f)]
        private float radius = 1f;

        [Header("混合模式")]
        [SerializeField]
        [Tooltip("渐变混合模式")]
        private BlendMode blendMode = BlendMode.Overlay;

        [Header("高级设置")]
        [SerializeField]
        [Tooltip("使用Shader渲染（性能更好）")]
        private bool useShader = true;

        [SerializeField]
        [Tooltip("实时更新（编辑器中）")]
        private bool realtimeUpdate = true;

        #endregion

        #region 私有字段

        private Image targetImage;
        private Material instanceMaterial;
        private static Shader gradientShader;
        
        private GradientType lastGradientType;
        private Color lastStartColor;
        private Color lastEndColor;
        private float lastAngle;
        private float lastOffset;
        private Vector2 lastCenter;
        private float lastRadius;
        private BlendMode lastBlendMode;

        #endregion

        #region Unity生命周期

        private void Awake()
        {
            if (Application.isPlaying)
            {
                Initialize();
            }
        }

        private void OnEnable()
        {
            Initialize();
            UpdateGradient();
        }

        private void OnDisable()
        {
            if (instanceMaterial != null && targetImage != null)
            {
                targetImage.material = null;
            }
        }

        private void OnDestroy()
        {
            if (instanceMaterial != null)
            {
                if (Application.isPlaying)
                    Destroy(instanceMaterial);
                else
                    DestroyImmediate(instanceMaterial);
            }
        }

        private void Update()
        {
            if (realtimeUpdate && HasParametersChanged())
            {
                UpdateGradient();
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!Application.isPlaying)
            {
                UnityEditor.EditorApplication.delayCall += () =>
                {
                    if (this != null)
                    {
                        Initialize();
                        UpdateGradient();
                    }
                };
            }
            else
            {
                UpdateGradient();
            }
        }
#endif

        #endregion

        #region 公共方法

        /// <summary>
        /// 设置渐变颜色
        /// </summary>
        public void SetGradientColors(Color start, Color end)
        {
            startColor = start;
            endColor = end;
            UpdateGradient();
        }

        /// <summary>
        /// 设置渐变类型
        /// </summary>
        public void SetGradientType(GradientType type)
        {
            gradientType = type;
            UpdateGradient();
        }

        /// <summary>
        /// 设置线性渐变角度
        /// </summary>
        public void SetAngle(float newAngle)
        {
            angle = newAngle % 360f;
            if (gradientType == GradientType.Linear)
            {
                UpdateGradient();
            }
        }

        /// <summary>
        /// 设置径向渐变中心
        /// </summary>
        public void SetCenter(Vector2 newCenter)
        {
            center = newCenter;
            if (gradientType == GradientType.Radial)
            {
                UpdateGradient();
            }
        }

        /// <summary>
        /// 设置径向渐变半径
        /// </summary>
        public void SetRadius(float newRadius)
        {
            radius = Mathf.Clamp(newRadius, 0f, 2f);
            if (gradientType == GradientType.Radial)
            {
                UpdateGradient();
            }
        }

        /// <summary>
        /// 获取当前渐变角度
        /// </summary>
        public float GetAngle()
        {
            return angle;
        }

        /// <summary>
        /// 获取当前起始颜色
        /// </summary>
        public Color GetStartColor()
        {
            return startColor;
        }

        /// <summary>
        /// 获取当前结束颜色
        /// </summary>
        public Color GetEndColor()
        {
            return endColor;
        }

        /// <summary>
        /// 动画：从当前颜色过渡到目标颜色
        /// </summary>
        public void AnimateToColors(Color targetStart, Color targetEnd, float duration)
        {
            if (Application.isPlaying)
            {
                StartCoroutine(AnimateColorsCoroutine(targetStart, targetEnd, duration));
            }
        }

        /// <summary>
        /// 动画：旋转线性渐变
        /// </summary>
        public void AnimateRotation(float targetAngle, float duration)
        {
            if (Application.isPlaying && gradientType == GradientType.Linear)
            {
                StartCoroutine(AnimateAngleCoroutine(targetAngle, duration));
            }
        }

        /// <summary>
        /// 刷新渐变效果
        /// </summary>
        public void RefreshGradient()
        {
            Initialize();
            UpdateGradient();
        }

        #endregion

        #region 私有方法

        private void Initialize()
        {
            // 获取Image组件
            if (targetImage == null)
            {
                targetImage = GetComponent<Image>();
            }

            if (!useShader)
            {
                return;
            }

            // 加载Shader
            if (gradientShader == null)
            {
                gradientShader = Shader.Find("Custom/UIGradient");
                
                if (gradientShader == null)
                {
                    Debug.LogWarning("[UIGradient] 找不到 Custom/UIGradient Shader！将使用非Shader模式。", this);
                    useShader = false;
                    return;
                }
            }

            // 创建材质实例
            if (instanceMaterial == null || instanceMaterial.shader != gradientShader)
            {
                if (instanceMaterial != null)
                {
                    if (Application.isPlaying)
                        Destroy(instanceMaterial);
                    else
                        DestroyImmediate(instanceMaterial);
                }
                
                instanceMaterial = new Material(gradientShader);
                instanceMaterial.name = "UIGradient_Instance";
                
                if (!Application.isPlaying)
                {
                    instanceMaterial.hideFlags = HideFlags.DontSave;
                }
                
                if (targetImage != null)
                {
                    targetImage.material = instanceMaterial;
                }
            }
        }

        private void UpdateGradient()
        {
            if (targetImage == null)
            {
                Initialize();
                if (targetImage == null) return;
            }

            if (useShader && instanceMaterial != null)
            {
                UpdateShaderGradient();
            }
            else
            {
                UpdateColorGradient();
            }

            // 更新缓存值
            lastGradientType = gradientType;
            lastStartColor = startColor;
            lastEndColor = endColor;
            lastAngle = angle;
            lastOffset = offset;
            lastCenter = center;
            lastRadius = radius;
            lastBlendMode = blendMode;
        }

        private void UpdateShaderGradient()
        {
            if (instanceMaterial == null) return;

            // 设置渐变类型
            instanceMaterial.SetFloat("_GradientType", (float)gradientType);
            
            // 设置渐变颜色
            instanceMaterial.SetColor("_GradientColor1", startColor);
            instanceMaterial.SetColor("_GradientColor2", endColor);
            
            // 设置线性渐变参数
            instanceMaterial.SetFloat("_GradientAngle", angle);
            instanceMaterial.SetFloat("_GradientOffset", offset);
            
            // 设置径向渐变参数
            instanceMaterial.SetVector("_GradientCenter", new Vector4(center.x, center.y, 0, 0));
            instanceMaterial.SetFloat("_GradientRadius", radius);
            
            // 设置混合模式
            switch (blendMode)
            {
                case BlendMode.Replace:
                    instanceMaterial.SetFloat("_UseTexture", 0f);
                    instanceMaterial.SetFloat("_MultiplyTexture", 0f);
                    break;
                case BlendMode.Multiply:
                    instanceMaterial.SetFloat("_UseTexture", 1f);
                    instanceMaterial.SetFloat("_MultiplyTexture", 1f);
                    break;
                case BlendMode.Overlay:
                    instanceMaterial.SetFloat("_UseTexture", 1f);
                    instanceMaterial.SetFloat("_MultiplyTexture", 0f);
                    break;
            }

            // 确保使用正确的纹理
            if (targetImage.sprite != null && instanceMaterial.mainTexture != targetImage.sprite.texture)
            {
                instanceMaterial.mainTexture = targetImage.sprite.texture;
            }
        }

        private void UpdateColorGradient()
        {
            // 简单模式：直接设置Image颜色为渐变中间色
            // 注意：这种模式不能实现真正的渐变效果
            Color blendedColor = Color.Lerp(startColor, endColor, 0.5f);
            targetImage.color = blendedColor;
        }

        private bool HasParametersChanged()
        {
            if (lastGradientType != gradientType) return true;
            if (lastStartColor != startColor) return true;
            if (lastEndColor != endColor) return true;
            if (lastBlendMode != blendMode) return true;

            if (gradientType == GradientType.Linear)
            {
                if (Mathf.Abs(lastAngle - angle) > 0.01f) return true;
                if (Mathf.Abs(lastOffset - offset) > 0.01f) return true;
            }
            else if (gradientType == GradientType.Radial)
            {
                if (Vector2.Distance(lastCenter, center) > 0.01f) return true;
                if (Mathf.Abs(lastRadius - radius) > 0.01f) return true;
            }

            return false;
        }

        #endregion

        #region 协程动画

        private System.Collections.IEnumerator AnimateColorsCoroutine(Color targetStart, Color targetEnd, float duration)
        {
            Color initialStart = startColor;
            Color initialEnd = endColor;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                startColor = Color.Lerp(initialStart, targetStart, t);
                endColor = Color.Lerp(initialEnd, targetEnd, t);
                
                UpdateGradient();
                yield return null;
            }

            startColor = targetStart;
            endColor = targetEnd;
            UpdateGradient();
        }

        private System.Collections.IEnumerator AnimateAngleCoroutine(float targetAngle, float duration)
        {
            float initialAngle = angle;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                angle = Mathf.Lerp(initialAngle, targetAngle, t);
                UpdateGradient();
                yield return null;
            }

            angle = targetAngle;
            UpdateGradient();
        }

        #endregion

        #region 编辑器辅助

#if UNITY_EDITOR
        [ContextMenu("重置为默认值")]
        private void ResetToDefault()
        {
            gradientType = GradientType.Linear;
            startColor = Color.white;
            endColor = Color.black;
            angle = 0f;
            offset = 0f;
            center = new Vector2(0.5f, 0.5f);
            radius = 1f;
            blendMode = BlendMode.Overlay;
            UpdateGradient();
        }

        [ContextMenu("预设：上下渐变")]
        private void PresetVertical()
        {
            gradientType = GradientType.Linear;
            angle = 90f;
            offset = 0f;
            UpdateGradient();
        }

        [ContextMenu("预设：左右渐变")]
        private void PresetHorizontal()
        {
            gradientType = GradientType.Linear;
            angle = 0f;
            offset = 0f;
            UpdateGradient();
        }

        [ContextMenu("预设：径向渐变")]
        private void PresetRadial()
        {
            gradientType = GradientType.Radial;
            center = new Vector2(0.5f, 0.5f);
            radius = 1f;
            UpdateGradient();
        }
#endif

        #endregion
    }
}

