using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Linq;
using TabernaNoctis.CharacterDesign;

namespace TabernaNoctis.NightScreen
{
    /// <summary>
    /// 顾客NPC行为测试器
    /// 不依赖到访系统，独立测试CustomerNpcBehavior的完整入场动画功能
    /// 测试流程：侧身走路 → 淡出 → 立绘淡入 → 服务 → 离场
    /// 使用方式：挂载到测试场景中，配置好引用后点击Inspector按钮测试
    /// </summary>
    public class CustomerNpcBehaviorTester : MonoBehaviour
    {
        [Title("测试配置")]
        [InfoBox("此脚本用于独立测试CustomerNpcBehavior组件的完整入场动画，包括侧身走路→淡出→立绘淡入流程")]
        
        [Header("行为组件引用")]
        [Required("必须分配CustomerNpcBehavior组件")]
        [SerializeField] private CustomerNpcBehavior customerBehavior;
        
        [Header("数据源")]
        [Tooltip("从NpcDatabase加载所有NPC数据")]
        [SerializeField] private NpcDatabase npcDatabase;
        
        [Tooltip("手动指定测试用的NPC数据")]
        [SerializeField] private NpcCharacterData manualTestData;

        [Header("测试选项")]
        [InfoBox("所有测试按钮都会播放完整动画效果，包括脚步声音效（音量0.5，侧身开始淡出时停止）")]
        
        [Title("运行时状态")]
        [ShowInInspector, ReadOnly]
        private List<NpcCharacterData> availableNpcs = new List<NpcCharacterData>();
        
        [ShowInInspector, ReadOnly]
        private int currentTestIndex = 0;

        private void Start()
        {
            LoadAvailableNpcs();
            
            if (availableNpcs.Count > 0)
            {
                Debug.Log($"[CustomerNpcBehaviorTester] 加载了 {availableNpcs.Count} 个NPC数据");
            }
            else
            {
                Debug.LogWarning("[CustomerNpcBehaviorTester] 未找到任何NPC数据！请配置NpcDatabase或manualTestData");
            }
        }

        /// <summary>
        /// 加载可用的NPC数据
        /// </summary>
        private void LoadAvailableNpcs()
        {
            availableNpcs.Clear();

            // 优先从NpcDatabase加载
            if (npcDatabase != null && npcDatabase.allNpcs != null)
            {
                availableNpcs.AddRange(npcDatabase.allNpcs);
                Debug.Log($"[CustomerNpcBehaviorTester] 从NpcDatabase加载了 {availableNpcs.Count} 个NPC");
            }

            // 如果没有数据库，尝试从Resources/Scripts/0_ScriptableObject/NpcDatabase加载
            if (availableNpcs.Count == 0)
            {
                var db = Resources.Load<NpcDatabase>("NpcDatabase");
                if (db != null && db.allNpcs != null)
                {
                    npcDatabase = db;
                    availableNpcs.AddRange(db.allNpcs);
                    Debug.Log($"[CustomerNpcBehaviorTester] 从Resources加载了 {availableNpcs.Count} 个NPC");
                }
            }

            // 如果还是没有，添加手动指定的数据
            if (availableNpcs.Count == 0 && manualTestData != null)
            {
                availableNpcs.Add(manualTestData);
                Debug.Log("[CustomerNpcBehaviorTester] 使用手动指定的测试数据");
            }
        }

        [Button("测试：完整入场动画（手动NPC）", ButtonSizes.Large), GUIColor(0.3f, 1f, 0.3f)]
        [InfoBox("播放手动指定NPC的完整入场动画：侧身走路→淡出→立绘淡入")]
        public void TestFullEnterAnimationManual()
        {
            if (customerBehavior == null)
            {
                Debug.LogError("[CustomerNpcBehaviorTester] customerBehavior 未分配！");
                return;
            }

            if (manualTestData == null)
            {
                Debug.LogError("[CustomerNpcBehaviorTester] manualTestData 未分配！请先在Inspector中指定一个NpcCharacterData");
                return;
            }

            Debug.Log($"[CustomerNpcBehaviorTester] 开始完整入场动画: {manualTestData.displayName} ({manualTestData.state}, {manualTestData.gender})");
            
            customerBehavior.Initialize(manualTestData, () =>
            {
                Debug.Log($"[CustomerNpcBehaviorTester] ?7?3 入场动画完成: {manualTestData.displayName}");
            });
        }

