using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using TabernaNoctis.CharacterDesign;

namespace TabernaNoctis.NightScreen
{
    /// <summary>
    /// 顾客NPC行为组件
    /// 管理顾客入场动画（侧身走路 → 淡出 → 立绘淡入）和UI显示
    /// </summary>
    public class CustomerNpcBehavior : MonoBehaviour
    {
        [Header("UI引用 - 侧身走路动画")]
        [SerializeField] private GameObject walkingContainer;     // 侧身走路动画容器
        [SerializeField] private Image walkingSilhouette;         // 侧身剪影Image
        [SerializeField] private Transform walkingStartPoint;     // 走路起始位置
        [SerializeField] private Transform walkingEndPoint;       // 走路结束位置（柜台前）

        [Header("UI引用 - 顾客立绘显示")]
        [SerializeField] private GameObject customerContainer;    // 顾客立绘容器
        [SerializeField] private CanvasGroup customerCanvasGroup; // 顾客立绘CanvasGroup
        [SerializeField] private Image portraitImage;             // 顾客立绘
        [SerializeField] private TextMeshProUGUI nameText;        // 顾客名字
        [SerializeField] private TextMeshProUGUI stateText;       // 顾客状态
        [SerializeField] private Image stateIcon;                 // 状态图标（可选）
        [SerializeField] private Slider moodBar;                  // 心情条（可选）

        [Header("动画配置")]
        [SerializeField] private float walkDuration = 3f;         // 走路总时长
        [SerializeField] private float walkBobHeight = 10f;       // 走路上下起伏高度
        [SerializeField] private float walkBobSpeed = 2f;         // 走路起伏频率
        [SerializeField] private float fadeOutStartTime = 0.8f;   // 淡出开始时间（走路结束前）
        [SerializeField] private float fadeOutDuration = 0.8f;    // 侧身淡出时长
        [SerializeField] private float fadeInDuration = 0.8f;     // 立绘淡入时长
        [SerializeField] private Ease fadeEase = Ease.InOutQuad;  // 淡入淡出缓动

        [Header("离场动画配置")]
        [SerializeField] private float exitFadeDuration = 1f;     // 离场淡出时长

        [Header("音频配置")]
        [SerializeField] private AudioSource footstepAudioSource; // 脚步声音源（可选，用于3D音效）
        [SerializeField] private float footstepVolume = 0.5f;     // 脚步声音量

        [Header("调试信息")]
        [SerializeField] private bool isAnimating = false;
        [SerializeField] private string currentCustomerId = "";

        // 公开属性
        public NpcCharacterData CurrentData { get; private set; }
        public bool IsServicing { get; private set; }
        public bool IsVisible => customerCanvasGroup != null && customerCanvasGroup.alpha > 0.1f;
        public bool IsAnimating => isAnimating;

        // 动画序列引用
        private Sequence currentAnimation;
        private Coroutine walkingBobCoroutine;
        
        // 音频控制
        private int footstepPlayId = -1; // 脚步声播放ID

        // 侧身剪影精灵缓存
        private Sprite maleSilhouetteSprite;
        private Sprite femaleSilhouetteSprite;

        #region Unity生命周期

        private void Awake()
        {
            ValidateComponents();
            LoadSilhouetteSprites();
            ResetState();
        }

        private void OnDestroy()
        {
            // 清理动画和协程
            StopAllAnimations();
        }

        #endregion

        #region 公开接口

        /// <summary>
        /// 初始化顾客数据并播放完整入场动画
        /// </summary>
        /// <param name="data">顾客数据</param>
        /// <param name="onComplete">动画完成回调</param>
        public void Initialize(NpcCharacterData data, Action onComplete = null)
        {
            if (data == null)
            {
                Debug.LogError("[CustomerNpcBehavior] Initialize: data为空");
                onComplete?.Invoke();
                return;
            }

            // 如果当前有顾客在服务中，先让其离场
            if (IsServicing && CurrentData != null)
            {
                Debug.Log($"[CustomerNpcBehavior] 上一位顾客 {CurrentData.displayName} 开始离场，为新顾客 {data.displayName} 让位");
                
                PlayExitAnimation(() =>
                {
                    // 上一位顾客离场完成后，初始化新顾客
                    InitializeNewCustomer(data, onComplete);
                });
            }
            else
            {
                // 没有当前顾客，直接初始化新顾客
                InitializeNewCustomer(data, onComplete);
            }
        }

