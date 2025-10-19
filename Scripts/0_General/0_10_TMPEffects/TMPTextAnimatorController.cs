using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// TMP波浪动画控制器 - 为TMP文本添加轻微的波浪形上下浮动效果
/// 无需任何插件依赖，直接操作顶点实现
/// </summary>
[RequireComponent(typeof(TextMeshProUGUI))]
public class TMPTextAnimatorController : MonoBehaviour
{
    [Header("波浪动画设置")]
    [SerializeField] private bool enableWaveAnimation = true;
    [SerializeField] private bool autoStart = true;
    
    [Header("波浪效果参数")]
    [Range(0.5f, 8f)]
    [SerializeField] private float waveAmplitude = 3f;      // 波浪振幅（上下浮动距离）
    [Range(0.5f, 5f)]
    [SerializeField] private float waveSpeed = 2f;         // 波浪速度
    [Range(0.1f, 2f)]
    [SerializeField] private float waveFrequency = 1f;     // 波浪频率（字符间的波浪密度）
    
    [Header("高级设置")]
    [SerializeField] private bool randomOffset = true;     // 随机起始偏移
    [SerializeField] private AnimationCurve waveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1); // 波浪曲线
    
    // 组件引用和动画状态
    private TextMeshProUGUI tmpText;
    private Coroutine waveCoroutine;
    private bool isAnimating = false;
    private float timeOffset;
    
    private void Awake()
    {
        // 获取组件引用
        tmpText = GetComponent<TextMeshProUGUI>();
        
        if (tmpText == null)
        {
            Debug.LogError("[TMPTextAnimatorController] 未找到 TextMeshProUGUI 组件！");
            enabled = false;
            return;
        }
        
        // 设置随机时间偏移
        if (randomOffset)
        {
            timeOffset = Random.Range(0f, Mathf.PI * 2f);
        }
    }
    
    private void Start()
    {
        if (autoStart && enableWaveAnimation)
        {
            StartWaveAnimation();
        }
    }
    
    private void OnEnable()
    {
        if (enableWaveAnimation && !isAnimating)
        {
            StartWaveAnimation();
        }
    }
    
    private void OnDisable()
    {
        StopWaveAnimation();
    }
    
    /// <summary>
    /// 开始波浪动画
    /// </summary>
    public void StartWaveAnimation()
    {
        if (isAnimating || tmpText == null) return;
        
        isAnimating = true;
        waveCoroutine = StartCoroutine(WaveAnimationCoroutine());
        
        Debug.Log("[TMPTextAnimatorController] 波浪动画已启动");
    }
    
    /// <summary>
    /// 停止波浪动画
    /// </summary>
    public void StopWaveAnimation()
    {
        if (waveCoroutine != null)
        {
            StopCoroutine(waveCoroutine);
            waveCoroutine = null;
        }
        
        isAnimating = false;
        
        Debug.Log("[TMPTextAnimatorController] 波浪动画已停止");
    }
    
    /// <summary>
    /// 波浪动画协程 - 核心动画实现
    /// </summary>
    private IEnumerator WaveAnimationCoroutine()
    {
        while (enableWaveAnimation && isAnimating)
        {
            // 强制更新文本网格
            tmpText.ForceMeshUpdate();
            
            // 获取文本信息
            TMP_TextInfo textInfo = tmpText.textInfo;
            
            for (int i = 0; i < textInfo.characterCount; i++)
            {
                TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
                
                // 跳过不可见字符
                if (!charInfo.isVisible) continue;
                
                // 获取字符的顶点索引
                int vertexIndex = charInfo.vertexIndex;
                
                // 获取材质索引
                int materialIndex = charInfo.materialReferenceIndex;
                Vector3[] vertices = textInfo.meshInfo[materialIndex].vertices;
                
                // 计算波浪偏移
                float time = Time.time * waveSpeed + timeOffset;
                float waveOffset = Mathf.Sin(time + i * waveFrequency) * waveAmplitude;
                
                // 应用曲线调制
                float curveValue = waveCurve.Evaluate((Mathf.Sin(time + i * waveFrequency) + 1f) * 0.5f);
                waveOffset *= curveValue;
                
                // 应用偏移到字符的所有顶点（每个字符有4个顶点）
                Vector3 offset = new Vector3(0, waveOffset, 0);
                
                vertices[vertexIndex + 0] += offset; // 左下
                vertices[vertexIndex + 1] += offset; // 左上
                vertices[vertexIndex + 2] += offset; // 右上
                vertices[vertexIndex + 3] += offset; // 右下
            }
            
            // 更新所有材质的网格
            for (int i = 0; i < textInfo.meshInfo.Length; i++)
            {
                textInfo.meshInfo[i].mesh.vertices = textInfo.meshInfo[i].vertices;
                tmpText.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
            }
            
            yield return null;
        }
    }
    
    #region 公共控制方法
    
    /// <summary>
    /// 切换波浪动画
    /// </summary>
    public void ToggleWaveAnimation()
    {
        if (isAnimating)
        {
            StopWaveAnimation();
        }
        else
        {
            StartWaveAnimation();
        }
    }
    
    /// <summary>
    /// 启用/禁用波浪动画
    /// </summary>
    public void SetWaveAnimationEnabled(bool enabled)
    {
        enableWaveAnimation = enabled;
        
        if (enabled && !isAnimating)
        {
            StartWaveAnimation();
        }
        else if (!enabled && isAnimating)
        {
            StopWaveAnimation();
        }
    }
    
    /// <summary>
    /// 设置波浪振幅
    /// </summary>
    public void SetWaveAmplitude(float amplitude)
    {
        waveAmplitude = Mathf.Clamp(amplitude, 0.5f, 8f);
    }
    
    /// <summary>
    /// 设置波浪速度
    /// </summary>
    public void SetWaveSpeed(float speed)
    {
        waveSpeed = Mathf.Clamp(speed, 0.5f, 5f);
    }
    
    /// <summary>
    /// 设置波浪频率
    /// </summary>
    public void SetWaveFrequency(float frequency)
    {
        waveFrequency = Mathf.Clamp(frequency, 0.1f, 2f);
    }
    
    /// <summary>
    /// 设置所有波浪参数
    /// </summary>
    public void SetWaveParameters(float amplitude, float speed, float frequency)
    {
        SetWaveAmplitude(amplitude);
        SetWaveSpeed(speed);
        SetWaveFrequency(frequency);
    }
    
    #endregion
    
    #region 预设波浪效果
    
    /// <summary>
    /// 轻微波浪预设
    /// </summary>
    [ContextMenu("应用轻微波浪")]
    public void ApplyGentleWave()
    {
        waveAmplitude = 2f;
        waveSpeed = 1.5f;
        waveFrequency = 0.8f;
        enableWaveAnimation = true;
        
        if (!isAnimating)
        {
            StartWaveAnimation();
        }
    }
    
    /// <summary>
    /// 标准波浪预设
    /// </summary>
    [ContextMenu("应用标准波浪")]
    public void ApplyStandardWave()
    {
        waveAmplitude = 3f;
        waveSpeed = 2f;
        waveFrequency = 1f;
        enableWaveAnimation = true;
        
        if (!isAnimating)
        {
            StartWaveAnimation();
        }
    }
    
    /// <summary>
    /// 强烈波浪预设
    /// </summary>
    [ContextMenu("应用强烈波浪")]
    public void ApplyStrongWave()
    {
        waveAmplitude = 5f;
        waveSpeed = 3f;
        waveFrequency = 1.2f;
        enableWaveAnimation = true;
        
        if (!isAnimating)
        {
            StartWaveAnimation();
        }
    }
    
    /// <summary>
    /// 慢速优雅波浪预设
    /// </summary>
    [ContextMenu("应用优雅波浪")]
    public void ApplyElegantWave()
    {
        waveAmplitude = 1.5f;
        waveSpeed = 0.8f;
        waveFrequency = 0.5f;
        enableWaveAnimation = true;
        
        if (!isAnimating)
        {
            StartWaveAnimation();
        }
    }
    
    #endregion
    
    #region 编辑器功能
    
#if UNITY_EDITOR
    private void OnValidate()
    {
        // 限制参数范围
        waveAmplitude = Mathf.Clamp(waveAmplitude, 0.5f, 8f);
        waveSpeed = Mathf.Clamp(waveSpeed, 0.5f, 5f);
        waveFrequency = Mathf.Clamp(waveFrequency, 0.1f, 2f);
        
        // 在运行时实时调整动画效果
        if (Application.isPlaying && enableWaveAnimation && isAnimating)
        {
            // 参数发生变化时，动画会自动应用新参数
        }
    }
#endif
    
    #endregion
}
