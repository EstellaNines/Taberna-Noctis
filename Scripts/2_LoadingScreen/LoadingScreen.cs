using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
using System.Collections.Generic;
using System;

// LoadingScreen：在被加载后，等待 LoadingRequest 消息，随后异步加载目标场景
public class LoadingScreen : MonoBehaviour
{
	[Serializable]
	public class LoadingAnimStep
	{
		public string stepName;
		public MonoBehaviour component; // 例如 GlassingClinking 或 UIProgressController
		public string startMethod; // 留空则按类型推断：GlassingClinking=RestartMove, UIProgressController=Play
		public GameObject rootToActivate; // 可选：开始该步骤时 SetActive(true)，未填则使用 component.gameObject
		public bool setActiveOnStart = true;
		public bool hideOnFinish = true; // 该步骤完成后是否隐藏其根
	}

	[SerializeField] private bool autoStartOnMessage = true;
	[SerializeField] private List<LoadingAnimStep> steps = new List<LoadingAnimStep>();

	private UnityAction<LoadingRequest> _listener;
	private string _pendingTarget;
	private LoadSceneMode _pendingMode;
	private AsyncOperation _loadOp;
	private int _currentStep = -1;

	private void Awake()
	{
		_listener = OnLoadingRequest;
		MessageManager.Register<LoadingRequest>(_listener);
		MessageManager.Register<ActivateLoadedScene>(OnActivateLoadedScene);

		// 初始隐藏列表中配置的根对象（未配置则使用组件自身GO）
		foreach (var s in steps)
		{
			if (s != null && s.setActiveOnStart)
			{
				var root = s.rootToActivate != null ? s.rootToActivate : (s.component != null ? s.component.gameObject : null);
				if (root != null) root.SetActive(false);
			}
		}
	}

	private void OnDestroy()
	{
		MessageManager.Remove<LoadingRequest>(_listener);
		MessageManager.Remove<ActivateLoadedScene>(OnActivateLoadedScene);
	}

	private void OnEnable()
	{
		// 如果消息先到而物体当时是禁用的，等启用后再自动开始
		if (autoStartOnMessage && !string.IsNullOrEmpty(_pendingTarget))
		{
			StartCoroutine(BeginLoad());
		}
		// 进入Loading即按序播放第一段
		if (_currentStep < 0)
		{
			StartStep(0);
		}
	}

	private void OnLoadingRequest(LoadingRequest req)
	{
		_pendingTarget = req.targetSceneName;
		_pendingMode = req.mode;
		if (autoStartOnMessage && isActiveAndEnabled)
		{
			StartCoroutine(BeginLoad());
		}
	}

	private System.Collections.IEnumerator BeginLoad()
	{
		if (string.IsNullOrEmpty(_pendingTarget)) yield break;
		_loadOp = SceneManager.LoadSceneAsync(_pendingTarget, _pendingMode);
		_loadOp.allowSceneActivation = false; // 等待进度动效结束后再切换
		var progressCtrl = FindProgressController();
		while (_loadOp != null && _loadOp.progress < 0.9f)
		{
			// 传递异步进度给进度UI（0..0.9）
			if (progressCtrl != null)
			{
				float normalized = Mathf.Clamp01(_loadOp.progress / 0.9f);
				progressCtrl.SetProgressNormalized(normalized);
			}
			yield return null;
		}
	}

	// 供动画事件或按钮在动效结束时手动触发开始加载
	public void StartLoading()
	{
		if (!string.IsNullOrEmpty(_pendingTarget))
		{
			StartCoroutine(BeginLoad());
		}
	}

	// 播放指定序号的步骤
	private void StartStep(int index)
	{
		if (index < 0 || index >= steps.Count)
		{
			SequenceFinished();
			return;
		}
		_currentStep = index;
		var s = steps[index];
		if (s == null || s.component == null)
		{
			StepDone(index);
			return;
		}
		if (s.setActiveOnStart)
		{
			var root = s.rootToActivate != null ? s.rootToActivate : (s.component != null ? s.component.gameObject : null);
			if (root != null) root.SetActive(true);
		}

		HookStepCompletion(s, index);
		InvokeStart(s);
		MessageManager.Send(new LoadingAnimStage { stage = (string.IsNullOrEmpty(s.stepName) ? $"Step{index}" : s.stepName) + "_Started" });
	}

	private void StepDone(int index)
	{
		var s = index >=0 && index < steps.Count ? steps[index] : null;
		MessageManager.Send(new LoadingAnimStage { stage = (s != null && !string.IsNullOrEmpty(s.stepName) ? s.stepName : $"Step{index}") + "_Finished" });
		// 完成后按需隐藏当前步骤根
		if (s != null && s.hideOnFinish)
		{
			var root = s.rootToActivate != null ? s.rootToActivate : (s.component != null ? s.component.gameObject : null);
			if (root != null) root.SetActive(false);
		}
		StartStep(index + 1);
	}

	private void SequenceFinished()
	{
		// 完成后发布“动效列表完成”消息，由外部（全局管理器）决定何时激活
		var nameToSend = string.IsNullOrEmpty(_pendingTarget) ? null : _pendingTarget;
		MessageManager.Send(new LoadingSequenceCompleted { targetSceneName = nameToSend });
	}

	private void HookStepCompletion(LoadingAnimStep s, int index)
	{
		// 针对已知两种类型进行绑定
		if (s.component is GlassingClinking g)
		{
			Action handler = null;
			handler = () => { g.onFinished -= handler; StepDone(index); };
			g.onFinished += handler;
			return;
		}
		if (s.component is UIProgressController p)
		{
			UnityAction handler = null;
			handler = () => { p.OnCompleted.RemoveListener(handler); StepDone(index); };
			p.OnCompleted.AddListener(handler);
			return;
		}
		// 未知类型：不绑定，立即推进（避免卡死）
		StepDone(index);
	}

	private void InvokeStart(LoadingAnimStep s)
	{
		if (s.component == null) return;
		var method = s.startMethod;
		if (string.IsNullOrEmpty(method))
		{
			if (s.component is GlassingClinking) method = "RestartMove";
			else if (s.component is UIProgressController) method = "Play";
			else method = "Play"; // 尝试通用名
		}
		var m = s.component.GetType().GetMethod(method, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic, null, Type.EmptyTypes, null);
		if (m != null) m.Invoke(s.component, null);
	}

	private UIProgressController FindProgressController()
	{
		for (int i = 0; i < steps.Count; i++)
		{
			if (steps[i]?.component is UIProgressController p) return p;
		}
		return null;
	}

	private void OnActivateLoadedScene(ActivateLoadedScene msg)
	{
		if (_loadOp != null)
		{
			_loadOp.allowSceneActivation = true;
		}
		else if (!string.IsNullOrEmpty(_pendingTarget))
		{
			SceneManager.LoadScene(_pendingTarget, _pendingMode);
		}
	}
}