        /// <summary>
        /// 初始化新顾客（内部方法）
        /// </summary>
        private void InitializeNewCustomer(NpcCharacterData data, Action onComplete)
        {
            CurrentData = data;
            IsServicing = true;
            currentCustomerId = data.identityId;

            Debug.Log($"[CustomerNpcBehavior] 初始化顾客: {data.displayName} ({data.state}, {data.gender})");

            // 预加载顾客立绘数据
            PrepareCustomerUI(data);

            // 播放完整入场动画序列
            PlayEnterAnimationSequence(onComplete);
        }

        /// <summary>
        /// 播放离场动画
        /// </summary>
        /// <param name="onComplete">动画完成回调</param>
        public void PlayExitAnimation(Action onComplete = null)
        {
            if (!IsServicing)
            {
                Debug.LogWarning("[CustomerNpcBehavior] 当前无顾客在服务中");
                onComplete?.Invoke();
                return;
            }

            StopAllAnimations();
            isAnimating = true;

            Debug.Log($"[CustomerNpcBehavior] 开始离场动画: {CurrentData?.displayName}");

            // 简单的淡出动画
            currentAnimation = DOTween.Sequence()
                .Append(customerCanvasGroup.DOFade(0f, exitFadeDuration).SetEase(fadeEase))
                .OnComplete(() =>
                {
                    isAnimating = false;
                    Debug.Log($"[CustomerNpcBehavior] 离场动画完成: {CurrentData?.displayName}");
                    onComplete?.Invoke();
                });

            currentAnimation.Play();
        }

        /// <summary>
        /// 重置组件状态
        /// </summary>
        public void ResetState()
        {
            StopAllAnimations();

            // 清空数据
            CurrentData = null;
            IsServicing = false;
            currentCustomerId = "";
            isAnimating = false;

            // 重置UI状态
            if (walkingContainer != null)
                walkingContainer.SetActive(false);

            if (customerContainer != null)
                customerContainer.SetActive(false);

            if (walkingSilhouette != null)
                walkingSilhouette.color = Color.white;

            if (customerCanvasGroup != null)
            {
                customerCanvasGroup.alpha = 0f;
                customerCanvasGroup.interactable = false;
                customerCanvasGroup.blocksRaycasts = false;
            }

            // 清空UI内容
            ClearCustomerUI();

            Debug.Log("[CustomerNpcBehavior] 状态已重置");
        }

        #endregion

        #region 入场动画序列

