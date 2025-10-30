using UnityEngine;
using UnityEngine.UI;

namespace TN.UI
{
    /// <summary>
    /// 圆角渐变组合组件 - 同时支持圆角和渐变效果
    /// </summary>
    [RequireComponent(typeof(Image))]
    [ExecuteAlways]  // 在编辑和运行模式下都执行，实时预览
    public class RoundedCornerGradient : MonoBehaviour
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
            Replace,    // 替换
            Multiply,   // 相乘
            Overlay     // 叠加
        }

        #endregion

        #region 序列化字段 - 圆角设置

        [Header("圆角设置")]
        [SerializeField]
        [Tooltip("启用四角独立控制")]
        private bool useIndividualCorners = false;
        
        [SerializeField]
        [Tooltip("圆角半径")]
        [Range(0f, 500f)]
        private float cornerRadius = 50f;
        
        [Header("四角独立半径")]
        [SerializeField]
        [Range(0f, 500f)]
        private float topLeftRadius = 50f;
        
        [SerializeField]
        [Range(0f, 500f)]
        private float topRightRadius = 50f;
        
        [SerializeField]
        [Range(0f, 500f)]
        private float bottomLeftRadius = 50f;
        
        [SerializeField]
        [Range(0f, 500f)]
        private float bottomRightRadius = 50f;

        [SerializeField]
        [Range(0f, 10f)]
        [Tooltip("边缘平滑度")]
        private float edgeSmoothing = 1f;

        #endregion

        #region 序列化字段 - 渐变设置

        [Header("渐变设置")]
        [SerializeField]
        [Tooltip("启用渐变效果")]
        private bool useGradient = false;

        [SerializeField]
        [Tooltip("渐变类型")]
        private GradientType gradientType = GradientType.Linear;

        [SerializeField]
        [Tooltip("起始颜色")]
        private Color startColor = Color.white;

        [SerializeField]
        [Tooltip("结束颜色")]
        private Color endColor = Color.black;

        [Header("线性渐变参数")]
        [SerializeField]
        [Range(0f, 360f)]
        private float gradientAngle = 90f;

        [SerializeField]
        [Range(-1f, 1f)]
        private float gradientOffset = 0f;

        [Header("径向渐变参数")]
        [SerializeField]
        private Vector2 gradientCenter = new Vector2(0.5f, 0.5f);

        [SerializeField]
        [Range(0f, 2f)]
        private float gradientRadius = 1f;

        [SerializeField]
        [Tooltip("渐变混合模式")]
        private BlendMode blendMode = BlendMode.Overlay;

        #endregion

        #region 序列化字段 - 高级设置

        [Header("高级设置")]
        [SerializeField]
        [Tooltip("忽略Image颜色")]
        private bool ignoreImageColor = true;
        
        [SerializeField]
        [Tooltip("自动使用RectTransform尺寸")]
        private bool useRectSize = true;

        [SerializeField]
        private Vector2 manualResolution = new Vector2(512, 512);

        #endregion

        #region 私有字段

        private Image targetImage;
        private Material instanceMaterial;
        private static Shader roundedCornerGradientShader;

        #endregion

        #region Unity生命周期

        private void Awake()
        {
            Initialize();
        }

        private void OnEnable()
        {
            Initialize();
            UpdateMaterialProperties();
        }
        
        private void Update()
        {
            // 在编辑模式下持续更新，确保实时预览
            #if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                UpdateMaterialProperties();
            }
            #endif
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

#if UNITY_EDITOR
        private void OnValidate()
        {
            // 统一模式下同步四个角的值
            if (!useIndividualCorners)
            {
                topLeftRadius = cornerRadius;
                topRightRadius = cornerRadius;
                bottomLeftRadius = cornerRadius;
                bottomRightRadius = cornerRadius;
            }
            
            // 在编辑模式下立即初始化和更新
            if (!Application.isPlaying)
            {
                // 立即初始化
                if (targetImage == null)
                {
                    targetImage = GetComponent<Image>();
                }
                
                if (roundedCornerGradientShader == null)
                {
                    roundedCornerGradientShader = Shader.Find("Custom/RoundedCornerGradient");
                }
                
                if (roundedCornerGradientShader != null && (instanceMaterial == null || instanceMaterial.shader != roundedCornerGradientShader))
                {
                    if (instanceMaterial != null)
                    {
                        DestroyImmediate(instanceMaterial);
                    }
                    instanceMaterial = new Material(roundedCornerGradientShader);
                    instanceMaterial.name = "RoundedCornerGradient_Instance";
                    instanceMaterial.hideFlags = HideFlags.DontSave;
                    
                    if (targetImage != null)
                    {
                        targetImage.material = instanceMaterial;
                    }
                }
                
                // 延迟更新材质属性
                UnityEditor.EditorApplication.delayCall += () =>
                {
                    if (this != null)
                    {
                        UpdateMaterialProperties();
                    }
                };
            }
            else
            {
                UpdateMaterialProperties();
            }
        }
