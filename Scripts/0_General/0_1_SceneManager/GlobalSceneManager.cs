using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
using System;

// 全局场景管理器：常驻（DontDestroyOnLoad），负责发起场景切换：
// A -> LoadingScreen -> B：先加载 LoadingScreen，LoadingScreen 收到 LoadingRequest 后异步加载 B
public class GlobalSceneManager : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private SceneSequenceConfig sequenceConfig; // 通过引用配置顺序
    [SerializeField] private string defaultTargetScene = string.Empty; // 可选：按钮无参调用时使用

    private static GlobalSceneManager s_Instance;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        // 不在启动前强制创建，让场景中的实例优先（以便保留已配置的 sequenceConfig）
    }

    private static void EnsureInstance()
    {
        if (s_Instance == null)
        {
            var go = new GameObject("GlobalSceneManager");
            s_Instance = go.AddComponent<GlobalSceneManager>();
            DontDestroyOnLoad(go);
        }
    }
    private UnityAction<LoadingSequenceCompleted> _onLoadingSeqCompleted;
    private UnityAction<ActivateLoadedScene> _onActivateLoaded;
    private UnityAction<GoNextScene> _onGoNext;
    private UnityAction<GoPrevScene> _onGoPrev;
    private UnityAction<GoToSceneIndex> _onGoToIndex;
    private UnityAction<GoToSceneName> _onGoToName;
    private string _currentTargetScene;
    private LoadSceneMode _currentMode = LoadSceneMode.Single;
    private int _currentIndex = -1;

	private void Awake()
	{
		if (s_Instance != null && s_Instance != this) { Destroy(gameObject); return; }
		s_Instance = this;
		DontDestroyOnLoad(gameObject);

        _onLoadingSeqCompleted = OnLoadingSequenceCompleted;
        _onActivateLoaded = OnActivateLoaded;
        _onGoNext = OnGoNext;
        _onGoPrev = OnGoPrev;
        _onGoToIndex = OnGoToIndex;
        _onGoToName = OnGoToName;
        MessageManager.Register(_onLoadingSeqCompleted);
        MessageManager.Register(_onActivateLoaded);
        MessageManager.Register(_onGoNext);
        MessageManager.Register(_onGoPrev);
        MessageManager.Register(_onGoToIndex);
        MessageManager.Register(_onGoToName);

		// 初始时将当前激活场景同步到顺序索引，避免 _currentIndex 为 -1 时 Next() 仍停留在当前场景
		SyncIndexWithActiveScene();
	}

	// 对外：请求切换到下一个场景（经由 LoadingScreen）
    public static void LoadWithLoadingScreen(string targetScene, LoadSceneMode mode = LoadSceneMode.Single)
	{
		if (s_Instance == null)
		{
			var go = new GameObject("GlobalSceneManager");
			s_Instance = go.AddComponent<GlobalSceneManager>();
			DontDestroyOnLoad(go);
		}
		s_Instance.InternalLoadWithLoadingScreen(targetScene, mode);
	}

	// ===== 对外静态接口：多种切换方式 =====
	public static void Next()
	{
        EnsureInstance();
        s_Instance.OnGoNext(new GoNextScene());
	}

	public static void Prev()
	{
        EnsureInstance();
        s_Instance.OnGoPrev(new GoPrevScene());
	}

	public static void GoToIndex(int index)
	{
        EnsureInstance();
        s_Instance.OnGoToIndex(new GoToSceneIndex { index = index });
	}

	public static void GoToName(string sceneName)
	{
        EnsureInstance();
        s_Instance.OnGoToName(new GoToSceneName { sceneName = sceneName });
	}

	public static void ReloadCurrent()
	{
        EnsureInstance();
		var active = SceneManager.GetActiveScene().name;
		LoadWithLoadingScreen(active, LoadSceneMode.Single);
	}

    private void InternalLoadWithLoadingScreen(string targetScene, LoadSceneMode mode)
	{
        _currentTargetScene = NormalizeSceneName(targetScene);
		_currentMode = mode;
        // 先切到 LoadingScreen（从配置读取）
        var loading = sequenceConfig != null && !string.IsNullOrEmpty(sequenceConfig.loadingScreenSceneName)
            ? sequenceConfig.loadingScreenSceneName : "S_LoadingScreen";
        SceneManager.LoadScene(loading, LoadSceneMode.Single);
		// 发送 LoadingRequest，让 LoadingScreen 自主开始异步加载目标场景
        var req = new LoadingRequest { targetSceneName = _currentTargetScene, mode = mode };
		MessageManager.Send(req);
	}

	// 实例方法：供 Unity Button OnClick 调用（带场景名参数）
	public void LoadTo(string targetScene)
	{
		if (string.IsNullOrEmpty(targetScene)) return;
		LoadWithLoadingScreen(targetScene, LoadSceneMode.Single);
	}

	// 实例方法：供 UI 按钮直接绑定（下一场景/上一场景/索引/重载）
	public void LoadNext() { Next(); }
	public void LoadPrev() { Prev(); }
	public void LoadIndex(int index) { GoToIndex(index); }
	public void ReloadActive() { ReloadCurrent(); }

	// 实例方法：供 Unity Button OnClick 无参调用（使用默认目标）
	public void LoadToDefault()
	{
		if (string.IsNullOrEmpty(defaultTargetScene)) return;
		LoadWithLoadingScreen(defaultTargetScene, LoadSceneMode.Single);
	}

	private void OnDestroy()
	{
		if (s_Instance == this)
		{
			MessageManager.Remove(_onLoadingSeqCompleted);
			MessageManager.Remove(_onActivateLoaded);
            MessageManager.Remove(_onGoNext);
            MessageManager.Remove(_onGoPrev);
            MessageManager.Remove(_onGoToIndex);
            MessageManager.Remove(_onGoToName);
		}
	}

	// Loading 列表完成 → 请求激活
	private void OnLoadingSequenceCompleted(LoadingSequenceCompleted msg)
	{
		var target = string.IsNullOrEmpty(msg.targetSceneName) ? _currentTargetScene : msg.targetSceneName;
		MessageManager.Send(new ActivateLoadedScene { targetSceneName = target });
	}

	// 外部请求激活 → 由 LoadingScreen 响应；如果没有 LoadingScreen 响应，则直接加载
	private void OnActivateLoaded(ActivateLoadedScene msg)
	{
		// 兜底：若没有人处理，则直接切换
		var target = string.IsNullOrEmpty(msg.targetSceneName) ? _currentTargetScene : msg.targetSceneName;
        target = NormalizeSceneName(target);
		if (!string.IsNullOrEmpty(target))
		{
			SceneManager.LoadScene(target, _currentMode);
            UpdateCurrentIndexByName(target);
		}
	}

    // ============ 顺序控制 ============
    private void OnGoNext(GoNextScene _)
    {
        if (sequenceConfig == null || sequenceConfig.orderedScenes == null || sequenceConfig.orderedScenes.Count == 0) return;
        SyncIndexWithActiveScene();
        int next = _currentIndex + 1;
        if (next >= sequenceConfig.orderedScenes.Count) next = sequenceConfig.orderedScenes.Count - 1;
        GoToIndexInternal(next);
    }

    private void OnGoPrev(GoPrevScene _)
    {
        if (sequenceConfig == null || sequenceConfig.orderedScenes == null || sequenceConfig.orderedScenes.Count == 0) return;
        SyncIndexWithActiveScene();
        int prev = _currentIndex - 1;
        if (prev < 0) prev = 0;
        GoToIndexInternal(prev);
    }

    private void OnGoToIndex(GoToSceneIndex msg)
    {
        GoToIndexInternal(msg.index);
    }

    private void OnGoToName(GoToSceneName msg)
    {
        if (string.IsNullOrEmpty(msg.sceneName)) return;
        int idx = IndexOfScene(msg.sceneName);
        if (idx >= 0) GoToIndexInternal(idx);
        else LoadWithLoadingScreen(NormalizeSceneName(msg.sceneName), LoadSceneMode.Single);
    }

    private void GoToIndexInternal(int index)
    {
        if (sequenceConfig == null || index < 0 || index >= sequenceConfig.orderedScenes.Count) return;
        var entry = sequenceConfig.orderedScenes[index];
        _currentIndex = index;
        LoadWithLoadingScreen(entry.sceneName, entry.loadMode);
    }

    private int IndexOfScene(string name)
    {
        if (sequenceConfig == null || sequenceConfig.orderedScenes == null) return -1;
        for (int i = 0; i < sequenceConfig.orderedScenes.Count; i++)
        {
            if (string.Equals(sequenceConfig.orderedScenes[i].sceneName, name, StringComparison.Ordinal)) return i;
        }
        return -1;
    }

    private void UpdateCurrentIndexByName(string name)
    {
        int idx = IndexOfScene(NormalizeSceneName(name));
        if (idx >= 0) _currentIndex = idx;
    }

    private string NormalizeSceneName(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        // 兼容旧名称到新编号前缀
        if (string.Equals(name, "StartScreen", StringComparison.Ordinal)) return "0_StartScreen";
        if (string.Equals(name, "SaveFilesScreen", StringComparison.Ordinal)) return "1_SaveFilesScreen";
        if (string.Equals(name, "DayMessageScreen", StringComparison.Ordinal)) return "2_DayMessageScreen";
        if (string.Equals(name, "DayScreen", StringComparison.Ordinal)) return "3_DayScreen";
        if (string.Equals(name, "NightScreen", StringComparison.Ordinal)) return "4_NightScreen";
        if (string.Equals(name, "SettlementScreen", StringComparison.Ordinal)) return "5_SettlementScreen";
        if (string.Equals(name, "LoadingScreen", StringComparison.Ordinal)) return "S_LoadingScreen";
        return name;
    }

	private void SyncIndexWithActiveScene()
	{
		if (sequenceConfig == null || sequenceConfig.orderedScenes == null || sequenceConfig.orderedScenes.Count == 0) return;
		var active = SceneManager.GetActiveScene().name;
		UpdateCurrentIndexByName(active);
	}
}


