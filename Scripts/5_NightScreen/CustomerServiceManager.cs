using System;
using System.Collections;
using UnityEngine;
using TabernaNoctis.CharacterDesign;
using Sirenix.OdinInspector;
using Random = UnityEngine.Random;
using TMPro;
using DG.Tweening;

namespace TabernaNoctis.NightScreen
{
    public interface IDialogueResolver
    {
        bool TryGetDialogueMap(string identityId, string state, string gender, out System.Collections.Generic.Dictionary<string, string> dialogueMap);
    }

    /// <summary>
    /// 顾客服务管理器
    /// 管理完整的顾客服务流程：
    /// - Night开始延迟3秒后第1位顾客出队
    /// - 监听卡牌拖拽 → 品尝3-5秒 → 自动结算 → 顾客离场
    /// - 前5位服务完成后立即出队下一位（无等待）
    /// - 第6位起服务完成后等待20秒再出队下一位
    /// </summary>
    public class CustomerServiceManager : MonoBehaviour
    {
        public static CustomerServiceManager Instance { get; private set; }

        // 对外广播的结算结果载体
        public struct SettlementBroadcast
        {
            public string npcId;
            public int moodDelta;
            public int price;
            public int tip;
            public int finalIncome;
            public int ratingDelta;
        }

        [Title("组件引用")]
        [InfoBox("场景中固定的CustomerNpcBehavior组件，复用显示所有顾客")]
        [Required("必须分配CustomerNpcBehavior组件")]
        [SerializeField] private CustomerNpcBehavior customerBehavior;

        [Title("对话显示与数据")]
        [LabelText("对话文本组件(TMP)")]
        [Tooltip("用于显示顾客随机台词的TMP文本组件")]
        [SerializeField] private TMP_Text dialogueText;

        [LabelText("对话框根节点")]
        [Tooltip("包含背景Image和TMP的根节点，用于统一显示/隐藏")] 
        [SerializeField] private GameObject dialogueRoot;

        [Title("TMP 打字机设置")]
        [LabelText("启用打字机显示")]
        [SerializeField] private bool enableTypewriter = true;

        [LabelText("打字机总时长(秒)")]
        [SerializeField] private float typewriterTotalDuration = 3f;

        [LabelText("（备用）每字符间隔(秒)")]
        [SerializeField] private float typewriterInterval = 0.03f;

        [LabelText("打字机弹出缩放")]
        [SerializeField] private float typewriterPopScale = 1.2f;

        [LabelText("弹出保持时长(秒)")]
        [SerializeField] private float typewriterPopHold = 0.06f;

        private Coroutine typewriterRoutine;

        [Title("心情进度（15段）")]
        [LabelText("心情条根(RectTransform)")]
        [Tooltip("父容器，代表总长度（分成15段）")]
        [SerializeField] private RectTransform moodBarRect;

        [LabelText("心情游标(RectTransform)")]
        [Tooltip("一个Image作为指示器，沿父容器水平移动到指定段中心")]
        [SerializeField] private RectTransform moodMarkerRect;

        [LabelText("最大心情（固定15）")]
        [SerializeField] private int moodMax = 15;

        [LabelText("当前顾客心情（运行时）")]
        [ShowInInspector, ReadOnly]
        private int currentCustomerMood = 0;

        [Title("结算显示（+心情/+金钱/+评价）")]
        [LabelText("心情增益根节点")]
        [SerializeField] private GameObject moodGainRoot;
        [LabelText("心情增益TMP")] 
        [SerializeField] private TMP_Text moodGainText;

        [LabelText("金钱增益根节点")]
        [SerializeField] private GameObject moneyGainRoot;
        [LabelText("金钱增益TMP")] 
        [SerializeField] private TMP_Text moneyGainText;

        [LabelText("评价增益根节点")]
        [SerializeField] private GameObject ratingGainRoot;
        [LabelText("评价增益TMP")] 
        [SerializeField] private TMP_Text ratingGainText;

        [Title("调试-模拟鸡尾酒结算")]
        [LabelText("调试心情增益")] [SerializeField] private int debugMoodDelta = 3;
        [LabelText("调试金钱固定")] [SerializeField] private int debugMoneyGain = 50;
        [LabelText("调试评价增益")] [SerializeField] private int debugRatingDelta = 8;

        [Title("DOTween 动画设置（增益显示）")]
        [LabelText("淡入时长(秒)")]
        [SerializeField] private float gainEnterDuration = 0.3f;
        [LabelText("淡出时长(秒)")]
        [SerializeField] private float gainExitDuration = 0.2f;
        [LabelText("入场Y偏移(像素)")]
        [SerializeField] private float gainEnterYOffset = 30f;
        [LabelText("顺序间隔(秒)")]
        [SerializeField] private float gainBetweenDelay = 0.08f;
        private Coroutine gainsRoutine;

        [LabelText("对话解析器组件")]
        [Tooltip("提供基于 身份/状态/性别 的台词映射(键: Dialogue:[1..3])")] 
        [SerializeField] private MonoBehaviour dialogueResolverComponent;
        private IDialogueResolver dialogueResolver;

