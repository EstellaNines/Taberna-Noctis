using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;
using System.Collections;
#if ODIN_INSPECTOR || SIRENIX_ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif
using VInspector;

// 用于在指定时长内驱动TMP百分比文本和RawImage材质中某个Shader浮点属性从起始值到目标值
public class UIProgressController : MonoBehaviour
{
	// 目标RawImage（其材质来自ShaderGraph）
	#if ODIN_INSPECTOR || SIRENIX_ODIN_INSPECTOR
	[LabelText("目标RawImage")]
	#endif
	[VInspector.Foldout("引用"), SerializeField, InspectorCN("目标RawImage")] private RawImage targetRawImage;

	// 百分比显示的TMP文本
	#if ODIN_INSPECTOR || SIRENIX_ODIN_INSPECTOR
	[LabelText("进度文本TMP")]
	#endif
	[VInspector.Foldout("引用"), SerializeField, InspectorCN("进度文本TMP")] private TextMeshProUGUI percentText;

	// Shader中的浮点属性名_Slider
	#if ODIN_INSPECTOR || SIRENIX_ODIN_INSPECTOR
	[LabelText("Shader浮点属性名")]
	#endif
	[VInspector.Foldout("引用"), SerializeField, InspectorCN("Shader浮点属性名")] private string shaderFloatPropertyName = "_Slider";

	// 动画总时长（秒）
	#if ODIN_INSPECTOR || SIRENIX_ODIN_INSPECTOR
	[LabelText("时长(秒)"), PropertyRange(0.01f, 10f)]
	#endif
	[VInspector.Foldout("播放设置"), SerializeField, InspectorCN("时长(秒)")] private float durationSeconds = 1f;

	// 是否在Enable时自动播放
	#if ODIN_INSPECTOR || SIRENIX_ODIN_INSPECTOR
	[LabelText("启用时自动播放")]
	#endif
	[VInspector.Foldout("播放设置"), SerializeField, InspectorCN("启用时自动播放")] private bool playOnEnable = true;

	// 是否使用不受Time.timeScale影响的时间
	#if ODIN_INSPECTOR || SIRENIX_ODIN_INSPECTOR
	[LabelText("忽略时间缩放")]
	#endif
	[VInspector.Foldout("播放设置"), SerializeField, InspectorCN("忽略时间缩放")] private bool useUnscaledTime = false;

	// 缓动曲线（0到1的进度映射），可视化调整
	#if ODIN_INSPECTOR || SIRENIX_ODIN_INSPECTOR
	[LabelText("缓动曲线")]
	#endif
	[VInspector.Foldout("播放设置"), SerializeField, InspectorCN("缓动曲线")] private AnimationCurve easing = AnimationCurve.Linear(0f, 0f, 1f, 1f);

	// 起止范围：TMP文本从0%到100%，材质属性从0到2
	private const float TextStart = 0f;
	private const float TextEnd = 100f;
	private const float ShaderStart = 0f;
	private const float ShaderEnd = 2.1f;

	// 缓存的属性ID与运行时材质实例
	private int shaderFloatPropertyId = -1;
	private Material runtimeMaterial;

	// 当前播放协程
	private Coroutine playRoutine;

	// 播放完成事件（供外部编排使用）
	[SerializeField] private UnityEvent onCompleted;
	public UnityEvent OnCompleted => onCompleted;

	[VInspector.Foldout("调试/操作"), VInspector.Button("播放")]
	private void Editor_PlayButton()
	{
		Play();
	}

	[VInspector.Foldout("调试/操作"), VInspector.Button("跳到起点")]
	private void Editor_SetToStartButton()
	{
		SetToStart();
	}

	[VInspector.Foldout("调试/操作"), VInspector.Button("跳到终点")]
	private void Editor_SetToEndButton()
	{
		SetToEnd();
	}

	[VInspector.Foldout("调试/状态"), VInspector.ReadOnly, VInspector.ShowInInspector]
	private float 当前文本百分比
	{
		get
		{
			if (percentText == null) return 0f;
			float value = 0f;
			if (int.TryParse(percentText.text.TrimEnd('%'), out var pct)) value = pct;
			return value;
		}
	}

