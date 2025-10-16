using System.Collections.Generic;

namespace TabernaNoctis.QueueSystem
{
    /// <summary>
    /// 节点对象池，管理 PooledQueueNode<T> 的创建和复用
    /// - 使用 Stack<T> 作为池容器（O(1) Push/Pop）
    /// - 预热机制减少运行时分配
    /// - 统计信息便于性能分析
    /// </summary>
    /// <typeparam name="T">节点存储的元素类型</typeparam>
    public sealed class NodePool<T>
    {
        private readonly Stack<PooledQueueNode<T>> _pool;
        private readonly int _initialCapacity;

        // 统计信息（可选，用于性能分析）
        private int _totalCreated;      // 累计创建的节点数
        private int _totalRented;       // 累计租用次数
        private int _totalReturned;     // 累计归还次数
        private int _peakPoolSize;      // 池中节点数峰值

        /// <summary>
        /// 构造节点池
        /// </summary>
        /// <param name="initialCapacity">初始容量（预热节点数）</param>
        public NodePool(int initialCapacity = 16)
        {
            _initialCapacity = initialCapacity;
            _pool = new Stack<PooledQueueNode<T>>(initialCapacity);
            Prewarm(initialCapacity);
        }

        /// <summary>
        /// 从池中租用一个节点
        /// - 如果池空，则创建新节点（动态扩展）
        /// </summary>
        public PooledQueueNode<T> Rent()
        {
            _totalRented++;
            
            if (_pool.Count > 0)
            {
                return _pool.Pop();
            }

            // 池空，创建新节点（会产生一次 GC，但后续会被复用）
            _totalCreated++;
            return new PooledQueueNode<T>();
        }

        /// <summary>
        /// 归还节点到池中
        /// - 自动重置节点状态
        /// - 更新统计信息
        /// </summary>
        public void Return(PooledQueueNode<T> node)
        {
            if (node == null) return;

            // 重置节点，清除引用，避免内存泄漏
            node.Reset();

            _pool.Push(node);
            _totalReturned++;

            // 更新峰值
            if (_pool.Count > _peakPoolSize)
            {
                _peakPoolSize = _pool.Count;
            }
        }

        /// <summary>
        /// 预热池：预先创建指定数量的节点
        /// - 在游戏启动或场景加载时调用，避免运行时分配
        /// </summary>
        public void Prewarm(int count)
        {
            for (int i = 0; i < count; i++)
            {
                var node = new PooledQueueNode<T>();
                _pool.Push(node);
                _totalCreated++;
            }

            if (_pool.Count > _peakPoolSize)
            {
                _peakPoolSize = _pool.Count;
            }
        }

        /// <summary>
        /// 获取池统计信息（用于调试和性能分析）
        /// </summary>
        public PoolStats GetStats()
        {
            return new PoolStats
            {
                CurrentPoolSize = _pool.Count,
                TotalCreated = _totalCreated,
                TotalRented = _totalRented,
                TotalReturned = _totalReturned,
                PeakPoolSize = _peakPoolSize,
                ActiveNodes = _totalRented - _totalReturned
            };
        }

        /// <summary>
        /// 清空池（释放所有节点，触发 GC）
        /// - 仅在需要彻底清理时调用（如场景卸载）
        /// </summary>
        public void Clear()
        {
            _pool.Clear();
        }

        /// <summary>
        /// 池统计信息结构（公开以供外部访问）
        /// </summary>
        public struct PoolStats
        {
            public int CurrentPoolSize;  // 当前池中空闲节点数
            public int TotalCreated;     // 累计创建的节点数
            public int TotalRented;      // 累计租用次数
            public int TotalReturned;    // 累计归还次数
            public int PeakPoolSize;     // 池中节点数峰值
            public int ActiveNodes;      // 当前活跃（未归还）的节点数

            public override string ToString()
            {
                return $"[NodePool] 当前池大小: {CurrentPoolSize}, 总创建: {TotalCreated}, " +
                       $"总租用: {TotalRented}, 总归还: {TotalReturned}, " +
                       $"峰值: {PeakPoolSize}, 活跃节点: {ActiveNodes}";
            }
        }
    }
}