        [Title("时间配置")]
        [LabelText("Night开始后初始延迟（秒）")]
        [Tooltip("Night阶段开始后等待多久出队第1位顾客")]
        [SerializeField] private float initialDelay = 3f;

        [LabelText("正常出队间隔（秒）")]
        [Tooltip("第6位及之后每位顾客服务完成后的等待时间")]
        [SerializeField] private float normalSpawnInterval = 20f;

        [LabelText("快速服务数量")]
        [Tooltip("前N位顾客服务完成后立即出队下一位，无等待")]
        [SerializeField] private int fastServeCount = 5;

        [Title("品尝时间配置")]
        [LabelText("最短品尝时间（秒）")]
        [SerializeField] private float minDrinkingTime = 10f;

        [LabelText("最长品尝时间（秒）")]
        [SerializeField] private float maxDrinkingTime = 15f;

        [Title("上酒与前奏设置")]
        [LabelText("上酒前奏最短延迟（秒）")]
        [SerializeField] private float serveLeadDelayMin = 2f;
        [LabelText("上酒前奏最长延迟（秒）")]
        [SerializeField] private float serveLeadDelayMax = 3f;
        [LabelText("上酒音效音量")] 
        [SerializeField] private float serveSfxVolume = 1f;

        [Title("运行时状态")]
        [ShowInInspector] private bool isNightActive = false;
        [ShowInInspector] private int currentServedCount = 0;
        [ShowInInspector] private bool isServicing = false;
        [ShowInInspector] private bool isDrinking = false;
        [ShowInInspector] private float nextServeCountdown = 0f;

        // 当前饮用音效路径（本次服务复用：开头一次、结尾一次）
        private string currentDrinkingClipPath = null;

        #region Unity生命周期

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            Debug.Log("[CustomerService] CustomerServiceManager 初始化完成");

            if (dialogueResolverComponent != null)
            {
                dialogueResolver = dialogueResolverComponent as IDialogueResolver;
                if (dialogueResolver == null)
                {
                    Debug.LogWarning("[CustomerService] 提供的对话解析器组件未实现 IDialogueResolver 接口");
                }
            }
        }

        private void Start()
        {
            // 强制设置品尝时间为10-15秒（覆盖Inspector中可能的旧值）
            minDrinkingTime = 10f;
            maxDrinkingTime = 15f;
            Debug.Log($"[CustomerService] 品尝时间已强制设置为: {minDrinkingTime}-{maxDrinkingTime}秒");

            // 默认隐藏对话框
            if (dialogueRoot != null)
            {
                dialogueRoot.SetActive(false);
            }

            // 默认隐藏增益显示
            if (moodGainRoot != null) moodGainRoot.SetActive(false);
            if (moneyGainRoot != null) moneyGainRoot.SetActive(false);
            if (ratingGainRoot != null) ratingGainRoot.SetActive(false);

            // 检查当前阶段，如果已经是Night阶段则立即启动服务
            if (TimeSystemManager.Instance != null)
            {
                var currentPhase = TimeSystemManager.Instance.CurrentPhase;
                Debug.Log($"[CustomerService] Start时检查当前阶段: {currentPhase}");

                if (currentPhase == TimePhase.Night && !isNightActive)
                {
                    Debug.Log("[CustomerService] 检测到已处于Night阶段，立即启动服务");
                    OnPhaseChanged(TimePhase.Night);
                }
            }
            else
            {
                Debug.LogWarning("[CustomerService] TimeSystemManager.Instance为空");
            }
        }

        private void OnEnable()
        {
            // 订阅事件
            MessageManager.Register<TimePhase>(MessageDefine.PHASE_CHANGED, OnPhaseChanged);
            MessageManager.Register<object>(MessageDefine.COCKTAIL_DELIVERED, OnCocktailDelivered);
            // 订阅详细交付事件（来自 CustomerNpcView）
            MessageManager.Register<CustomerNpcView.CocktailDeliveryPayload>(CustomerNpcView.COCKTAIL_DELIVERED_DETAIL, OnCocktailDeliveredDetail);
            Debug.Log("[CustomerService] 事件订阅完成 - PHASE_CHANGED, COCKTAIL_DELIVERED");
        }

        private void OnDisable()
        {
            // 取消订阅
            MessageManager.Remove<TimePhase>(MessageDefine.PHASE_CHANGED, OnPhaseChanged);
            MessageManager.Remove<object>(MessageDefine.COCKTAIL_DELIVERED, OnCocktailDelivered);
            MessageManager.Remove<CustomerNpcView.CocktailDeliveryPayload>(CustomerNpcView.COCKTAIL_DELIVERED_DETAIL, OnCocktailDeliveredDetail);
        }

        private void Update()
        {
            // 更新倒计时显示（仅用于Inspector观察）
            if (nextServeCountdown > 0)
            {
                float deltaTime = Time.deltaTime;
                if (TimeSystemManager.Instance != null)
                {
                    deltaTime *= TimeSystemManager.Instance.GlobalTimeScale;
                }
                nextServeCountdown -= deltaTime;
            }
        }

        #endregion

        #region 事件处理