        [Button("测试：完整入场动画（随机NPC）", ButtonSizes.Large), GUIColor(0.3f, 0.7f, 1f)]
        [InfoBox("从NpcDatabase中随机选择一个NPC并播放完整入场动画")]
        public void TestFullEnterAnimationRandom()
        {
            if (customerBehavior == null)
            {
                Debug.LogError("[CustomerNpcBehaviorTester] customerBehavior 未分配！");
                return;
            }

            if (availableNpcs.Count == 0)
            {
                LoadAvailableNpcs();
            }

            if (availableNpcs.Count == 0)
            {
                Debug.LogError("[CustomerNpcBehaviorTester] 没有可用的NPC数据！请配置NpcDatabase或manualTestData");
                return;
            }

            var randomNpc = availableNpcs[Random.Range(0, availableNpcs.Count)];
            Debug.Log($"[CustomerNpcBehaviorTester] 开始随机NPC入场动画: {randomNpc.displayName} ({randomNpc.state}, {randomNpc.gender})");
            
            customerBehavior.Initialize(randomNpc, () =>
            {
                Debug.Log($"[CustomerNpcBehaviorTester] ?7?3 随机NPC入场动画完成: {randomNpc.displayName}");
            });
        }

        [Button("测试：下一个NPC入场动画", ButtonSizes.Large), GUIColor(0.7f, 0.7f, 1f)]
        [InfoBox("按顺序播放NpcDatabase中下一个NPC的完整入场动画")]
        public void TestNextNpcEnterAnimation()
        {
            if (customerBehavior == null)
            {
                Debug.LogError("[CustomerNpcBehaviorTester] customerBehavior 未分配！");
                return;
            }

            if (availableNpcs.Count == 0)
            {
                LoadAvailableNpcs();
            }

            if (availableNpcs.Count == 0)
            {
                Debug.LogError("[CustomerNpcBehaviorTester] 没有可用的NPC数据！请配置NpcDatabase或manualTestData");
                return;
            }

            currentTestIndex = (currentTestIndex + 1) % availableNpcs.Count;
            var npc = availableNpcs[currentTestIndex];
            Debug.Log($"[CustomerNpcBehaviorTester] 开始第 {currentTestIndex + 1}/{availableNpcs.Count} 个NPC入场动画: {npc.displayName} ({npc.state}, {npc.gender})");
            
            customerBehavior.Initialize(npc, () =>
            {
                Debug.Log($"[CustomerNpcBehaviorTester] ?7?3 第 {currentTestIndex + 1} 个NPC入场动画完成: {npc.displayName}");
            });
        }

        [Button("测试：按状态入场动画", ButtonSizes.Medium)]
        [InfoBox("播放指定状态NPC的完整入场动画（Busy, Friendly, Irritable, Melancholy, Picky）")]
        public void TestEnterAnimationByState(string state)
        {
            if (customerBehavior == null)
            {
                Debug.LogError("[CustomerNpcBehaviorTester] customerBehavior 未分配！");
                return;
            }

            if (availableNpcs.Count == 0)
            {
                LoadAvailableNpcs();
            }

            var npc = availableNpcs.FirstOrDefault(n => n.state == state);
            if (npc != null)
            {
                Debug.Log($"[CustomerNpcBehaviorTester] 开始 {state} 状态NPC入场动画: {npc.displayName} ({npc.gender})");
                
                customerBehavior.Initialize(npc, () =>
                {
                    Debug.Log($"[CustomerNpcBehaviorTester] ?7?3 {state} 状态NPC入场动画完成: {npc.displayName}");
                });
            }
            else
            {
                Debug.LogWarning($"[CustomerNpcBehaviorTester] 未找到状态为 {state} 的NPC");
            }
        }

