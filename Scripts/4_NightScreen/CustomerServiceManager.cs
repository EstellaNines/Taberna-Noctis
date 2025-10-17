using System;
using System.Collections;
using UnityEngine;
using TabernaNoctis.CharacterDesign;
using Sirenix.OdinInspector;

namespace TabernaNoctis.NightScreen
{
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

        [Title("组件引用")]
        [InfoBox("场景中固定的CustomerNpcBehavior组件，复用显示所有顾客")]
        [Required("必须分配CustomerNpcBehavior组件")]
        [SerializeField] private CustomerNpcBehavior customerBehavior;

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

        [Title("运行时状态")]
        [ShowInInspector] private bool isNightActive = false;
        [ShowInInspector] private int currentServedCount = 0;
        [ShowInInspector] private bool isServicing = false;
        [ShowInInspector] private bool isDrinking = false;
        [ShowInInspector] private float nextServeCountdown = 0f;

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
        }

        private void Start()
        {
            // 强制设置品尝时间为10-15秒（覆盖Inspector中可能的旧值）
            minDrinkingTime = 10f;
            maxDrinkingTime = 15f;
            Debug.Log($"[CustomerService] 品尝时间已强制设置为: {minDrinkingTime}-{maxDrinkingTime}秒");
            
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
            Debug.Log("[CustomerService] 事件订阅完成 - PHASE_CHANGED, COCKTAIL_DELIVERED");
        }

        private void OnDisable()
        {
            // 取消订阅
            MessageManager.Remove<TimePhase>(MessageDefine.PHASE_CHANGED, OnPhaseChanged);
            MessageManager.Remove<object>(MessageDefine.COCKTAIL_DELIVERED, OnCocktailDelivered);
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

        private void OnCocktailDelivered(object cocktailData)
        {
            if (!isServicing || isDrinking)
            {
                if (!isServicing)
                    Debug.LogWarning("[CustomerService] 当前无顾客在服务中，无法交付鸡尾酒");
                if (isDrinking)
                    Debug.LogWarning("[CustomerService] 顾客正在品尝中，请稍后");
                return;
            }

            Debug.Log($"[CustomerService] 顾客收到鸡尾酒，开始品尝...");
            StartCoroutine(CustomerDrinkingProcess(cocktailData));
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
            });
        }

        /// <summary>
        /// 顾客品尝鸡尾酒流程
        /// </summary>
        private IEnumerator CustomerDrinkingProcess(object cocktailData)
        {
            isDrinking = true;

            // 随机品尝时间10-15秒
            float drinkingTime = UnityEngine.Random.Range(minDrinkingTime, maxDrinkingTime);
            Debug.Log($"[CustomerService] 顾客品尝中... ({drinkingTime:F1}秒) [配置范围: {minDrinkingTime}-{maxDrinkingTime}秒]");

            yield return new WaitForSeconds(drinkingTime);

            // 结算：给钱、小费、评价
            var customerData = customerBehavior.CurrentData;
            CalculatePaymentAndRating(customerData, cocktailData);

            Debug.Log($"[CustomerService] 服务完成: {customerData.displayName} (第{currentServedCount}位)");

            // 顾客淡出离场
            customerBehavior.PlayExitAnimation(() =>
            {
                MessageManager.Send(MessageDefine.CUSTOMER_VISITED, customerData);
                isServicing = false;
                isDrinking = false;

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