        private void OnPhaseChanged(TimePhase phase)
        {
            Debug.Log($"[CustomerService] 接收到阶段变更事件: {phase}");

            if (phase == TimePhase.Night)
            {
                isNightActive = true;
                currentServedCount = 0;
                StartCoroutine(CustomerServiceLoop());
                Debug.Log($"[CustomerService] Night阶段开始，{initialDelay}秒后第一位顾客到访");
            }
            else
            {
                isNightActive = false;
                StopAllCoroutines();
                Debug.Log($"[CustomerService] 阶段切换到{phase}，停止顾客服务");
            }
        }

        private Coroutine compatDeliverRoutine;
        private object compatCocktailDataCache;

        private void OnCocktailDelivered(object cocktailData)
        {
            // 兼容旧事件：等待一帧，若期间收到详细负载则放弃旧流程，避免“双启动”导致重复结算
            if (!isServicing || isDrinking)
            {
                if (!isServicing)
                    Debug.LogWarning("[CustomerService] 当前无顾客在服务中，无法交付鸡尾酒");
                if (isDrinking)
                    Debug.LogWarning("[CustomerService] 顾客正在品尝中，请稍后");
                return;
            }

            compatCocktailDataCache = cocktailData;
            if (compatDeliverRoutine != null) StopCoroutine(compatDeliverRoutine);
            compatDeliverRoutine = StartCoroutine(DelayStartCompatDelivery());
        }

        private IEnumerator DelayStartCompatDelivery()
        {
            // 等待一帧（或极短时长），给“详细负载”机会先到达
            yield return null; // 1 frame
            if (_hasPendingDeliveryPayload)
            {
                Debug.Log("[CustomerService] 本帧收到详细负载，放弃兼容事件流程");
                yield break;
            }
            Debug.Log($"[CustomerService] 顾客收到鸡尾酒（兼容事件），开始品尝...");
            StartCoroutine(CustomerDrinkingProcess(compatCocktailDataCache));
        }

        // ============= 新增：接收详细交付事件 =============
        private bool _hasPendingDeliveryPayload = false;
        private CustomerNpcView.CocktailDeliveryPayload _pendingDeliveryPayload;

        private void OnCocktailDeliveredDetail(CustomerNpcView.CocktailDeliveryPayload payload)
        {
            if (!isServicing || isDrinking)
            {
                if (!isServicing)
                    Debug.LogWarning("[CustomerService] 当前无顾客在服务中，无法交付鸡尾酒(详细)");
                if (isDrinking)
                    Debug.LogWarning("[CustomerService] 顾客正在品尝中，请稍后(详细)");
                return;
            }

            _pendingDeliveryPayload = payload;
            _hasPendingDeliveryPayload = true;
            Debug.Log($"[CustomerService] 收到详细交付数据 → 卡:{payload.baseCard?.nameEN} ΔM:{payload.moodDelta} 价格:{payload.price} 小费:{payload.tip} 收入:{payload.finalIncome} 评价:{payload.reputationDelta}");
            StartCoroutine(CustomerDrinkingProcessReal());
        }

        #endregion

        #region 服务流程

        /// <summary>
        /// 顾客服务主循环
        /// </summary>
        private IEnumerator CustomerServiceLoop()
        {
            // Night开始后延迟3秒
            yield return new WaitForSeconds(initialDelay);

            // 出队第1位顾客
            TryServeNextCustomer();
        }

        /// <summary>
        /// 尝试服务下一位顾客
        /// </summary>
        private void TryServeNextCustomer()
        {
            if (isServicing)
            {
                Debug.LogWarning("[CustomerService] 已有顾客在服务中，跳过出队");
                return;
            }

            if (CustomerSpawnManager.Instance == null)
            {
                Debug.LogError("[CustomerService] CustomerSpawnManager.Instance为空");
                return;
            }

            var data = CustomerSpawnManager.Instance.DequeueCustomer();
            if (data == null)
            {
                Debug.Log("[CustomerService] 队列为空，无更多顾客");
                return;
            }

            isServicing = true;
            isDrinking = false;
            currentServedCount++;

            Debug.Log($"[CustomerService] 开始服务第{currentServedCount}位顾客: {data.displayName} ({data.state}, {data.gender})");

            // 调用Initialize会自动处理上一位顾客淡出
            customerBehavior.Initialize(data, () =>
            {
                MessageManager.Send(MessageDefine.CUSTOMER_DEQUEUED, data);
                Debug.Log($"[CustomerService] 第{currentServedCount}位顾客入场完成，等待玩家拖拽鸡尾酒...");

                // 显示对话框（顾客立绘入场完成后）
                if (dialogueRoot != null)
                {
                    dialogueRoot.SetActive(true);
                }

                // 更新心情游标位置
                UpdateMoodMarker(data);

                // 入场时随机展示一条不与上次重复的台词（需在对话框激活后启动打字机）
                TryShowRandomDialogueForCurrentCustomer();
            });
        }