        [Button("测试：离场动画", ButtonSizes.Large), GUIColor(1f, 0.5f, 0.3f)]
        [InfoBox("播放当前顾客的离场动画")]
        public void TestExitAnimation()
        {
            if (customerBehavior == null)
            {
                Debug.LogError("[CustomerNpcBehaviorTester] customerBehavior 未分配！");
                return;
            }

            if (!customerBehavior.IsServicing)
            {
                Debug.LogWarning("[CustomerNpcBehaviorTester] 当前没有顾客在服务中，无法播放离场动画");
                return;
            }

            Debug.Log($"[CustomerNpcBehaviorTester] 开始离场动画: {customerBehavior.CurrentData?.displayName}");
            
            customerBehavior.PlayExitAnimation(() =>
            {
                Debug.Log($"[CustomerNpcBehaviorTester] ?7?3 离场动画完成: {customerBehavior.CurrentData?.displayName}");
            });
        }

        [Button("重置行为组件", ButtonSizes.Medium), GUIColor(1f, 1f, 0.3f)]
        [InfoBox("重置CustomerNpcBehavior组件状态")]
        public void TestResetBehavior()
        {
            if (customerBehavior == null)
            {
                Debug.LogError("[CustomerNpcBehaviorTester] customerBehavior 未分配！");
                return;
            }

            customerBehavior.ResetState();
            Debug.Log("[CustomerNpcBehaviorTester] 行为组件已重置");
        }

        [Title("快速状态入场动画测试")]
        [InfoBox("点击按钮播放对应状态NPC的完整入场动画")]
        
        [HorizontalGroup("States")]
        [Button("Busy (绿)"), GUIColor(0f, 1f, 0f)]
        private void TestBusy() => TestEnterAnimationByState("Busy");

        [HorizontalGroup("States")]
        [Button("Friendly (蓝紫)"), GUIColor(0.54f, 0.17f, 0.89f)]
        private void TestFriendly() => TestEnterAnimationByState("Friendly");

        [HorizontalGroup("States")]
        [Button("Irritable (红)"), GUIColor(1f, 0f, 0f)]
        private void TestIrritable() => TestEnterAnimationByState("Irritable");

        [HorizontalGroup("States2")]
        [Button("Melancholy (青)"), GUIColor(0f, 1f, 1f)]
        private void TestMelancholy() => TestEnterAnimationByState("Melancholy");

        [HorizontalGroup("States2")]
        [Button("Picky (黄)"), GUIColor(1f, 1f, 0f)]
        private void TestPicky() => TestEnterAnimationByState("Picky");

        [Title("调试信息")]
        [Button("刷新NPC列表", ButtonSizes.Medium)]
        public void RefreshNpcList()
        {
            LoadAvailableNpcs();
            Debug.Log($"[CustomerNpcBehaviorTester] 已刷新，当前有 {availableNpcs.Count} 个NPC");
        }

        [Button("打印所有NPC", ButtonSizes.Medium)]
        public void PrintAllNpcs()
        {
            if (availableNpcs.Count == 0)
            {
                LoadAvailableNpcs();
            }

            Debug.Log($"========== 所有NPC列表 (共{availableNpcs.Count}个) ==========");
            for (int i = 0; i < availableNpcs.Count; i++)
            {
                var npc = availableNpcs[i];
                Debug.Log($"{i + 1}. {npc.displayName} ({npc.state}, {npc.gender}) - ID: {npc.id}");
            }
            Debug.Log("==============================================");
        }

        [Button("验证组件引用", ButtonSizes.Medium)]
        public void ValidateReferences()
        {
            bool valid = true;

            if (customerBehavior == null)
            {
                Debug.LogError("[CustomerNpcBehaviorTester] ?7?4 customerBehavior 未分配！");
                valid = false;
            }
            else
            {
                Debug.Log("[CustomerNpcBehaviorTester] ?7?7 customerBehavior 已分配");
                
                // 验证CustomerNpcBehavior的内部引用
                if (customerBehavior.IsAnimating)
                {
                    Debug.Log("[CustomerNpcBehaviorTester] ?7?2 当前正在播放动画");
                }
                
                if (customerBehavior.IsServicing)
                {
                    Debug.Log($"[CustomerNpcBehaviorTester] ?6?7 当前服务中的顾客: {customerBehavior.CurrentData?.displayName}");
                }
            }

            if (npcDatabase == null && manualTestData == null)
            {
                Debug.LogWarning("[CustomerNpcBehaviorTester] ?7?2 未分配任何数据源（npcDatabase 和 manualTestData 都为空）");
            }
            else if (npcDatabase != null)
            {
                Debug.Log($"[CustomerNpcBehaviorTester] ?7?7 npcDatabase 已分配 (包含 {npcDatabase.allNpcs?.Count ?? 0} 个NPC)");
            }
            else if (manualTestData != null)
            {
                Debug.Log($"[CustomerNpcBehaviorTester] ?7?7 manualTestData 已分配: {manualTestData.displayName} ({manualTestData.state}, {manualTestData.gender})");
            }

            if (valid)
            {
                Debug.Log("[CustomerNpcBehaviorTester] ?7?3 所有必要组件引用验证通过");
            }
            else
            {
                Debug.LogError("[CustomerNpcBehaviorTester] ?7?4 组件引用验证失败，请检查Inspector配置");
            }
        }