#endif

        #endregion

        #region 公共方法 - 圆角控制

        public void SetCornerRadius(float radius)
        {
            useIndividualCorners = false;
            cornerRadius = Mathf.Max(0, radius);
            topLeftRadius = topRightRadius = bottomLeftRadius = bottomRightRadius = cornerRadius;
            UpdateMaterialProperties();
        }

        public void SetIndividualCornerRadii(float topLeft, float topRight, float bottomLeft, float bottomRight)
        {
            useIndividualCorners = true;
            topLeftRadius = topLeft;
            topRightRadius = topRight;
            bottomLeftRadius = bottomLeft;
            bottomRightRadius = bottomRight;
            UpdateMaterialProperties();
        }

        #endregion

        #region 公共方法 - 渐变控制

        public void SetGradientEnabled(bool enabled)
        {
            useGradient = enabled;
            UpdateMaterialProperties();
        }

        public void SetGradientColors(Color start, Color end)
        {
            startColor = start;
            endColor = end;
            UpdateMaterialProperties();
        }

        public void SetGradientType(GradientType type)
        {
            gradientType = type;
            UpdateMaterialProperties();
        }

        public void SetGradientAngle(float angle)
        {
            gradientAngle = angle % 360f;
            UpdateMaterialProperties();
        }

        #endregion

        #region 公共方法 - 通用

        public void RefreshMaterial()
        {
            Initialize();
            UpdateMaterialProperties();
        }

        #endregion

        #region 私有方法

        private void Initialize()
        {
            if (targetImage == null)
            {
                targetImage = GetComponent<Image>();
            }

            if (roundedCornerGradientShader == null)
            {
                roundedCornerGradientShader = Shader.Find("Custom/RoundedCornerGradient");
                
                if (roundedCornerGradientShader == null)
                {
                    Debug.LogError("[RoundedCornerGradient] 找不到 Custom/RoundedCornerGradient Shader！", this);
                    enabled = false;
                    return;
                }
            }

            if (instanceMaterial == null || instanceMaterial.shader != roundedCornerGradientShader)
            {
                if (instanceMaterial != null)
                {
                    if (Application.isPlaying)
                        Destroy(instanceMaterial);
                    else
                        DestroyImmediate(instanceMaterial);
                }
                
                instanceMaterial = new Material(roundedCornerGradientShader);
                instanceMaterial.name = "RoundedCornerGradient_Instance";
                
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

        private void UpdateMaterialProperties()
        {
            if (instanceMaterial == null || targetImage == null)
            {
                Initialize();
                if (instanceMaterial == null) return;
            }

            // 分辨率
            Vector2 resolution = GetResolution();
            instanceMaterial.SetVector("_Resolution", new Vector4(resolution.x, resolution.y, 0, 0));

            // 圆角设置
            instanceMaterial.SetFloat("_CornerSmoothing", edgeSmoothing);
            instanceMaterial.SetFloat("_UseIndividualCorners", useIndividualCorners ? 1f : 0f);
            
            if (useIndividualCorners)
            {
                instanceMaterial.SetVector("_CornerRadii", 
                    new Vector4(topLeftRadius, topRightRadius, bottomLeftRadius, bottomRightRadius));
            }
            else
            {
                instanceMaterial.SetFloat("_CornerRadius", cornerRadius);
            }

            // 渐变设置
            instanceMaterial.SetFloat("_UseGradient", useGradient ? 1f : 0f);
            
            if (useGradient)
            {
                instanceMaterial.SetFloat("_GradientType", (float)gradientType);
                instanceMaterial.SetColor("_GradientColor1", startColor);
                instanceMaterial.SetColor("_GradientColor2", endColor);
                instanceMaterial.SetFloat("_GradientAngle", gradientAngle);
                instanceMaterial.SetFloat("_GradientOffset", gradientOffset);
                instanceMaterial.SetVector("_GradientCenter", new Vector4(gradientCenter.x, gradientCenter.y, 0, 0));
                instanceMaterial.SetFloat("_GradientRadius", gradientRadius);
                instanceMaterial.SetFloat("_GradientBlendMode", (float)blendMode);
            }

            // 高级设置
            instanceMaterial.SetFloat("_IgnoreImageColor", ignoreImageColor ? 1f : 0f);

            // 纹理
            if (targetImage.sprite != null && instanceMaterial.mainTexture != targetImage.sprite.texture)
            {
                instanceMaterial.mainTexture = targetImage.sprite.texture;
            }
        }

        private Vector2 GetResolution()
        {
            if (useRectSize)
            {
                RectTransform rectTransform = transform as RectTransform;
                if (rectTransform != null)
                {
                    return rectTransform.rect.size;
                }
            }

            if (useRectSize && targetImage != null && targetImage.sprite != null)
            {
                return new Vector2(
                    targetImage.sprite.texture.width,
                    targetImage.sprite.texture.height);
            }

            return manualResolution;
        }

        #endregion

        #region 编辑器辅助

#if UNITY_EDITOR
        [ContextMenu("重置为默认值")]
        private void ResetToDefault()
        {
            // 圆角
            useIndividualCorners = false;
            cornerRadius = 50f;
            topLeftRadius = topRightRadius = bottomLeftRadius = bottomRightRadius = 50f;
            edgeSmoothing = 1f;
            
            // 渐变
            useGradient = false;
            gradientType = GradientType.Linear;
            startColor = Color.white;
            endColor = Color.black;
            gradientAngle = 90f;
            gradientOffset = 0f;
            gradientCenter = new Vector2(0.5f, 0.5f);
            gradientRadius = 1f;
            blendMode = BlendMode.Overlay;
            
            // 高级
            ignoreImageColor = true;
            useRectSize = true;
            manualResolution = new Vector2(512, 512);
            
            UpdateMaterialProperties();
        }
#endif

        #endregion
    }
}