        /// <summary>
        /// 顾客品尝鸡尾酒流程
        /// </summary>
        private IEnumerator CustomerDrinkingProcess(object cocktailData)
        {
            isDrinking = true;

            // 随机品尝时间10-15秒
            float drinkingTime = Random.Range(minDrinkingTime, maxDrinkingTime);
            Debug.Log($"[CustomerService] 顾客品尝中... ({drinkingTime:F1}秒) [配置范围: {minDrinkingTime}-{maxDrinkingTime}秒]");

            // 台词框立即隐藏 + 上酒音效 → 等待2-3秒 → 再开始播放原先的品尝音效
            if (dialogueRoot != null) dialogueRoot.SetActive(false);
            if (AudioManager.instance != null)
            {
                AudioManager.instance.PlaySE(GlobalAudio.ServeDrink, Mathf.Clamp01(serveSfxVolume));
            }
            float serveLead = Random.Range(Mathf.Min(serveLeadDelayMin, serveLeadDelayMax), Mathf.Max(serveLeadDelayMin, serveLeadDelayMax));
            yield return new WaitForSeconds(serveLead);

            // 开始播放（原）品尝音效一次
            PlayDrinkingSoundAtStart();

            yield return new WaitForSeconds(drinkingTime);

            // 结算：若无详细负载，走模拟；否则交给真实结算协程
            var customerData = customerBehavior.CurrentData;
            if (!_hasPendingDeliveryPayload)
            {
                SimulateCocktailSettlement(customerData, debugMoodDelta, debugMoneyGain, debugRatingDelta);
                // 广播模拟结算（将 moneyGain 视作最终收入）
                BroadcastSettlement(new SettlementBroadcast
                {
                    npcId = customerData != null ? customerData.id : string.Empty,
                    moodDelta = debugMoodDelta,
                    price = debugMoneyGain,
                    tip = 0,
                    finalIncome = debugMoneyGain,
                    ratingDelta = debugRatingDelta
                });
            }
            else
            {
                ApplyRealCocktailSettlement(customerData, _pendingDeliveryPayload);
                _hasPendingDeliveryPayload = false;
                // 同步广播真实结算
                BroadcastSettlement(new SettlementBroadcast
                {
                    npcId = customerData != null ? customerData.id : string.Empty,
                    moodDelta = _pendingDeliveryPayload.moodDelta,
                    price = _pendingDeliveryPayload.price,
                    tip = _pendingDeliveryPayload.tip,
                    finalIncome = _pendingDeliveryPayload.finalIncome,
                    ratingDelta = _pendingDeliveryPayload.reputationDelta
                });
            }

            // 不再在结束前重复播放品尝音效（只在开始时播放一次）

            Debug.Log($"[CustomerService] 服务完成: {customerData.displayName} (第{currentServedCount}位)");

            // 顾客淡出离场
            customerBehavior.PlayExitAnimation(() =>
            {
                MessageManager.Send(MessageDefine.CUSTOMER_VISITED, customerData);
                isServicing = false;
                isDrinking = false;

                // 停止打字机并显示完整文本
                StopTypewriter();

                // 隐藏对话框（顾客离场时）
                if (dialogueRoot != null)
                {
                    dialogueRoot.SetActive(false);
                }

                // 离场时将三项增益做淡出
                FadeOutGains();

                // 判断是否需要等待
                if (currentServedCount < fastServeCount)
                {
                    // 前5位：立即服务下一位
                    Debug.Log($"[CustomerService] 前{fastServeCount}位快速服务，立即出队下一位");
                    TryServeNextCustomer();
                }
                else
                {
                    // 第6位起：等待20秒
                    Debug.Log($"[CustomerService] 第{currentServedCount}位已完成，{normalSpawnInterval}秒后服务下一位");
                    nextServeCountdown = normalSpawnInterval;
                    StartCoroutine(DelayedServeNext());
                }
            });
        }