        /// <summary>
        /// 播放完整入场动画序列
        /// </summary>
        private void PlayEnterAnimationSequence(Action onComplete)
        {
            StopAllAnimations();
            isAnimating = true;

            // 设置侧身剪影
            SetupWalkingSilhouette();

            // 激活走路容器，隐藏顾客容器
            walkingContainer.SetActive(true);
            customerContainer.SetActive(true);
            customerCanvasGroup.alpha = 0f;

            // 设置侧身剪影初始位置
            walkingSilhouette.transform.position = walkingStartPoint.position;

            Debug.Log($"[CustomerNpcBehavior] 开始入场动画序列: {CurrentData?.displayName}");

            // 启动走路上下起伏动画（包含移动）
            StartWalkingBobAnimation();

            // 创建动画序列（仅处理淡入淡出）
            currentAnimation = DOTween.Sequence();

            // 1. 等待走路动画进行到淡出开始时间
            var fadeOutStartDelay = walkDuration - fadeOutStartTime;
            currentAnimation.AppendInterval(fadeOutStartDelay);

            // 2. 开始淡出侧身剪影，同时停止脚步声
            currentAnimation.Append(walkingSilhouette.DOFade(0f, fadeOutDuration).SetEase(fadeEase)
                .OnStart(() => {
                    StopFootstepSound();
                    Debug.Log("[CustomerNpcBehavior] 脚步声在侧身开始淡出时停止");
                }));

            // 3. 同时淡入顾客立绘（与淡出同时进行）
            currentAnimation.Insert(fadeOutStartDelay, 
                customerCanvasGroup.DOFade(1f, fadeInDuration).SetEase(fadeEase));

            // 4. 动画完成回调
            currentAnimation.OnComplete(() =>
            {
                // 隐藏走路容器
                walkingContainer.SetActive(false);
                
                // 启用顾客交互
                customerCanvasGroup.interactable = true;
                customerCanvasGroup.blocksRaycasts = true;
                
                isAnimating = false;
                Debug.Log($"[CustomerNpcBehavior] 入场动画序列完成: {CurrentData?.displayName}");
                onComplete?.Invoke();
            });

            currentAnimation.Play();
        }

        /// <summary>
        /// 设置侧身剪影
        /// </summary>
        private void SetupWalkingSilhouette()
        {
            if (walkingSilhouette == null || CurrentData == null)
                return;

            // 根据性别选择侧身剪影
            Sprite silhouetteSprite = CurrentData.gender.ToLower() == "female" 
                ? femaleSilhouetteSprite 
                : maleSilhouetteSprite;

            if (silhouetteSprite != null)
            {
                walkingSilhouette.sprite = silhouetteSprite;
                walkingSilhouette.color = Color.white; // 重置透明度
                Debug.Log($"[CustomerNpcBehavior] 设置侧身剪影: {CurrentData.gender} -> {silhouetteSprite.name}");
            }
            else
            {
                Debug.LogWarning($"[CustomerNpcBehavior] 侧身剪影加载失败: {CurrentData.gender}");
            }
        }

        /// <summary>
        /// 启动走路上下起伏动画
        /// </summary>
        private void StartWalkingBobAnimation()
        {
            if (walkingSilhouette == null)
                return;

            // 播放脚步声
            PlayFootstepSound();

            walkingBobCoroutine = StartCoroutine(WalkingBobCoroutine());
        }

        /// <summary>
        /// 走路上下起伏协程
        /// </summary>
        private IEnumerator WalkingBobCoroutine()
        {
            float elapsed = 0f;
            Vector3 startWorldPos = walkingStartPoint.position;
            Vector3 endWorldPos = walkingEndPoint.position;

            while (elapsed < walkDuration && isAnimating)
            {
                // 计算当前应该在的水平位置（基于时间进度）
                float progress = elapsed / walkDuration;
                Vector3 currentBasePos = Vector3.Lerp(startWorldPos, endWorldPos, progress);
                
                // 计算上下起伏偏移
                float bobOffset = Mathf.Sin(elapsed * walkBobSpeed * Mathf.PI * 2) * walkBobHeight;
                
                // 应用位置：基础位置 + 起伏偏移
                walkingSilhouette.transform.position = currentBasePos + Vector3.up * bobOffset;

                elapsed += Time.deltaTime;
                yield return null;
            }

            // 确保最终位置正确
            walkingSilhouette.transform.position = endWorldPos;
        }

        #endregion

        #region UI管理

        /// <summary>
        /// 准备顾客UI数据
        /// </summary>
        private void PrepareCustomerUI(NpcCharacterData data)
        {
            // 加载并设置立绘
            LoadPortrait(data.portraitPath);

            // 设置文本信息
            if (nameText != null)
                nameText.text = data.displayName;

            if (stateText != null)
            {
                stateText.text = data.state;
                stateText.color = data.stateColor;
            }

            // 设置心情值
            if (moodBar != null)
            {
                moodBar.value = Mathf.Clamp01(data.initialMood / 100f);
            }

            // 加载状态图标（可选）
            LoadStateIcon(data.state);
        }