        [Title("完整流程测试")]
        [Button("测试：完整服务流程", ButtonSizes.Large), GUIColor(0.8f, 0.3f, 1f)]
        [InfoBox("播放完整的顾客服务流程：入场动画 → 等待3秒 → 离场动画")]
        public void TestFullServiceFlow()
        {
            if (customerBehavior == null)
            {
                Debug.LogError("[CustomerNpcBehaviorTester] customerBehavior 未分配！");
                return;
            }

            if (availableNpcs.Count == 0)
            {
                LoadAvailableNpcs();
            }

            if (availableNpcs.Count == 0)
            {
                Debug.LogError("[CustomerNpcBehaviorTester] 没有可用的NPC数据！");
                return;
            }

            var randomNpc = availableNpcs[Random.Range(0, availableNpcs.Count)];
            Debug.Log($"[CustomerNpcBehaviorTester] ?9?0 开始完整服务流程: {randomNpc.displayName} ({randomNpc.state}, {randomNpc.gender})");
            
            // 1. 播放入场动画
            customerBehavior.Initialize(randomNpc, () =>
            {
                Debug.Log($"[CustomerNpcBehaviorTester] ?7?3 入场动画完成，3秒后自动离场");
                
                // 2. 等待3秒后自动离场
                StartCoroutine(DelayedExitCoroutine(3f));
            });
        }

        [Button("测试：连续顾客切换", ButtonSizes.Large), GUIColor(1f, 0.6f, 0.2f)]
        [InfoBox("测试连续顾客切换：上一位顾客淡出 → 新顾客入场")]
        public void TestContinuousCustomerSwitch()
        {
            if (customerBehavior == null)
            {
                Debug.LogError("[CustomerNpcBehaviorTester] customerBehavior 未分配！");
                return;
            }

            if (availableNpcs.Count < 2)
            {
                LoadAvailableNpcs();
            }

            if (availableNpcs.Count < 2)
            {
                Debug.LogError("[CustomerNpcBehaviorTester] 需要至少2个NPC数据才能测试连续切换！");
                return;
            }

            StartCoroutine(ContinuousSwitchCoroutine());
        }

        [Button("测试：2个顾客完整进场流程", ButtonSizes.Large), GUIColor(0.2f, 1f, 0.8f)]
        [InfoBox("完整测试2个顾客的进场流程：第1个顾客完整入场 → 第2个顾客到来（第1个淡出+第2个入场）")]
        public void TestTwoCustomersCompleteFlow()
        {
            if (customerBehavior == null)
            {
                Debug.LogError("[CustomerNpcBehaviorTester] customerBehavior 未分配！");
                return;
            }

            if (availableNpcs.Count < 2)
            {
                LoadAvailableNpcs();
            }

            if (availableNpcs.Count < 2)
            {
                Debug.LogError("[CustomerNpcBehaviorTester] 需要至少2个NPC数据才能测试2个顾客流程！");
                return;
            }

            StartCoroutine(TwoCustomersCompleteFlowCoroutine());
        }

        private System.Collections.IEnumerator ContinuousSwitchCoroutine()
        {
            for (int i = 0; i < Mathf.Min(5, availableNpcs.Count); i++)
            {
                var npc = availableNpcs[i];
                Debug.Log($"[CustomerNpcBehaviorTester] ?9?4 切换到第 {i + 1} 位顾客: {npc.displayName} ({npc.state}, {npc.gender})");
                
                bool animationComplete = false;
                
                customerBehavior.Initialize(npc, () =>
                {
                    Debug.Log($"[CustomerNpcBehaviorTester] ?7?3 第 {i + 1} 位顾客入场完成");
                    animationComplete = true;
                });

                // 等待入场动画完成
                yield return new WaitUntil(() => animationComplete);
                
                // 等待2秒观察效果
                yield return new WaitForSeconds(2f);
            }
            
            Debug.Log("[CustomerNpcBehaviorTester] ?9?5 连续切换测试完成");
        }

