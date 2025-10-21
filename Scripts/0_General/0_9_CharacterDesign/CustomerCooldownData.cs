using System;
using System.Collections.Generic;

namespace TabernaNoctis.CharacterDesign
{
    /// <summary>
    /// 顾客冷却数据：用于保存系统持久化
    /// </summary>
    [Serializable]
    public class CustomerCooldownData
    {
        /// <summary>顾客ID（如 CompanyEmployee_001_M）</summary>
        public string npcId;
        
        /// <summary>剩余冷却计数（需要再有N位顾客到访才解冻）</summary>
        public int remainingCooldown;

        public CustomerCooldownData() { }

        public CustomerCooldownData(string npcId, int remainingCooldown)
        {
            this.npcId = npcId;
            this.remainingCooldown = remainingCooldown;
        }

        public override string ToString()
        {
            return $"[Cooldown] {npcId}: {remainingCooldown} 剩余";
        }
    }

    /// <summary>
    /// 夜间顾客系统完整状态：用于跨天持久化
    /// </summary>
    [Serializable]
    public class NightCustomerState
    {
        /// <summary>当前队列中的顾客ID列表（按入队顺序）</summary>
        public List<string> queuedNpcIds = new List<string>();
        
        /// <summary>冷却池：等待解冻的顾客</summary>
        public List<CustomerCooldownData> cooldownPool = new List<CustomerCooldownData>();
        
        /// <summary>可用池：可以被抽取的顾客ID列表</summary>
        public List<string> availablePool = new List<string>();
        
        /// <summary>全局到访计数（用于冷却机制）</summary>
        public int globalVisitorCount = 0;
        
        /// <summary>总生成计数（用于快速生成逻辑）</summary>
        public int totalSpawnedCount = 0;
        
        /// <summary>生成计时器累积时间</summary>
        public float spawnTimer = 0f;
        
        /// <summary>保底池：最后3位顾客的ID（营业结束时若未到访则直接归还）</summary>
        public List<string> guaranteeIds = new List<string>();
        
        /// <summary>已到访顾客ID集合（用于保底机制判断）</summary>
        public List<string> visitedIds = new List<string>();

        /// <summary>
        /// 创建默认状态（全部50个顾客在可用池）
        /// </summary>
        public static NightCustomerState CreateDefault()
        {
            var state = new NightCustomerState();
            
            // 从 NpcDatabase 加载所有50个顾客ID
            var database = UnityEngine.Resources.Load<NpcDatabase>("NpcDatabase");
            if (database != null)
            {
                foreach (var npc in database.allNpcs)
                {
                    if (npc != null && !string.IsNullOrEmpty(npc.id))
                    {
                        state.availablePool.Add(npc.id);
                    }
                }
            }
            
            return state;
        }

        public override string ToString()
        {
            return $"[NightCustomerState] 队列:{queuedNpcIds.Count}, 可用:{availablePool.Count}, " +
                   $"冷却:{cooldownPool.Count}, 到访:{globalVisitorCount}, 生成:{totalSpawnedCount}, 保底:{guaranteeIds.Count}";
        }
    }
}