        /// <summary>
        /// 加载顾客立绘
        /// </summary>
        private void LoadPortrait(string portraitPath)
        {
            if (portraitImage == null || string.IsNullOrEmpty(portraitPath))
                return;

            try
            {
                var sprite = Resources.Load<Sprite>(portraitPath);
                if (sprite != null)
                {
                    portraitImage.sprite = sprite;
                    Debug.Log($"[CustomerNpcBehavior] 立绘加载成功: {portraitPath}");
                }
                else
                {
                    Debug.LogWarning($"[CustomerNpcBehavior] 立绘加载失败: {portraitPath}");
                    LoadDefaultPortrait();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[CustomerNpcBehavior] 立绘加载异常: {portraitPath}, 错误: {e.Message}");
                LoadDefaultPortrait();
            }
        }

        /// <summary>
        /// 加载默认立绘
        /// </summary>
        private void LoadDefaultPortrait()
        {
            if (portraitImage == null)
                return;

            var defaultSprite = Resources.Load<Sprite>("Character/Portrait/Default");
            if (defaultSprite != null)
            {
                portraitImage.sprite = defaultSprite;
                Debug.Log("[CustomerNpcBehavior] 使用默认立绘");
            }
        }

        /// <summary>
        /// 加载状态图标
        /// </summary>
        private void LoadStateIcon(string state)
        {
            if (stateIcon == null || string.IsNullOrEmpty(state))
                return;

            try
            {
                var iconPath = $"UI/StateIcons/{state}";
                var iconSprite = Resources.Load<Sprite>(iconPath);
                
                if (iconSprite != null)
                {
                    stateIcon.sprite = iconSprite;
                    stateIcon.gameObject.SetActive(true);
                }
                else
                {
                    stateIcon.gameObject.SetActive(false);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[CustomerNpcBehavior] 状态图标加载异常: {state}, 错误: {e.Message}");
                stateIcon.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 清空顾客UI
        /// </summary>
        private void ClearCustomerUI()
        {
            if (portraitImage != null)
                portraitImage.sprite = null;

            if (nameText != null)
                nameText.text = "";

            if (stateText != null)
            {
                stateText.text = "";
                stateText.color = Color.white;
            }

            if (moodBar != null)
                moodBar.value = 0f;

            if (stateIcon != null)
            {
                stateIcon.sprite = null;
                stateIcon.gameObject.SetActive(false);
            }
        }

        #endregion

        #region 资源加载

        /// <summary>
        /// 加载侧身剪影精灵
        /// </summary>
        private void LoadSilhouetteSprites()
        {
            try
            {
                maleSilhouetteSprite = Resources.Load<Sprite>("Character/Portrait/侧身(男)");
                femaleSilhouetteSprite = Resources.Load<Sprite>("Character/Portrait/侧身(女)");

                if (maleSilhouetteSprite == null)
                    Debug.LogWarning("[CustomerNpcBehavior] 男性侧身剪影加载失败: Character/Portrait/侧身(男)");

                if (femaleSilhouetteSprite == null)
                    Debug.LogWarning("[CustomerNpcBehavior] 女性侧身剪影加载失败: Character/Portrait/侧身(女)");

                Debug.Log($"[CustomerNpcBehavior] 侧身剪影加载完成 - 男性: {maleSilhouetteSprite != null}, 女性: {femaleSilhouetteSprite != null}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[CustomerNpcBehavior] 侧身剪影加载异常: {e.Message}");
            }
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 验证必要组件
        /// </summary>
        private void ValidateComponents()
        {
            if (walkingContainer == null)
                Debug.LogError($"[CustomerNpcBehavior] {gameObject.name}: walkingContainer引用缺失");

            if (walkingSilhouette == null)
                Debug.LogError($"[CustomerNpcBehavior] {gameObject.name}: walkingSilhouette引用缺失");

            if (walkingStartPoint == null)
                Debug.LogError($"[CustomerNpcBehavior] {gameObject.name}: walkingStartPoint引用缺失");

            if (walkingEndPoint == null)
                Debug.LogError($"[CustomerNpcBehavior] {gameObject.name}: walkingEndPoint引用缺失");

            if (customerContainer == null)
                Debug.LogError($"[CustomerNpcBehavior] {gameObject.name}: customerContainer引用缺失");

            if (customerCanvasGroup == null)
                Debug.LogError($"[CustomerNpcBehavior] {gameObject.name}: customerCanvasGroup引用缺失");

            if (portraitImage == null)
                Debug.LogWarning($"[CustomerNpcBehavior] {gameObject.name}: portraitImage引用缺失");
        }

        /// <summary>
        /// 停止所有动画和协程
        /// </summary>
        private void StopAllAnimations()
        {
            // 停止DOTween动画
            if (currentAnimation != null && currentAnimation.IsActive())
            {
                currentAnimation.Kill();
                currentAnimation = null;
            }

            // 停止走路起伏协程
            if (walkingBobCoroutine != null)
            {
                StopCoroutine(walkingBobCoroutine);
                walkingBobCoroutine = null;
            }
            
            // 停止脚步声
            StopFootstepSound();
        }

        #endregion

        #region 音频管理

        /// <summary>
        /// 播放脚步声（使用可控音效API）
        /// </summary>
        private void PlayFootstepSound()
        {
            if (footstepPlayId != -1 || CurrentData == null)
                return;

            // 根据性别选择脚步声
            string footstepAudio = CurrentData.gender.ToLower() == "female"
                ? GlobalAudio.HighheelWalkFootstep
                : GlobalAudio.BootWalkFootstep;

            if (AudioManager.instance == null)
            {
                Debug.LogWarning("[CustomerNpcBehavior] AudioManager.instance为空，无法播放脚步声");
                return;
            }

            // 使用可控音效API播放循环脚步声
            footstepPlayId = AudioManager.instance.PlayControllableSE(footstepAudio, footstepVolume, loop: true);
            
            if (footstepPlayId != -1)
            {
                Debug.Log($"[CustomerNpcBehavior] 开始播放脚步声 ID:{footstepPlayId}, {footstepAudio} (音量: {footstepVolume})");
            }
        }

        /// <summary>
        /// 停止脚步声（使用可控音效API立即停止）
        /// </summary>
        private void StopFootstepSound()
        {
            if (footstepPlayId != -1 && AudioManager.instance != null)
            {
                AudioManager.instance.StopControllableSE(footstepPlayId);
                Debug.Log($"[CustomerNpcBehavior] 脚步声已停止 ID:{footstepPlayId}");
                footstepPlayId = -1;
            }
        }

        #endregion

        #region 编辑器调试

#if UNITY_EDITOR
        [Header("编辑器调试")]
        [SerializeField] private bool showDebugInfo = true;

        private void OnValidate()
        {
            if (!Application.isPlaying && showDebugInfo)
            {
                ValidateComponents();
            }
        }

        /// <summary>
        /// 编辑器下测试入场动画
        /// </summary>
        [ContextMenu("测试入场动画")]
        private void TestEnterAnimation()
        {
            if (Application.isPlaying && CurrentData != null)
            {
                PlayEnterAnimationSequence(() => Debug.Log("入场动画测试完成"));
            }
            else
            {
                Debug.LogWarning("需要在运行时且有顾客数据时才能测试");
            }
        }

        /// <summary>
        /// 编辑器下测试离场动画
        /// </summary>
        [ContextMenu("测试离场动画")]
        private void TestExitAnimation()
        {
            if (Application.isPlaying)
            {
                PlayExitAnimation(() => Debug.Log("离场动画测试完成"));
            }
        }
#endif

        #endregion
    }
}
