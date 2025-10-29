using UnityEngine;
using UnityEngine.UI;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

/// <summary>
/// 达到星级后的祝贺界面：
/// - Continue：开始新的一天并进入 DayMessage 场景
/// - Back：返回主菜单
/// </summary>
public class CongratulationScreenController : MonoBehaviour
{
#if ODIN_INSPECTOR
    [BoxGroup("按钮"), LabelText("继续按钮")]
#endif
    [SerializeField] private Button continueButton;

#if ODIN_INSPECTOR
    [BoxGroup("按钮"), LabelText("返回主菜单按钮")]
#endif
    [SerializeField] private Button backButton;

#if ODIN_INSPECTOR
    [BoxGroup("奖牌"), LabelText("奖牌容器(含Horizontal)")]
#endif
    [SerializeField] private Transform medalsRoot;

#if ODIN_INSPECTOR
    [BoxGroup("奖牌"), LabelText("奖牌Image(最多5个)")]
#endif
    [SerializeField] private Image[] medalImages = new Image[5];

    private void Awake()
    {
        if (continueButton != null) continueButton.onClick.AddListener(OnContinueClicked);
        if (backButton != null) backButton.onClick.AddListener(OnBackClicked);
        AutoCollectMedalsIfNeeded();
    }

    private void OnDestroy()
    {
        if (continueButton != null) continueButton.onClick.RemoveListener(OnContinueClicked);
        if (backButton != null) backButton.onClick.RemoveListener(OnBackClicked);
    }

    private void OnEnable()
    {
        RefreshMedals();
    }

    private void AutoCollectMedalsIfNeeded()
    {
        if ((medalImages == null || medalImages.Length == 0) && medalsRoot != null)
        {
            var imgs = medalsRoot.GetComponentsInChildren<Image>(true);
            // 尝试仅收集直接子节点上的Image（避免拿到背景）
            var list = new System.Collections.Generic.List<Image>();
            foreach (Transform child in medalsRoot)
            {
                var img = child.GetComponent<Image>();
                if (img != null) list.Add(img);
            }
            if (list.Count == 0 && imgs != null && imgs.Length > 0)
            {
                list.AddRange(imgs);
            }
            medalImages = list.ToArray();
        }
    }

    private void RefreshMedals()
    {
        int star = 0;
        if (SaveManager.Instance != null)
        {
            var snap = SaveManager.Instance.GenerateSaveData();
            if (snap != null) star = Mathf.Clamp(snap.starRating, 0, 5);
        }
        ApplyMedalDisplay(star);
    }

    private void ApplyMedalDisplay(int star)
    {
        if (medalImages == null || medalImages.Length == 0) return;
        for (int i = 0; i < medalImages.Length; i++)
        {
            var img = medalImages[i];
            if (img == null) continue;
            bool on = (i < star);
            img.gameObject.SetActive(on);
        }
    }

    private void OnContinueClicked()
    {
        // 进入新的一天
        if (TimeSystemManager.Instance != null)
        {
            TimeSystemManager.Instance.StartNewDay();
        }
        TryLoadScene("2_DayMessageScreen");
    }

    private void OnBackClicked()
    {
        TryLoadScene("0_StartScreen");
    }

    private void TryLoadScene(string sceneName)
    {
        try
        {
            GlobalSceneManager.LoadWithLoadingScreen(sceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
        catch
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
        }
    }
}