        // 真实交付流程（已拥有详细payload时启用）
        private IEnumerator CustomerDrinkingProcessReal()
        {
            isDrinking = true;

            // 随机品尝时间10-15秒
            float drinkingTime = Random.Range(minDrinkingTime, maxDrinkingTime);
            Debug.Log($"[CustomerService] 顾客品尝中... ({drinkingTime:F1}秒) [配置范围: {minDrinkingTime}-{maxDrinkingTime}秒]");

            // 台词框立即隐藏 + 上酒音效 → 等待2-3秒 → 再开始播放原先的品尝音效
            if (dialogueRoot != null) dialogueRoot.SetActive(false);
            if (AudioManager.instance != null)
            {
                AudioManager.instance.PlaySE(GlobalAudio.ServeDrink, Mathf.Clamp01(serveSfxVolume));
            }
            float serveLead = Random.Range(Mathf.Min(serveLeadDelayMin, serveLeadDelayMax), Mathf.Max(serveLeadDelayMin, serveLeadDelayMax));
            yield return new WaitForSeconds(serveLead);

            // 开始播放（原）品尝音效一次
            PlayDrinkingSoundAtStart();

            yield return new WaitForSeconds(drinkingTime);

            var customerData = customerBehavior.CurrentData;
            // 记录用于广播的结算数据
            int bMood = 0, bPrice = 0, bTip = 0, bIncome = 0, bRating = 0;
            if (_hasPendingDeliveryPayload)
            {
                var p = _pendingDeliveryPayload;
                ApplyRealCocktailSettlement(customerData, p);
                bMood = p.moodDelta;
                bPrice = p.price;
                bTip = p.tip;
                bIncome = p.finalIncome;
                bRating = p.reputationDelta;
                _hasPendingDeliveryPayload = false;
            }
            else
            {
                Debug.LogWarning("[CustomerService] 详细负载丢失，回退到模拟结算");
                SimulateCocktailSettlement(customerData, debugMoodDelta, debugMoneyGain, debugRatingDelta);
                bMood = debugMoodDelta;
                bPrice = debugMoneyGain;
                bTip = 0;
                bIncome = debugMoneyGain;
                bRating = debugRatingDelta;
            }

            // 不再在结束前重复播放品尝音效（只在开始时播放一次）

            // 广播真实结算（或回退模拟值）
            BroadcastSettlement(new SettlementBroadcast
            {
                npcId = customerData != null ? customerData.id : string.Empty,
                moodDelta = bMood,
                price = bPrice,
                tip = bTip,
                finalIncome = bIncome,
                ratingDelta = bRating
            });

            Debug.Log($"[CustomerService] 服务完成: {customerData.displayName} (第{currentServedCount}位)");

            // 顾客淡出离场
            customerBehavior.PlayExitAnimation(() =>
            {
                MessageManager.Send(MessageDefine.CUSTOMER_VISITED, customerData);
                isServicing = false;
                isDrinking = false;

                // 停止打字机并显示完整文本
                StopTypewriter();

                // 隐藏对话框（顾客离场时）
                if (dialogueRoot != null)
                {
                    dialogueRoot.SetActive(false);
                }

                // 离场时将三项增益做淡出
                FadeOutGains();

                // 判断是否需要等待
                if (currentServedCount < fastServeCount)
                {
                    // 前5位：立即服务下一位
                    Debug.Log($"[CustomerService] 前{fastServeCount}位快速服务，立即出队下一位");
                    TryServeNextCustomer();
                }
                else
                {
                    // 第6位起：等待20秒
                    Debug.Log($"[CustomerService] 第{currentServedCount}位已完成，{normalSpawnInterval}秒后服务下一位");
                    nextServeCountdown = normalSpawnInterval;
                    StartCoroutine(DelayedServeNext());
                }
            });
        }

        // 使用详细payload驱动真实结算显示
        private void ApplyRealCocktailSettlement(NpcCharacterData customerData, CustomerNpcView.CocktailDeliveryPayload payload)
        {
            if (customerData == null) return;

            // 更新运行时心情并钳制
            currentCustomerMood = Mathf.Clamp(customerData.initialMood + payload.moodDelta, 0, moodMax);

            // 驱动心情游标到新位置（先前/后）
            UpdateMoodMarkerTemp(customerData.initialMood);
            UpdateMoodMarkerTemp(currentCustomerMood);

            // 顺序显示三项增益（心情 → 金钱 → 评价）
            if (gainsRoutine != null) StopCoroutine(gainsRoutine);
            gainsRoutine = StartCoroutine(ShowGainsSequence(payload.moodDelta, payload.finalIncome, payload.reputationDelta));

            // 首次做出：将鸡尾酒配方标记为“已发现/解锁”，写入配方书
            if (payload.cocktail != null && SaveManager.Instance != null)
            {
                try
                {
                    SaveManager.Instance.DiscoverRecipe(payload.cocktail);
                    MessageManager.Send<string>(MessageDefine.RECIPE_DISCOVERED, payload.cocktail.id.ToString());
                }
                catch { }
            }
        }

        private void BroadcastSettlement(SettlementBroadcast data)
        {
            Debug.Log($"[CustomerService] 广播结算 → ΔM:{data.moodDelta} 价格:{data.price} 小费:{data.tip} 收入:{data.finalIncome} 评价:{data.ratingDelta}");
            MessageManager.Send<CustomerServiceManager.SettlementBroadcast>(MessageDefine.SERVICE_PAYMENT_COMPLETE, data);
            // 动态写入存档：实时累计夜晚收入与评价
            if (SaveManager.Instance != null)
            {
                try { SaveManager.Instance.ApplyServiceGain(data.finalIncome, data.ratingDelta); }
                catch { }
            }
        }

        /// <summary>
        /// 在顾客开始品尝时播放喝酒音效（开头一次），并记录本次使用的音效路径。
        /// 规则：男性从品尝1-4随机一条；女性固定品尝5。
        /// </summary>
        private void PlayDrinkingSoundAtStart()
        {
            var data = customerBehavior?.CurrentData;
            if (data == null || AudioManager.instance == null) return;

            // 选择音效
            string clipPath;
            if (!string.IsNullOrEmpty(data.gender) && data.gender.ToLower() == "female")
            {
                clipPath = GlobalAudio.DrinkTaste5; // 女士固定用品尝5
            }
            else
            {
                // 男士从1-4随机一个
                int idx = Random.Range(1, 5); // 1..4
                clipPath = idx switch
                {
                    1 => GlobalAudio.DrinkTaste1,
                    2 => GlobalAudio.DrinkTaste2,
                    3 => GlobalAudio.DrinkTaste3,
                    _ => GlobalAudio.DrinkTaste4
                };
            }

            currentDrinkingClipPath = clipPath;
            // 开头立刻播放一次
            AudioManager.instance.PlaySE(currentDrinkingClipPath, 1f);
        }

