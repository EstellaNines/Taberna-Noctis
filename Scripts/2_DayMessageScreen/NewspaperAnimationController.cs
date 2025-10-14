using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections.Generic;
using Sirenix.OdinInspector;

/// <summary>
/// 报纸动画控制器：按 image 列表顺序逐步显示图片
/// </summary>
public class NewspaperAnimationController : MonoBehaviour
{
    [Title("报纸图片设置")]
    [LabelText("报纸图片")]
    [SerializeField] private List<Image> newspaperImages = new List<Image>();

    [Title("显示参数")]
    [LabelText("每页之间的延迟")] [SerializeField] private float delayBetweenPages = 0.3f;
    [LabelText("淡入动画时长")] [SerializeField] private float fadeInDuration = 0.5f;
    [LabelText("是否在Start时自动播放")] [SerializeField] private bool playOnStart = true;
    [LabelText("淡入缓动曲线")] [SerializeField] private Ease fadeEase = Ease.OutQuad;

    

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

    

    private void OnDestroy()
    {
        StopAnimation();
    }
}

