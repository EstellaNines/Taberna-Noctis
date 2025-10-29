using UnityEngine;
using UnityEngine.UI;

namespace TN.UI
{
    /// <summary>
    /// 圆角Image组件 - 为UI Image添加实时可调的圆角效果
    /// </summary>
    [RequireComponent(typeof(Image))]
    [ExecuteAlways]  // 在编辑和运行模式下都执行，实时预览
    public class RoundedCornerImage : MonoBehaviour
    {
        #region 序列化字段

        [Header("圆角设置")]
        [SerializeField]
        [Tooltip("启用四角独立控制")]
        private bool useIndividualCorners = false;
        
        [SerializeField]
        [Tooltip("圆角半径（像素）- 统一模式")]
        [Range(0f, 500f)]
        private float cornerRadius = 50f;
        
        [Header("四角独立半径")]
        [SerializeField]
        [Tooltip("左上角半径")]
        [Range(0f, 500f)]
        private float topLeftRadius = 50f;
        
        [SerializeField]
        [Tooltip("右上角半径")]
        [Range(0f, 500f)]
        private float topRightRadius = 50f;
        
        [SerializeField]
        [Tooltip("左下角半径")]
        [Range(0f, 500f)]
        private float bottomLeftRadius = 50f;
        
        [SerializeField]
        [Tooltip("右下角半径")]
        [Range(0f, 500f)]
        private float bottomRightRadius = 50f;

        [Header("其他设置")]
        [SerializeField]
        [Tooltip("边缘平滑度")]
        [Range(0f, 10f)]
        private float edgeSmoothing = 1f;

        [Header("高级设置")]
        [SerializeField]
        [Tooltip("忽略Image组件的颜色设置（推荐开启，确保圆角效果一致）")]
        private bool ignoreImageColor = true;
        
        [SerializeField]
        [Tooltip("自动将Image颜色设置为白色（避免颜色影响）")]
        private bool autoSetWhiteColor = true;
        
        [SerializeField]
        [Tooltip("是否自动使用Image的RectTransform尺寸")]
        private bool useRectSize = true;

        [SerializeField]
        [Tooltip("手动设置的分辨率（当useRectSize为false时使用）")]
        private Vector2 manualResolution = new Vector2(512, 512);

        #endregion

        #region 私有字段

        private Image targetImage;
        private Material instanceMaterial;
        private static Shader roundedCornerShader;
        
        private bool lastUseIndividualCorners;
        private float lastCornerRadius;
        private float lastTopLeftRadius;
        private float lastTopRightRadius;
        private float lastBottomLeftRadius;
        private float lastBottomRightRadius;
        private float lastEdgeSmoothing;
        private Vector2 lastResolution;

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
            // 检查参数是否变化
            if (HasParametersChanged())
            {
                UpdateMaterialProperties();
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // 统一模式下，当cornerRadius改变时同步更新四个独立角的值
            if (!useIndividualCorners)
            {
                topLeftRadius = cornerRadius;
                topRightRadius = cornerRadius;
                bottomLeftRadius = cornerRadius;
                bottomRightRadius = cornerRadius;
            }
            
            // 确保在编辑器模式下也能初始化
            if (!Application.isPlaying)
            {
                // 立即初始化，不延迟
                if (targetImage == null)
                {
                    targetImage = GetComponent<Image>();
                }
                
                if (roundedCornerShader == null)
                {
                    roundedCornerShader = Shader.Find("Custom/RoundedCorner");
                }
                
                if (roundedCornerShader != null && (instanceMaterial == null || instanceMaterial.shader != roundedCornerShader))
                {
                    if (instanceMaterial != null)
                    {
                        DestroyImmediate(instanceMaterial);
                    }
                    instanceMaterial = new Material(roundedCornerShader);
                    instanceMaterial.name = "RoundedCorner_Instance";
                    instanceMaterial.hideFlags = HideFlags.DontSave;
                    
                    if (targetImage != null)
                    {
                        targetImage.material = instanceMaterial;
                    }
                }
            }
            
            // 编辑器中参数改变时更新
            if (Application.isPlaying)
            {
                UpdateMaterialProperties();
            }
            else
            {
                UnityEditor.EditorApplication.delayCall += () =>
                {
                    if (this != null)
                    {
                        UpdateMaterialProperties();
                    }
                };
            }
        }
#endif