        /// <summary>
        /// 延迟服务下一位顾客（第6位起）
        /// </summary>
        private IEnumerator DelayedServeNext()
        {
            float elapsed = 0f;

            while (elapsed < normalSpawnInterval && isNightActive)
            {
                float deltaTime = Time.deltaTime;
                if (TimeSystemManager.Instance != null)
                {
                    deltaTime *= TimeSystemManager.Instance.GlobalTimeScale;
                }
                elapsed += deltaTime;
                yield return null;
            }

            if (isNightActive)
            {
                nextServeCountdown = 0f;
                TryServeNextCustomer();
            }
        }

        /// <summary>
        /// 计算付款和评价（占位符）
        /// </summary>
        private void CalculatePaymentAndRating(NpcCharacterData customerData, object cocktailData)
        {
            // TODO: 根据实际游戏逻辑实现
            // 1. 计算基础价格（basePaymentDefault）
            // 2. 根据顾客满意度计算小费（identityMultiplier * satisfaction）
            // 3. 更新评价系统
            // 4. 触发金币增加效果

            Debug.Log($"[CustomerService] 结算完成 - 顾客: {customerData.displayName}");

            // 发送结算消息（供其他系统使用）
            // MessageManager.Send(MessageDefine.SERVICE_PAYMENT_COMPLETE, paymentData);
        }

        #endregion

        #region 台词选择与展示

        /// <summary>
        /// 尝试为当前顾客随机显示一条台词（避免与上次相同）。
        /// 需要在Inspector中绑定 dialogueText 与 dialogueResolverComponent。
        /// </summary>
        public void TryShowRandomDialogueForCurrentCustomer()
        {
            if (dialogueText == null) return;
            var data = customerBehavior?.CurrentData;
            if (data == null)
            {
                Debug.LogWarning("[CustomerService] 当前顾客数据为空，无法显示台词");
                return;
            }

            string line = PickNonRepeatingDialogue(data, out int chosenIndex);
            if (!string.IsNullOrEmpty(line))
            {
                if (enableTypewriter)
                {
                    if (typewriterRoutine != null) StopCoroutine(typewriterRoutine);
                    typewriterRoutine = StartCoroutine(PlayTMPTypewriter(line));
                }
                else
                {
                    dialogueText.maxVisibleCharacters = int.MaxValue;
                    dialogueText.text = line;
                }
                // 记录该NPC本次选择索引（用于下一次避免重复）
                if (CustomerSpawnManager.Instance != null)
                {
                    CustomerSpawnManager.Instance.SetLastDialogueIndex(data.id, chosenIndex);
                }
            }
            else
            {
                Debug.LogWarning($"[CustomerService] 未能为 {data.identityId}/{data.state}/{data.gender} 找到有效台词");
            }
        }

        /// <summary>
        /// 基于 身份/状态/性别 获取台词映射(键: Dialogue:[1..3])，随机选择且不与上次相同。
        /// </summary>
        private string PickNonRepeatingDialogue(NpcCharacterData data, out int chosenIndex)
        {
            chosenIndex = -1;
            if (dialogueResolver == null)
            {
                return null;
            }

            if (!dialogueResolver.TryGetDialogueMap(data.identityId, data.state, data.gender, out var map) || map == null || map.Count == 0)
            {
                return null;
            }

            // 收集可用下标
            var candidates = new System.Collections.Generic.List<int>(3);
            for (int i = 1; i <= 3; i++)
            {
                string key = $"Dialogue:[{i}]";
                if (map.ContainsKey(key) && !string.IsNullOrEmpty(map[key]))
                {
                    candidates.Add(i);
                }
            }
            if (candidates.Count == 0)
            {
                return null;
            }

            // 取上一次选择，用于防重复
            int lastIndex = -1;
            if (CustomerSpawnManager.Instance != null)
            {
                lastIndex = CustomerSpawnManager.Instance.GetLastDialogueIndex(data.id);
            }

            if (candidates.Count > 1 && lastIndex != -1)
            {
                candidates.Remove(lastIndex);
                if (candidates.Count == 0)
                {
                    // 如果全被移除，恢复候选（退化为可重复）
                    for (int i = 1; i <= 3; i++)
                    {
                        string key = $"Dialogue:[{i}]";
                        if (map.ContainsKey(key) && !string.IsNullOrEmpty(map[key]))
                        {
                            candidates.Add(i);
                        }
                    }
                }
            }

            int pickIdx = candidates[Random.Range(0, candidates.Count)];
            chosenIndex = pickIdx;
            return map[$"Dialogue:[{pickIdx}]"];
        }

        #endregion

        #region TMP 打字机实现