	[VInspector.Foldout("调试/状态"), VInspector.ReadOnly, VInspector.ShowInInspector]
	private float 当前材质Slider
	{
		get
		{
			if (runtimeMaterial == null || shaderFloatPropertyId == -1) return 0f;
			return runtimeMaterial.GetFloat(shaderFloatPropertyId);
		}
	}

	private void Awake()
	{
		// 缓存组件，尽量在Awake阶段准备好依赖
		if (targetRawImage == null)
		{
			targetRawImage = GetComponent<RawImage>();
		}

		// 创建或获取UI使用的运行时材质实例，避免修改共享材质
		if (targetRawImage != null)
		{
			runtimeMaterial = targetRawImage.material;
		}

		// 解析并校验属性名，优先_Slider，失败时尝试不带下划线的Slider
		ResolveShaderFloatProperty();
	}

	private void OnEnable()
	{
		// 可选开播
		if (playOnEnable)
		{
			Play();
		}
	}

	// 对外播放入口：使用当前durationSeconds
	public void Play()
	{
		if (!isActiveAndEnabled)
		{
			return;
		}

		// 确保属性已解析
		if (shaderFloatPropertyId == -1)
		{
			ResolveShaderFloatProperty();
		}

		// 确保时长有效
		if (durationSeconds <= 0f)
		{
			durationSeconds = 0.0001f;
		}

		// 复位与重启
		if (playRoutine != null)
		{
			StopCoroutine(playRoutine);
		}
		playRoutine = StartCoroutine(AnimateRoutine(durationSeconds));
	}

	// 指定时长播放
	public void Play(float customDurationSeconds)
	{
		if (customDurationSeconds <= 0f)
		{
			customDurationSeconds = 0.0001f;
		}
		durationSeconds = customDurationSeconds;
		Play();
	}

	// 立即跳到起点
	public void SetToStart()
	{
		ApplyProgress(0f);
	}

	// 立即跳到终点
	public void SetToEnd()
	{
		ApplyProgress(1f);
	}

	// 外部实时设置规范化进度[0,1]，用于异步加载等场景
	public void SetProgressNormalized(float normalized)
	{
		ApplyProgress(Mathf.Clamp01(normalized));
	}

	// 协程：从0到1推进进度
	private IEnumerator AnimateRoutine(float seconds)
	{
		float elapsed = 0f;
		ApplyProgress(0f);

		while (elapsed < seconds)
		{
			float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
			elapsed += dt;
			float t = Mathf.Clamp01(elapsed / seconds);
			float eased = easing != null ? Mathf.Clamp01(easing.Evaluate(t)) : t;
			ApplyProgress(eased);
			yield return null;
		}

		ApplyProgress(1f);
		playRoutine = null;
		onCompleted?.Invoke();
	}

	// 根据规范化进度更新UI显示与材质属性
	private void ApplyProgress(float normalized)
	{
		// 更新文本
		if (percentText != null)
		{
			float pct = Mathf.Lerp(TextStart, TextEnd, normalized);
			int pctInt = Mathf.RoundToInt(pct);
			percentText.text = pctInt.ToString() + "%";
		}

		// 更新材质属性
		if (runtimeMaterial != null && shaderFloatPropertyId != -1)
		{
			float v = Mathf.Lerp(ShaderStart, ShaderEnd, normalized);
			runtimeMaterial.SetFloat(shaderFloatPropertyId, v);
		}
	}

	// 解析Shader属性名，优先_Slider，若材质不包含则尝试Slider
	private void ResolveShaderFloatProperty()
	{
		shaderFloatPropertyId = -1;
		if (runtimeMaterial == null)
		{
			return;
		}

		// 主候选
		string primary = string.IsNullOrEmpty(shaderFloatPropertyName) ? "_Slider" : shaderFloatPropertyName;
		int primaryId = Shader.PropertyToID(primary);
		if (runtimeMaterial.HasProperty(primaryId))
		{
			shaderFloatPropertyName = primary;
			shaderFloatPropertyId = primaryId;
			return;
		}

		// 备选：去掉或添加下划线尝试
		string alt = primary.StartsWith("_") ? primary.TrimStart('_') : ("_" + primary);
		int altId = Shader.PropertyToID(alt);
		if (runtimeMaterial.HasProperty(altId))
		{
			shaderFloatPropertyName = alt;
			shaderFloatPropertyId = altId;
			return;
		}

		// 未找到则保持-1，避免无效写入
	}
}