        #endregion

        #region 公共方法

        /// <summary>
        /// 设置圆角半径（统一模式）
        /// </summary>
        public void SetCornerRadius(float radius)
        {
            useIndividualCorners = false;
            cornerRadius = Mathf.Max(0, radius);
            
            // 同步更新四个独立角的值
            topLeftRadius = cornerRadius;
            topRightRadius = cornerRadius;
            bottomLeftRadius = cornerRadius;
            bottomRightRadius = cornerRadius;
            
            UpdateMaterialProperties();
        }

        /// <summary>
        /// 设置四个角的半径（独立模式）
        /// </summary>
        public void SetIndividualCornerRadii(float topLeft, float topRight, float bottomLeft, float bottomRight)
        {
            useIndividualCorners = true;
            topLeftRadius = Mathf.Max(0, topLeft);
            topRightRadius = Mathf.Max(0, topRight);
            bottomLeftRadius = Mathf.Max(0, bottomLeft);
            bottomRightRadius = Mathf.Max(0, bottomRight);
            UpdateMaterialProperties();
        }

        /// <summary>
        /// 设置边缘平滑度
        /// </summary>
        public void SetEdgeSmoothing(float smoothing)
        {
            edgeSmoothing = Mathf.Max(0, smoothing);
            UpdateMaterialProperties();
        }

        /// <summary>
        /// 设置分辨率
        /// </summary>
        public void SetResolution(Vector2 resolution)
        {
            useRectSize = false;
            manualResolution = resolution;
            UpdateMaterialProperties();
        }

        /// <summary>
        /// 启用/禁用四角独立控制
        /// </summary>
        public void SetUseIndividualCorners(bool enable)
        {
            useIndividualCorners = enable;
            UpdateMaterialProperties();
        }

        /// <summary>
        /// 启用自动使用RectTransform尺寸
        /// </summary>
        public void EnableAutoRectSize()
        {
            useRectSize = true;
            UpdateMaterialProperties();
        }

        /// <summary>
        /// 刷新材质
        /// </summary>
        public void RefreshMaterial()
        {
            Initialize();
            UpdateMaterialProperties();
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

            // 加载Shader
            if (roundedCornerShader == null)
            {
                roundedCornerShader = Shader.Find("Custom/RoundedCorner");
                
                if (roundedCornerShader == null)
                {
                    Debug.LogError("[RoundedCornerImage] 找不到 Custom/RoundedCorner Shader！请确保Shader文件存在于 Assets/Shader/RoundedCorner.shader", this);
                    enabled = false;
                    return;
                }
            }

            // 创建材质实例
            if (instanceMaterial == null || instanceMaterial.shader != roundedCornerShader)
            {
                if (instanceMaterial != null)
                {
                    if (Application.isPlaying)
                        Destroy(instanceMaterial);
                    else
                        DestroyImmediate(instanceMaterial);
                }
                
                instanceMaterial = new Material(roundedCornerShader);
                instanceMaterial.name = "RoundedCorner_Instance";
                
                if (!Application.isPlaying)
                {
                    instanceMaterial.hideFlags = HideFlags.DontSave;
                }
                
                if (targetImage != null)
                {
                    targetImage.material = instanceMaterial;
                }
            }
            else if (targetImage != null && targetImage.material != instanceMaterial)
            {
                // 确保Material已应用
                targetImage.material = instanceMaterial;
            }
        }