        private IEnumerator PlayTMPTypewriter(string fullText)
        {
            if (dialogueText == null)
                yield break;

            // 确保TMP节点已激活，避免characterCount为0导致直接显示完整
            if (!dialogueText.gameObject.activeInHierarchy)
            {
                // 等待下一帧，直到激活
                yield return null;
            }

            dialogueText.text = fullText;
            dialogueText.ForceMeshUpdate();
            int total = dialogueText.textInfo.characterCount;
            if (total <= 0)
            {
                dialogueText.maxVisibleCharacters = int.MaxValue;
                typewriterRoutine = null;
                yield break;
            }

            // 计算每字符间隔，确保总时长内完成
            float perChar = (typewriterTotalDuration > 0.01f) ? (typewriterTotalDuration / Mathf.Max(1, total)) : typewriterInterval;
            float holdBase = Mathf.Max(0f, typewriterPopHold);

            dialogueText.maxVisibleCharacters = 0;
            for (int i = 1; i <= total; i++)
            {
                dialogueText.maxVisibleCharacters = i;

                // 等一帧以确保几何顶点已经生成
                yield return null;

                int charIndex = i - 1;
                if (charIndex >= 0 && charIndex < dialogueText.textInfo.characterCount)
                {
                    var charInfo = dialogueText.textInfo.characterInfo[charIndex];
                    if (charInfo.isVisible)
                    {
                        int meshIndex = charInfo.materialReferenceIndex;
                        int vertexIndex = charInfo.vertexIndex;
                        var meshInfo = dialogueText.textInfo.meshInfo[meshIndex];
                        var vertices = meshInfo.vertices;

                        // 计算字符中心点（对角线中点）
                        Vector3 center = (vertices[vertexIndex] + vertices[vertexIndex + 2]) * 0.5f;

                        // 放大
                        for (int v = 0; v < 4; v++)
                        {
                            Vector3 offset = vertices[vertexIndex + v] - center;
                            vertices[vertexIndex + v] = center + offset * typewriterPopScale;
                        }
                        dialogueText.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);

                        // 停留一小段时间（不超过每字符间隔的大部分）
                        float hold = Mathf.Min(holdBase, perChar * 0.8f);
                        if (hold > 0f)
                            yield return new WaitForSeconds(hold);

                        // 还原
                        for (int v = 0; v < 4; v++)
                        {
                            Vector3 offset = vertices[vertexIndex + v] - center;
                            vertices[vertexIndex + v] = center + offset / Mathf.Max(0.0001f, typewriterPopScale);
                        }
                        dialogueText.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);

                        // 剩余时间
                        float tail = Mathf.Max(0f, perChar - hold);
                        if (tail > 0f)
                            yield return new WaitForSeconds(tail);
                        continue;
                    }
                }

