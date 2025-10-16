using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine.Events;

/// <summary>
/// 报纸动画控制器：按 image 列表顺序逐步显示图片
/// </summary>
public class NewspaperAnimationController : MonoBehaviour
{
    [Title("报纸图片设置")]
    [LabelText("报纸图片")]
    [SerializeField] private List<Image> newspaperImages = new List<Image>();

    [Title("显示参数")]
    [LabelText("每页之间的延迟")][SerializeField] private float delayBetweenPages = 0.3f;
    [LabelText("淡入动画时长")][SerializeField] private float fadeInDuration = 0.5f;
    [LabelText("是否在Start时自动播放")][SerializeField] private bool playOnStart = true;
    [LabelText("淡入缓动曲线")][SerializeField] private Ease fadeEase = Ease.OutQuad;

    [Title("每日消息预制体")]
    [LabelText("预制体(挂有 DailyMessageView)")][SerializeField] private DailyMessageView dailyMessagePrefab;
    [LabelText("生成到此(父级)")][SerializeField] private Transform dailyMessageParent;
    [LabelText("数据对象(SO)")][SerializeField] private DailyMessagesData dailyMessagesData;
    [LabelText("启用每日消息")][SerializeField] private bool enableDailyMessage = false;
    [LabelText("使用固定种子(调试用)")][Tooltip("启用后会基于天数混合种子，确保每天不同。生产环境建议关闭使用真随机。")][SerializeField] private bool useSeed = false;
    [LabelText("种子基数")][Tooltip("会与当前天数混合：finalSeed = seed + currentDay * 1000")][SerializeField] private int seed = 0;



    private Sequence _animationSequence;



    private void Start()
    {
        if (playOnStart)
        {
            PlayAnimation();
        }
    }

    /// <summary>
    /// 按顺序逐步显示所有报纸，并追加每日消息页
    /// </summary>
    public void PlayAnimation()
    {
        // 停止之前的动画
        StopAnimation();

        // 初始化所有报纸为不可见
        InitializeNewspapers();

        // 每日新闻预制体推迟到第4张报纸显示后再生成
        bool spawnDailyAfterFour = enableDailyMessage && dailyMessagePrefab != null;


        // 创建动画序列
        _animationSequence = DOTween.Sequence();

        for (int i = 0; i < newspaperImages.Count; i++)
        {
            Image newspaper = newspaperImages[i];
            if (newspaper == null) continue;

            float delay = i * delayBetweenPages;

            // 获取或添加CanvasGroup组件
            CanvasGroup canvasGroup = GetOrAddCanvasGroup(newspaper);
            canvasGroup.alpha = 0;

            // 先激活GameObject，然后播放淡入动画
            Image capturedNewspaper = newspaper;
            CanvasGroup capturedGroup = canvasGroup;

            _animationSequence.InsertCallback(delay, () =>
            {
                if (capturedNewspaper != null)
                {
                    capturedNewspaper.gameObject.SetActive(true);
                }
            });

            // 添加淡入动画
            _animationSequence.Insert(delay, capturedGroup.DOFade(1f, fadeInDuration).SetEase(fadeEase));
        }

        if (spawnDailyAfterFour)
        {
            int afterCount = 4;
            int clamped = Mathf.Clamp(afterCount, 0, Mathf.Max(0, newspaperImages.Count));
            float callbackDelay = clamped * delayBetweenPages;
            _animationSequence.InsertCallback(callbackDelay, () =>
            {
                var parent = dailyMessageParent != null ? dailyMessageParent : transform;
                var view = Instantiate(dailyMessagePrefab, parent);
                view.gameObject.SetActive(true);
                if (dailyMessagesData == null)
                {
                    Debug.LogWarning("[Newspaper] DailyMessagesData SO is null. Skipping daily message generation.");
                    return;
                }
                
                // 基于当前天数+存档ID混合种子，确保每天不同且不同存档不同
                int currentDay = TimeSystemManager.Instance != null ? TimeSystemManager.Instance.CurrentDay : 1;
                string slotId = SaveManager.Instance != null ? SaveManager.Instance.CurrentSlotID : "1";
                int slotHash = string.IsNullOrEmpty(slotId) ? 0 : slotId.GetHashCode();
                int finalSeed = useSeed ? (seed + slotHash + currentDay * 1000) : 0;
                
                if (useSeed)
                {
                    Debug.Log($"[Newspaper] 使用种子随机: 基数={seed}, 存档Hash={slotHash}, 天数={currentDay}, 最终种子={finalSeed}");
                }
                else
                {
                    Debug.Log($"[Newspaper] 使用真随机模式（基于时间戳+Guid+Unity.Random）");
                }
                
                var entry = dailyMessagesData.GetRandomEntry(useSeed ? (int?)finalSeed : null);
                
                if (entry == null)
                {
                    Debug.LogWarning("[Newspaper] No entries in DailyMessagesData.");
                    return;
                }
                // 加载图片并设置到预制体暴露的 Image（支持 06/6 双格式容错）
                Sprite sprite = !string.IsNullOrEmpty(entry.imagePath) ? LoadSpriteWithFallback(entry.imagePath) : null;
                view.SetSprite(sprite);

                // 使用新版25组合概率计算器
                var probabilityResult = VisitProbabilityCalculator.CalculateProbabilities(entry.adjustments);
                
                // 打印新版25组合概率调整（详细信息）
                Debug.Log($"[每日消息] {entry.id}《{entry.title}》 - 新版25组合概率计算完成");
                Debug.Log(probabilityResult.calculationSummary);
                
                // 打印受影响的组合详情
                if (entry.adjustments != null && entry.adjustments.Count > 0)
                {
                    Debug.Log($"[每日消息] 受影响的组合详情:");
                    foreach (var combo in probabilityResult.combinations)
                    {
                        if (Mathf.Abs(combo.adjustment) > 0.01f) // 只显示有调整的组合
                        {
                            Debug.Log($"  {combo}");
                        }
                    }
                }

                // 广播原有消息（保持兼容性）
                var payload = new DailyMessagesData.DailyMessageApplied
                {
                    id = entry.id,
                    title = entry.title,
                    imagePath = entry.imagePath,
                    adjustments = entry.adjustments
                };
                MessageManager.Send(payload);
                
                // 发送新版概率数据到NightScreen
                var probabilityPayload = new DailyProbabilityToNight
                {
                    messageId = entry.id,
                    messageTitle = entry.title,
                    currentDay = currentDay,
                    probabilityResult = probabilityResult,
                    originalAdjustments = entry.adjustments
                };
                
                Debug.Log($"[Newspaper] 发送概率数据到NightScreen: {probabilityPayload}");
                MessageManager.Send(MessageDefine.DAILY_PROBABILITY_TO_NIGHT, probabilityPayload);
                Debug.Log($"[Newspaper] 概率数据已发送，消息键: {MessageDefine.DAILY_PROBABILITY_TO_NIGHT}");
                
                // 同时保存到持久化存储，确保NightScreen能够获取到数据
                SaveProbabilityDataToPersistent(probabilityPayload);
            });
        }

        _animationSequence.Play();
    }