        private void UpdateMaterialProperties()
        {
            if (instanceMaterial == null || targetImage == null)
            {
                Initialize();
                if (instanceMaterial == null) return;
            }

            // 自动设置Image颜色为白色（避免颜色污染）
            if (autoSetWhiteColor && targetImage != null)
            {
                if (targetImage.color != Color.white)
                {
                    targetImage.color = Color.white;
#if UNITY_EDITOR
                    if (!Application.isPlaying)
                    {
                        UnityEditor.EditorUtility.SetDirty(targetImage);
                    }
#endif
                }
            }

            // 获取分辨率
            Vector2 resolution = GetResolution();

            // 更新材质属性
            instanceMaterial.SetFloat("_CornerSmoothing", edgeSmoothing);
            instanceMaterial.SetVector("_Resolution", new Vector4(resolution.x, resolution.y, 0, 0));
            
            // 设置是否忽略Image颜色
            instanceMaterial.SetFloat("_IgnoreImageColor", ignoreImageColor ? 1f : 0f);
            
            // 设置圆角模式
            if (useIndividualCorners)
            {
                instanceMaterial.SetFloat("_UseIndividualCorners", 1f);
                instanceMaterial.SetVector("_CornerRadii", 
                    new Vector4(topLeftRadius, topRightRadius, bottomLeftRadius, bottomRightRadius));
            }
            else
            {
                instanceMaterial.SetFloat("_UseIndividualCorners", 0f);
                instanceMaterial.SetFloat("_CornerRadius", cornerRadius);
            }

            // 确保使用正确的纹理
            if (targetImage.sprite != null && instanceMaterial.mainTexture != targetImage.sprite.texture)
            {
                instanceMaterial.mainTexture = targetImage.sprite.texture;
            }

            // 更新缓存值
            lastUseIndividualCorners = useIndividualCorners;
            lastCornerRadius = cornerRadius;
            lastTopLeftRadius = topLeftRadius;
            lastTopRightRadius = topRightRadius;
            lastBottomLeftRadius = bottomLeftRadius;
            lastBottomRightRadius = bottomRightRadius;
            lastEdgeSmoothing = edgeSmoothing;
            lastResolution = resolution;
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

            // 如果启用自动尺寸但无法获取，尝试从Sprite获取
            if (useRectSize && targetImage != null && targetImage.sprite != null)
            {
                return new Vector2(
                    targetImage.sprite.texture.width,
                    targetImage.sprite.texture.height);
            }

            return manualResolution;
        }

        private bool HasParametersChanged()
        {
            if (lastUseIndividualCorners != useIndividualCorners)
                return true;
            
            if (!useIndividualCorners)
            {
                if (Mathf.Abs(lastCornerRadius - cornerRadius) > 0.01f)
                    return true;
            }
            else
            {
                if (Mathf.Abs(lastTopLeftRadius - topLeftRadius) > 0.01f)
                    return true;
                if (Mathf.Abs(lastTopRightRadius - topRightRadius) > 0.01f)
                    return true;
                if (Mathf.Abs(lastBottomLeftRadius - bottomLeftRadius) > 0.01f)
                    return true;
                if (Mathf.Abs(lastBottomRightRadius - bottomRightRadius) > 0.01f)
                    return true;
            }

            if (Mathf.Abs(lastEdgeSmoothing - edgeSmoothing) > 0.01f)
                return true;

            Vector2 currentResolution = GetResolution();
            if (Vector2.Distance(lastResolution, currentResolution) > 0.01f)
                return true;

            return false;
        }

        #endregion

        #region 编辑器辅助

#if UNITY_EDITOR
        [ContextMenu("重置为默认值")]
        private void ResetToDefault()
        {
            useIndividualCorners = false;
            cornerRadius = 50f;
            topLeftRadius = 50f;
            topRightRadius = 50f;
            bottomLeftRadius = 50f;
            bottomRightRadius = 50f;
            edgeSmoothing = 1f;
            useRectSize = true;
            manualResolution = new Vector2(512, 512);
            UpdateMaterialProperties();
        }

        [ContextMenu("匹配Sprite尺寸")]
        private void MatchSpriteSize()
        {
            if (targetImage == null)
                targetImage = GetComponent<Image>();

            if (targetImage != null && targetImage.sprite != null)
            {
                useRectSize = false;
                manualResolution = new Vector2(
                    targetImage.sprite.texture.width,
                    targetImage.sprite.texture.height);
                UpdateMaterialProperties();
            }
        }
        
        [ContextMenu("设置仅上方圆角")]
        private void SetTopCornersOnly()
        {
            useIndividualCorners = true;
            topLeftRadius = topRightRadius = 50f;
            bottomLeftRadius = bottomRightRadius = 0f;
            UpdateMaterialProperties();
        }
        
        [ContextMenu("设置仅下方圆角")]
        private void SetBottomCornersOnly()
        {
            useIndividualCorners = true;
            topLeftRadius = topRightRadius = 0f;
            bottomLeftRadius = bottomRightRadius = 50f;
            UpdateMaterialProperties();
        }
#endif

        #endregion
    }
}

