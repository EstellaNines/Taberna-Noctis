using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace TabernaNoctis.NightScreen
{
    /// <summary>
    /// 顾客NPC UI视图组件
    /// 挂载在CustomerNPC预制件上，负责读取并显示顾客数据
    /// </summary>
    public class CustomerNpcView : MonoBehaviour
    {
        [Header("UI组件引用")]
        [SerializeField] private Image portraitImage;           // 立绘图片
        [SerializeField] private TextMeshProUGUI stateText;     // 状态标签（彩色）
        [SerializeField] private TextMeshProUGUI nameText;      // 人物名称（白色）
        
        [Header("动画配置")]
        [SerializeField] private CanvasGroup canvasGroup;       // 用于淡入淡出
        [SerializeField] private float fadeInDuration = 0.5f;
        [SerializeField] private float fadeOutDuration = 0.5f;
        
        [Header("调试信息")]
        [SerializeField] private NpcCharacterData currentData;  // 当前显示的NPC数据（调试用）
        
        /// <summary>
        /// 当前显示的顾客数据
        /// </summary>
        public NpcCharacterData CurrentData => currentData;
        
        /// <summary>
        /// 是否正在显示顾客
        /// </summary>
        public bool IsActive => canvasGroup != null && canvasGroup.alpha > 0.5f;

        private void Awake()
        {
            // 确保初始状态为隐藏
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
            }
        }

        /// <summary>
        /// 初始化并显示顾客UI
        /// </summary>
        /// <param name="data">顾客数据</param>
        /// <param name="playAnimation">是否播放淡入动画</param>
        public void ShowCustomer(NpcCharacterData data, bool playAnimation = true)
        {
            if (data == null)
            {
                Debug.LogError("[CustomerNpcView] ShowCustomer: data 为空！");
                return;
            }

            currentData = data;

            // 1. 加载立绘
            LoadPortrait(data.portraitPath);

            // 2. 显示状态标签（彩色，100%不透明，格式：<状态>）
            if (stateText != null)
            {
                // 禁用Rich Text以防止<>被解释为标签
                stateText.richText = false;
                stateText.text = $"<{data.state}>"; // 显示格式：<Busy>, <Friendly>等
                
                Color stateColorWithAlpha = data.stateColor;
                stateColorWithAlpha.a = 1f; // 确保alpha为1（完全不透明）
                stateText.color = stateColorWithAlpha; // 应用状态对应的颜色
                
                // 强制刷新
                stateText.SetAllDirty();
                
                Debug.Log($"[CustomerNpcView] 状态文本: '<{data.state}>', 颜色: {stateColorWithAlpha}");
            }

            // 3. 显示人物名称（白色，100%不透明）
            if (nameText != null)
            {
                nameText.text = data.displayName; // 显示简短名称：Company Employee, Boss等
                
                // 强制设置为白色，覆盖任何可能的样式
                Color nameColorWithAlpha = Color.white;
                nameColorWithAlpha.a = 1f; // 确保alpha为1（完全不透明）
                nameText.color = nameColorWithAlpha;
                
                // 强制刷新
                nameText.SetAllDirty();
                
                Debug.Log($"[CustomerNpcView] 名称文本: '{data.displayName}', 颜色: 白色");
            }

            // 4. 播放淡入动画
            if (playAnimation && canvasGroup != null)
            {
                canvasGroup.DOKill(); // 停止之前的动画
                canvasGroup.alpha = 0f;
                canvasGroup.DOFade(1f, fadeInDuration).SetEase(Ease.OutQuad);
            }
            else if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }

            Debug.Log($"[CustomerNpcView] 显示顾客: {data.displayName} ({data.state})");
        }

        /// <summary>
        /// 隐藏顾客UI
        /// </summary>
        /// <param name="playAnimation">是否播放淡出动画</param>
        /// <param name="onComplete">完成回调</param>
        public void HideCustomer(bool playAnimation = true, System.Action onComplete = null)
        {
            if (playAnimation && canvasGroup != null)
            {
                canvasGroup.DOKill();
                canvasGroup.DOFade(0f, fadeOutDuration)
                    .SetEase(Ease.InQuad)
                    .OnComplete(() =>
                    {
                        ResetView();
                        onComplete?.Invoke();
                    });
            }
            else
            {
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 0f;
                }
                ResetView();
                onComplete?.Invoke();
            }

            Debug.Log($"[CustomerNpcView] 隐藏顾客: {currentData?.displayName}");
        }

        /// <summary>
        /// 重置视图状态
        /// </summary>
        public void ResetView()
        {
            currentData = null;

            if (portraitImage != null)
            {
                portraitImage.sprite = null;
            }

            if (stateText != null)
            {
                stateText.text = "";
                stateText.richText = false; // 确保Rich Text被禁用
            }

            if (nameText != null)
            {
                nameText.text = "";
                nameText.color = Color.white; // 重置为白色
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
            }
        }

        /// <summary>
        /// 从Resources加载立绘
        /// </summary>
        private void LoadPortrait(string portraitPath)
        {
            if (portraitImage == null)
            {
                Debug.LogWarning("[CustomerNpcView] portraitImage 未分配！");
                return;
            }

            if (string.IsNullOrEmpty(portraitPath))
            {
                Debug.LogWarning("[CustomerNpcView] portraitPath 为空，无法加载立绘");
                portraitImage.sprite = null;
                return;
            }

            // 从Resources加载Sprite
            Sprite sprite = Resources.Load<Sprite>(portraitPath);
            
            if (sprite != null)
            {
                portraitImage.sprite = sprite;
                portraitImage.enabled = true;
                Debug.Log($"[CustomerNpcView] 成功加载立绘: {portraitPath}");
            }
            else
            {
                Debug.LogWarning($"[CustomerNpcView] 无法加载立绘: Resources/{portraitPath}");
                portraitImage.sprite = null;
            }
        }

        /// <summary>
        /// 验证组件引用是否完整
        /// </summary>
        private void OnValidate()
        {
            bool hasErrors = false;

            if (portraitImage == null)
            {
                Debug.LogWarning($"[CustomerNpcView] {gameObject.name}: portraitImage 未分配！");
                hasErrors = true;
            }

            if (stateText == null)
            {
                Debug.LogWarning($"[CustomerNpcView] {gameObject.name}: stateText 未分配！应该拖入【状态标签】的TMP组件");
                hasErrors = true;
            }
            else
            {
                // 检查状态文本的名称，帮助识别是否拖错了
                string objName = stateText.gameObject.name.ToLower();
                if (objName.Contains("name") && !objName.Contains("state"))
                {
                    Debug.LogError($"[CustomerNpcView] {gameObject.name}: stateText 可能拖错了！当前拖入的是 '{stateText.gameObject.name}'，应该拖入【StateText/状态文本】组件");
                    hasErrors = true;
                }
            }

            if (nameText == null)
            {
                Debug.LogWarning($"[CustomerNpcView] {gameObject.name}: nameText 未分配！应该拖入【名称/身份】的TMP组件");
                hasErrors = true;
            }
            else
            {
                // 检查名称文本的名称，帮助识别是否拖错了
                string objName = nameText.gameObject.name.ToLower();
                if (objName.Contains("state") && !objName.Contains("name"))
                {
                    Debug.LogError($"[CustomerNpcView] {gameObject.name}: nameText 可能拖错了！当前拖入的是 '{nameText.gameObject.name}'，应该拖入【NameText/名称文本】组件");
                    hasErrors = true;
                }
            }

            if (canvasGroup == null)
            {
                Debug.LogWarning($"[CustomerNpcView] {gameObject.name}: canvasGroup 未分配！");
                hasErrors = true;
            }

            if (!hasErrors)
            {
                Debug.Log($"[CustomerNpcView] {gameObject.name}: ? 所有组件引用验证通过");
            }
        }
    }
}