        private System.Collections.IEnumerator TwoCustomersCompleteFlowCoroutine()
        {
            // 确保从干净状态开始
            customerBehavior.ResetState();
            yield return new WaitForSeconds(0.5f);

            // 选择两个不同的顾客
            var customer1 = availableNpcs[0];
            var customer2 = availableNpcs[1];

            Debug.Log($"[CustomerNpcBehaviorTester] ?9?0 开始2个顾客完整进场流程测试");
            Debug.Log($"[CustomerNpcBehaviorTester] 第1个顾客: {customer1.displayName} ({customer1.state}, {customer1.gender})");
            Debug.Log($"[CustomerNpcBehaviorTester] 第2个顾客: {customer2.displayName} ({customer2.state}, {customer2.gender})");

            // === 第1个顾客完整入场流程 ===
            Debug.Log($"[CustomerNpcBehaviorTester] ?9?9 阶段1：第1个顾客开始入场动画");
            
            bool customer1EnterComplete = false;
            customerBehavior.Initialize(customer1, () =>
            {
                Debug.Log($"[CustomerNpcBehaviorTester] ?7?3 第1个顾客入场动画完成: {customer1.displayName}");
                customer1EnterComplete = true;
            });

            // 等待第1个顾客完整入场动画完成（侧身走路→淡出→立绘淡入）
            yield return new WaitUntil(() => customer1EnterComplete);
            
            // 让第1个顾客停留一段时间，观察效果
            Debug.Log($"[CustomerNpcBehaviorTester] ?7?7 第1个顾客停留中，3秒后第2个顾客到来...");
            yield return new WaitForSeconds(3f);

            // === 第2个顾客到来，触发第1个顾客离场 + 第2个顾客入场 ===
            Debug.Log($"[CustomerNpcBehaviorTester] ?9?9 阶段2：第2个顾客到来，第1个顾客开始离场");
            
            bool customer2EnterComplete = false;
            customerBehavior.Initialize(customer2, () =>
            {
                Debug.Log($"[CustomerNpcBehaviorTester] ?7?3 第2个顾客入场动画完成: {customer2.displayName}");
                Debug.Log($"[CustomerNpcBehaviorTester] ?9?7 顾客切换流程：{customer1.displayName} → {customer2.displayName}");
                customer2EnterComplete = true;
            });

            // 等待第2个顾客完整入场动画完成
            yield return new WaitUntil(() => customer2EnterComplete);
            
            // 让第2个顾客停留一段时间，观察最终效果
            Debug.Log($"[CustomerNpcBehaviorTester] ?7?7 第2个顾客停留中，2秒后测试结束...");
            yield return new WaitForSeconds(2f);

            Debug.Log($"[CustomerNpcBehaviorTester] ?9?5 2个顾客完整进场流程测试完成！");
            Debug.Log($"[CustomerNpcBehaviorTester] ?9?6 测试总结：");
            Debug.Log($"[CustomerNpcBehaviorTester] - 第1个顾客完整入场：侧身走路→淡出→立绘淡入 ?7?3");
            Debug.Log($"[CustomerNpcBehaviorTester] - 第2个顾客到来时第1个顾客淡出离场 ?7?3");
            Debug.Log($"[CustomerNpcBehaviorTester] - 第2个顾客完整入场：侧身走路→淡出→立绘淡入 ?7?3");
        }

        private System.Collections.IEnumerator DelayedExitCoroutine(float delay)
        {
            yield return new WaitForSeconds(delay);
            
            if (customerBehavior != null && customerBehavior.IsServicing)
            {
                Debug.Log($"[CustomerNpcBehaviorTester] ?7?4 开始自动离场: {customerBehavior.CurrentData?.displayName}");
                
                customerBehavior.PlayExitAnimation(() =>
                {
                    Debug.Log($"[CustomerNpcBehaviorTester] ?9?5 完整服务流程结束: {customerBehavior.CurrentData?.displayName}");
                });
            }
        }
    }
}

