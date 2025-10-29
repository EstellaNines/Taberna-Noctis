using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 白天 BGM 控制器：
/// - 在主菜单、存档、每日消息、早上、下午等场景自动以 0.75 音量循环播放 Day BGM
/// - 跨 Loading 场景不停播；进入夜晚或其它非白天场景时停止
/// - 常驻全局并自动创建，无需手动摆放
/// </summary>
public class DaytimeBgmController : MonoBehaviour
{
    private static DaytimeBgmController _instance;

    [Header("场景白名单（进入时播放Day BGM）")]
    [SerializeField] private string[] dayBgmScenes = new[]
    {
        "0_StartScreen",
        "1_SaveFilesScreen",
        "2_DayMessageScreen",
        "3_DayScreen",
        "4_AfternoonScreen"
    };

    [Header("加载场景名（进入时保持播放，不主动启动）")]
    [SerializeField] private string loadingSceneName = "S_LoadingScreen";

    [Header("音量（0-1）")]
    [Range(0f,1f)]
    [SerializeField] private float dayBgmVolume = 0.05f;

    [Header("夜晚/结算场景（播放夜晚BGM）")]
    [SerializeField] private string[] nightBgmScenes = new[]
    {
        "5_NightScreen",
        "6_SettlementScreen"
    };

    [Header("夜晚BGM音量（0-1）")]
    [Range(0f,1f)]
    [SerializeField] private float nightBgmVolume = 0.05f;

    [Header("淡入淡出(秒)")]
    [SerializeField] private float fadeInSeconds = 4f;
    [SerializeField] private float fadeOutSeconds = 4f;

    private enum BgmMode { None, Day, Night }
    private BgmMode currentMode = BgmMode.None;
    private string lastSceneName = string.Empty;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (FindObjectOfType<DaytimeBgmController>() == null)
        {
            var go = new GameObject("DaytimeBgmController");
            go.AddComponent<DaytimeBgmController>();
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        SceneManager.activeSceneChanged += OnActiveSceneChanged;
    }

    private void OnDisable()
    {
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
    }

    private void Start()
    {
        // 首次场景评估
        EvaluateScene(SceneManager.GetActiveScene());
    }

    private void OnActiveSceneChanged(Scene oldScene, Scene newScene)
    {
        lastSceneName = oldScene.name;
        EvaluateScene(newScene);
    }

    private void EvaluateScene(Scene scene)
    {
        string name = scene.name;
        if (string.IsNullOrEmpty(name)) return;

        bool isWhitelist = IsWhitelisted(name);
        bool isLoading = string.Equals(name, loadingSceneName, System.StringComparison.Ordinal);
        bool isNight = IsNightScene(name);

        if (isWhitelist)
        {
            EnsureDayBgm();
            return;
        }

        if (isLoading)
        {
            // 进入 Loading：若刚从下午离开，则在Loading上完成日→夜的淡出/淡入
            if (string.Equals(lastSceneName, "4_AfternoonScreen", System.StringComparison.Ordinal))
            {
                // 淡出日BGM
                if (currentMode == BgmMode.Day)
                {
                    AudioManager.instance?.FadeOutBGM(fadeOutSeconds, stopAtEnd: true);
                    currentMode = BgmMode.None;
                }
                // 淡入夜BGM
                AudioManager.instance?.FadeInBGM(GlobalAudio.NightBackgroundMusic, nightBgmVolume, fadeInSeconds, loop: true);
                currentMode = BgmMode.Night;
            }
            return;
        }

        if (isNight)
        {
            EnsureNightBgm();
            return;
        }

        // 其它场景：淡出当前BGM
        FadeOutCurrentIfAny();
    }

    private bool IsWhitelisted(string sceneName)
    {
        for (int i = 0; i < dayBgmScenes.Length; i++)
        {
            if (string.Equals(sceneName, dayBgmScenes[i], System.StringComparison.Ordinal))
                return true;
        }
        return false;
    }

    private bool IsNightScene(string sceneName)
    {
        for (int i = 0; i < nightBgmScenes.Length; i++)
        {
            if (string.Equals(sceneName, nightBgmScenes[i], System.StringComparison.Ordinal))
                return true;
        }
        return false;
    }

    private void EnsureDayBgm()
    {
        if (AudioManager.instance == null) return;
        if (currentMode == BgmMode.Day) return;
        // 切换到日BGM：淡出旧的 → 淡入日BGM
        AudioManager.instance.FadeOutBGM(fadeOutSeconds, stopAtEnd: true);
        AudioManager.instance.FadeInBGM(GlobalAudio.DayBackgroundMusic, Mathf.Clamp01(dayBgmVolume), fadeInSeconds, loop: true);
        currentMode = BgmMode.Day;
    }

    private void EnsureNightBgm()
    {
        if (AudioManager.instance == null) return;
        if (currentMode == BgmMode.Night) return;
        AudioManager.instance.FadeOutBGM(fadeOutSeconds, stopAtEnd: true);
        AudioManager.instance.FadeInBGM(GlobalAudio.NightBackgroundMusic, Mathf.Clamp01(nightBgmVolume), fadeInSeconds, loop: true);
        currentMode = BgmMode.Night;
    }

    private void FadeOutCurrentIfAny()
    {
        if (currentMode == BgmMode.None) return;
        AudioManager.instance?.FadeOutBGM(fadeOutSeconds, stopAtEnd: true);
        currentMode = BgmMode.None;
    }
}