                // 对于不可见字符（如空格等），直接等待整段时长
                yield return new WaitForSeconds(perChar);
            }
            typewriterRoutine = null;
        }

        private void StopTypewriter()
        {
            if (typewriterRoutine != null)
            {
                StopCoroutine(typewriterRoutine);
                typewriterRoutine = null;
            }
            if (dialogueText != null)
            {
                dialogueText.maxVisibleCharacters = int.MaxValue;
            }
        }

        // 取消连字符处理：不再需要辅助格式化函数

        #endregion

        #region 心情游标

        private void UpdateMoodMarker(NpcCharacterData data)
        {
            if (moodBarRect == null || moodMarkerRect == null || data == null) return;
            int clamped = Mathf.Clamp(data.initialMood, 0, Mathf.Max(1, moodMax));
            // 将0..moodMax 映射到条的宽度，落在每段中心
            float width = moodBarRect.rect.width;
            int segments = Mathf.Max(1, moodMax);
            float segmentWidth = width / segments;
            // 0心情放置在第1段中心？按设计通常从1开始，这里：
            // 若clamped==0则放第1段中心；若>=1则放对应段中心
            int slot = Mathf.Max(1, clamped); // 1..segments
            float x = -width * 0.5f + (slot - 0.5f) * segmentWidth;
            var anchored = moodMarkerRect.anchoredPosition;
            anchored.x = x;
            moodMarkerRect.anchoredPosition = anchored;
        }

        #endregion

        #region 结算与模拟接口

        /// <summary>
        /// 调试：模拟鸡尾酒结算。固定增加心情/金钱/评价，并驱动UI显示与游标动画。
        /// </summary>
        private void SimulateCocktailSettlement(NpcCharacterData customerData, int moodDelta, int moneyGain, int ratingDelta)
        {
            if (customerData == null) return;

            // 1) 更新运行时心情并钳制
            currentCustomerMood = Mathf.Clamp(customerData.initialMood + moodDelta, 0, moodMax);

            // 2) 驱动心情条游标到新位置
            // 暂用 initialMood 显示前位置，再用 currentCustomerMood 更新后位置
            UpdateMoodMarkerTemp(customerData.initialMood);
            UpdateMoodMarkerTemp(currentCustomerMood);

            // 3) 顺序显示三项增益（心情 → 金钱 → 评价）
            if (gainsRoutine != null) StopCoroutine(gainsRoutine);
            gainsRoutine = StartCoroutine(ShowGainsSequence(moodDelta, moneyGain, ratingDelta));

            // 4) 留出接口：真实鸡尾酒结算入口
            // HandleRealCocktailSettlement(cocktailData);
        }

        /// <summary>
        /// 真正鸡尾酒拖拽后的结算入口（预留）。
        /// 参数可替换成你实际的鸡尾酒数据类型。
        /// </summary>
        public void HandleRealCocktailSettlement(object realCocktailData)
        {
            // TODO: 根据 realCocktailData 计算 moodDelta/moneyGain/ratingDelta
            // 示例：
            // int moodDelta = CalculateMoodDelta(realCocktailData, customerBehavior.CurrentData);
            // int moneyGain = CalculateMoney(realCocktailData);
            // int ratingDelta = CalculateRating(realCocktailData);
            // SimulateCocktailSettlement(customerBehavior.CurrentData, moodDelta, moneyGain, ratingDelta);
        }

        private IEnumerator ShowGainsSequence(int moodDelta, int moneyGain, int ratingDelta)
        {
            // 统一重置初始状态
            ResetGainItem(moodGainRoot);
            ResetGainItem(moneyGainRoot);
            ResetGainItem(ratingGainRoot);

            // 心情
            if (moodGainRoot != null)
            {
                if (moodGainText != null) moodGainText.text = $"+{moodDelta}";
                yield return PlayGainEnter(moodGainRoot);
            }
            yield return new WaitForSeconds(gainBetweenDelay);

            // 金钱
            if (moneyGainRoot != null)
            {
                if (moneyGainText != null) moneyGainText.text = $"+{moneyGain}";
                yield return PlayGainEnter(moneyGainRoot);
            }
            yield return new WaitForSeconds(gainBetweenDelay);

            // 评价
            if (ratingGainRoot != null)
            {
                if (ratingGainText != null) ratingGainText.text = $"+{ratingDelta}";
                yield return PlayGainEnter(ratingGainRoot);
            }
        }

        private void ResetGainItem(GameObject root)
        {
            if (root == null) return;
            var cg = root.GetComponent<CanvasGroup>();
            if (cg == null) cg = root.AddComponent<CanvasGroup>();
            var rt = root.transform as RectTransform;
            if (rt == null) return;
            root.SetActive(true);
            cg.alpha = 0f;
            var pos = rt.anchoredPosition;
            pos.y -= gainEnterYOffset;
            rt.anchoredPosition = pos;
        }

        private Tween CreateEnterTween(GameObject root)
        {
            var cg = root.GetComponent<CanvasGroup>();
            var rt = root.transform as RectTransform;
            var seq = DOTween.Sequence();
            seq.Join(cg.DOFade(1f, gainEnterDuration));
            seq.Join(rt.DOAnchorPosY(rt.anchoredPosition.y + gainEnterYOffset, gainEnterDuration).SetEase(Ease.OutCubic));
            return seq;
        }

        private IEnumerator PlayGainEnter(GameObject root)
        {
            if (root == null) yield break;
            var tween = CreateEnterTween(root);
            yield return tween.WaitForCompletion();
        }

        private void FadeOutGains()
        {
            FadeOutGain(moodGainRoot);
            FadeOutGain(moneyGainRoot);
            FadeOutGain(ratingGainRoot);
        }

        private void FadeOutGain(GameObject root)
        {
            if (root == null) return;
            var cg = root.GetComponent<CanvasGroup>();
            if (cg == null) cg = root.AddComponent<CanvasGroup>();
            cg.DOFade(0f, gainExitDuration);
        }

        // 仅用整数位置更新游标（不改变SO数据），用于动画起止
        private void UpdateMoodMarkerTemp(int moodValue)
        {
            if (moodBarRect == null || moodMarkerRect == null) return;
            int clamped = Mathf.Clamp(moodValue, 0, Mathf.Max(1, moodMax));
            float width = moodBarRect.rect.width;
            int segments = Mathf.Max(1, moodMax);
            float segmentWidth = width / segments;
            int slot = Mathf.Max(1, clamped);
            float x = -width * 0.5f + (slot - 0.5f) * segmentWidth;
            var anchored = moodMarkerRect.anchoredPosition;
            anchored.x = x;
            moodMarkerRect.anchoredPosition = anchored;
        }

        #endregion

        #region 编辑器调试

#if UNITY_EDITOR
        [Title("调试工具")]

        [Button("模拟交付鸡尾酒", ButtonSizes.Large), GUIColor(0.3f, 1f, 0.3f)]
        [InfoBox("模拟玩家拖拽鸡尾酒卡牌给顾客（用于测试）")]
        private void DebugDeliverCocktail()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[CustomerService] 仅在运行时可用");
                return;
            }

            OnCocktailDelivered(null);
        }

        [Button("强制出队下一位顾客", ButtonSizes.Medium)]
        private void DebugForceNextCustomer()
        {
            if (!Application.isPlaying) return;

            StopAllCoroutines();
            TryServeNextCustomer();
        }

        [Button("显示服务状态", ButtonSizes.Medium)]
        private void DebugShowStatus()
        {
            if (!Application.isPlaying) return;

            Debug.Log($"========== CustomerServiceManager 状态 ==========");
            Debug.Log($"Night激活: {isNightActive}");
            Debug.Log($"已服务数量: {currentServedCount}");
            Debug.Log($"正在服务: {isServicing}");
            Debug.Log($"正在品尝: {isDrinking}");
            Debug.Log($"当前顾客: {customerBehavior?.CurrentData?.displayName ?? "无"}");
            Debug.Log($"下次出队倒计时: {nextServeCountdown:F1}秒");
            Debug.Log($"队列剩余: {CustomerSpawnManager.Instance?.GetQueueCount() ?? 0}位");
            Debug.Log($"===============================================");
        }
#endif

        #endregion
    }
}