    /// <summary>
    /// 停止动画
    /// </summary>
    public void StopAnimation()
    {
        if (_animationSequence != null && _animationSequence.IsActive())
        {
            _animationSequence.Kill();
        }
    }

    /// <summary>
    /// 重置所有报纸到初始状态（隐藏）
    /// </summary>
    public void ResetNewspapers()
    {
        StopAnimation();
        InitializeNewspapers();
    }

    private void InitializeNewspapers()
    {
        foreach (var newspaper in newspaperImages)
        {
            if (newspaper == null) continue;

            // 确保有CanvasGroup组件并设置alpha为0
            CanvasGroup canvasGroup = GetOrAddCanvasGroup(newspaper);
            canvasGroup.alpha = 0;

            newspaper.gameObject.SetActive(false);
        }
    }

    private CanvasGroup GetOrAddCanvasGroup(Image image)
    {
        CanvasGroup canvasGroup = image.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = image.gameObject.AddComponent<CanvasGroup>();
        }
        return canvasGroup;
    }

    private Sprite LoadSpriteWithFallback(string path)
    {
        // 1) 尝试原路径
        var sp = Resources.Load<Sprite>(path);
        if (sp != null) return sp;

        // 2) 若末尾为 _0d → 尝试去掉前导 0
        int us = path.LastIndexOf('_');
        if (us >= 0 && us < path.Length - 1)
        {
            var suffix = path.Substring(us + 1);
            if (int.TryParse(suffix, out var n))
            {
                if (suffix.Length == 2 && suffix[0] == '0')
                {
                    var alt = path.Substring(0, us + 1) + n.ToString();
                    sp = Resources.Load<Sprite>(alt);
                    if (sp != null) return sp;
                }
                else if (suffix.Length == 1 && n < 10)
                {
                    var alt = path.Substring(0, us + 1) + "0" + suffix;
                    sp = Resources.Load<Sprite>(alt);
                    if (sp != null) return sp;
                }
            }
        }
        Debug.LogWarning($"[Newspaper] Sprite not found. Tried: {path} and zero-padding variants.");
        return null;
    }



    /// <summary>
    /// 保存概率数据到持久化存储
    /// </summary>
    private void SaveProbabilityDataToPersistent(DailyProbabilityToNight data)
    {
        try
        {
            string json = JsonUtility.ToJson(data);
            PlayerPrefs.SetString("DailyProbabilityData", json);
            PlayerPrefs.SetInt("DailyProbabilityDay", data.currentDay);
            PlayerPrefs.Save();
            Debug.Log($"[Newspaper] 概率数据已保存到持久化存储，天数: {data.currentDay}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Newspaper] 保存概率数据失败: {e.Message}");
        }
    }

    private void OnDestroy()
    {
        StopAnimation();
    }
}

