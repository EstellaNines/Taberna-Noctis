using UnityEngine;
using TMPro;
using DG.Tweening;
using Sirenix.OdinInspector;

namespace TabernaNoctis.UI
{
	/// <summary>
	/// 在 DailyMessage 场景中显示当日提示，例如在一个 Image 下的 TMP 文本中显示 "Day1"。
	/// 将此脚本挂到包含 Image 的父物体上（有子 TMP_Text），或直接指定 dayText 引用。
	/// </summary>
	public class DailyMessageDayLabel : MonoBehaviour
	{
		[Header("引用")]
		[LabelText("天数字文本")]
		[SerializeField]
		[Tooltip("显示天数字的 TMP 文本，不指定则自动在子物体中查找")]
		private TMP_Text dayText;

		[Header("配置")]
		[LabelText("显示前缀")]
		[SerializeField]
		[Tooltip("显示前缀，例如 'Day'")]
		private string dayPrefix = "Day";

		[LabelText("天数字")]
		[SerializeField]
		[Tooltip("要显示的天数，运行时可由外部设置")]
		private int dayNumber = 1;

		[LabelText("启动时自动刷新")]
		[SerializeField]
		[Tooltip("Start 时自动刷新显示")]
		private bool autoUpdateOnStart = true;

		[Header("流程控制")]
		[LabelText("按时间系统流程显示")]
		[SerializeField]
		[Tooltip("按时间系统流程显示（进入场景时读取当前天数，并在收到 DAY_STARTED 时刷新/播放）")]
		private bool showByTimeSystemFlow = true;

		[Header("淡入淡出")]
		[LabelText("启动时自动播放")]
		[SerializeField]
		[Tooltip("Start 时自动播放淡入/停留/淡出")]
		private bool autoPlayOnStart = true;

		[LabelText("淡入时长(秒)")]
		[SerializeField]
		[Tooltip("淡入时长（秒）")]
		private float fadeInDuration = 0.6f;

		[LabelText("停留时长(秒)")]
		[SerializeField]
		[Tooltip("停留时长（秒）")]
		private float holdDuration = 1.2f;

		[LabelText("淡出时长(秒)")]
		[SerializeField]
		[Tooltip("淡出时长（秒）")]
		private float fadeOutDuration = 0.6f;

		[LabelText("目标Alpha")]
		[SerializeField]
		[Range(0f,1f)]
		[Tooltip("目标 Alpha")]
		private float targetAlpha = 1f;

		[LabelText("结束后隐藏物体")]
		[SerializeField]
		[Tooltip("淡出后是否隐藏 GameObject")]
		private bool deactivateOnComplete = false;

		private CanvasGroup canvasGroup;
		private Sequence fadeSequence;

		private void Reset()
		{
			TryAutoWire();
		}

		private void Awake()
		{
			if (dayText == null)
			{
				TryAutoWire();
			}
			// 确保存在 CanvasGroup 以驱动整体淡入淡出
			canvasGroup = GetComponent<CanvasGroup>();
			if (canvasGroup == null)
			{
				canvasGroup = gameObject.AddComponent<CanvasGroup>();
			}
		}

		private void OnEnable()
		{
			// 订阅“新一天开始”的流程消息
			MessageManager.Register<int>(MessageDefine.DAY_STARTED, OnDayStarted);
		}

		// 合并到文件末尾的 OnDisable 实现

		private void Start()
		{
			if (showByTimeSystemFlow)
			{
				// 进入场景时从时间系统读取当前天数
				if (TimeSystemManager.Instance != null)
				{
					SetDay(TimeSystemManager.Instance.CurrentDay);
				}
			}
			else if (autoUpdateOnStart)
			{
				Refresh();
			}
			if (autoPlayOnStart)
			{
				PlayFade();
			}
		}

		private void OnDayStarted(int day)
		{
			if (!showByTimeSystemFlow) return;
			SetDay(day);
			PlayFade();
		}

		private void TryAutoWire()
		{
			dayText = GetComponentInChildren<TMP_Text>(true);
		}

		[ContextMenu("刷新显示")]
		public void Refresh()
		{
			if (dayText == null)
			{
				TryAutoWire();
			}
			if (dayText == null)
			{
				Debug.LogWarning("[DailyMessageDayLabel] 未找到 TMP_Text 引用");
				return;
			}

			dayText.text = $"{dayPrefix}{dayNumber}";
		}

		[ContextMenu("播放淡入/停留/淡出一次")]
		public void PlayFade()
		{
			if (fadeSequence != null && fadeSequence.IsActive())
			{
				fadeSequence.Kill();
			}
			if (canvasGroup == null)
			{
				canvasGroup = GetComponent<CanvasGroup>();
				if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
			}
			gameObject.SetActive(true);
			canvasGroup.alpha = 0f;
			fadeSequence = DOTween.Sequence();
			fadeSequence.Append(canvasGroup.DOFade(targetAlpha, fadeInDuration).SetEase(Ease.OutQuad));
			fadeSequence.AppendInterval(holdDuration);
			fadeSequence.Append(canvasGroup.DOFade(0f, fadeOutDuration).SetEase(Ease.InQuad));
			fadeSequence.OnComplete(() =>
			{
				if (deactivateOnComplete) gameObject.SetActive(false);
			});
		}

		/// <summary>
		/// 外部设置“天数”（例如由时间系统、存档系统驱动）
		/// </summary>
		public void SetDay(int day)
		{
			dayNumber = Mathf.Max(1, day);
			Refresh();
		}

		/// <summary>
		/// 同时设置 day 并播放一次淡入/停留/淡出
		/// </summary>
		public void SetDayAndPlay(int day)
		{
			SetDay(day);
			PlayFade();
		}

		/// <summary>
		/// 外部设置显示前缀（可选）
		/// </summary>
		public void SetPrefix(string prefix)
		{
			dayPrefix = prefix ?? string.Empty;
			Refresh();
		}

		private void OnDisable()
		{
			// 取消订阅 DAY_STARTED
			MessageManager.Remove<int>(MessageDefine.DAY_STARTED, OnDayStarted);
			// 停止并清理淡入/淡出序列
			if (fadeSequence != null && fadeSequence.IsActive())
			{
				fadeSequence.Kill();
				fadeSequence = null;
			}
		}
	}
}


