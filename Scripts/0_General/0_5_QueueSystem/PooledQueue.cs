using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace TabernaNoctis.QueueSystem
{
    /// <summary>
    /// 高性能、零 GC 的对象池队列
    /// - 基于链表 + 节点对象池实现
    /// - O(1) 入队/出队操作
    /// - 动态容量，无需预分配大数组
    /// - 节点复用，避免频繁 GC
    /// </summary>
    /// <typeparam name="T">队列元素类型</typeparam>
    public sealed class PooledQueue<T> : IEnumerable<T>
    {
        private readonly NodePool<T> _nodePool;
        private PooledQueueNode<T> _head;
        private PooledQueueNode<T> _tail;
        private int _count;

        /// <summary>队列中元素数量</summary>
        public int Count => _count;

        /// <summary>队列是否为空</summary>
        public bool IsEmpty => _count == 0;

        /// <summary>
        /// 构造队列
        /// </summary>
        /// <param name="initialPoolCapacity">节点池初始容量（预热节点数）</param>
        public PooledQueue(int initialPoolCapacity = 16)
        {
            _nodePool = new NodePool<T>(initialPoolCapacity);
            _head = null;
            _tail = null;
            _count = 0;
        }

        /// <summary>
        /// 入队：将元素添加到队尾
        /// - 时间复杂度：O(1)
        /// - 空间复杂度：O(1)（节点复用，几乎无 GC）
        /// </summary>
        public void Enqueue(T item)
        {
            // 从池中租用节点
            var newNode = _nodePool.Rent();
            newNode.Value = item;
            newNode.Next = null;

            if (_tail != null)
            {
                // 队列非空，链接到尾部
                _tail.Next = newNode;
                _tail = newNode;
            }
            else
            {
                // 队列为空，新节点既是头也是尾
                _head = newNode;
                _tail = newNode;
            }

            _count++;
        }

        /// <summary>
        /// 出队：尝试移除并返回队首元素
        /// - 时间复杂度：O(1)
        /// - 节点自动归还到池中
        /// </summary>
        /// <param name="result">出队的元素（如果成功）</param>
        /// <returns>是否成功出队</returns>
        public bool TryDequeue(out T result)
        {
            if (_head == null)
            {
                // 队列为空
                result = default;
                return false;
            }

            // 取出队首元素
            result = _head.Value;
            var oldHead = _head;

            // 移动头指针
            _head = _head.Next;
            if (_head == null)
            {
                // 队列已空，清空尾指针
                _tail = null;
            }

            // 归还节点到池
            _nodePool.Return(oldHead);
            _count--;
            return true;
        }

        /// <summary>
        /// 查看队首元素但不移除
        /// </summary>
        /// <param name="result">队首元素（如果存在）</param>
        /// <returns>是否成功查看</returns>
        public bool TryPeek(out T result)
        {
            if (_head == null)
            {
                result = default;
                return false;
            }

            result = _head.Value;
            return true;
        }

        /// <summary>
        /// 清空队列
        /// - 所有节点归还到池中
        /// - 时间复杂度：O(n)
        /// </summary>
        public void Clear()
        {
            var current = _head;
            while (current != null)
            {
                var next = current.Next;
                _nodePool.Return(current);
                current = next;
            }

            _head = null;
            _tail = null;
            _count = 0;
        }

        /// <summary>
        /// 预热节点池
        /// - 建议在场景加载时调用，避免运行时分配
        /// </summary>
        public void PrewarmPool(int additionalCapacity)
        {
            _nodePool.Prewarm(additionalCapacity);
        }

        /// <summary>
        /// 获取节点池统计信息（用于性能分析）
        /// </summary>
        public NodePool<T>.PoolStats GetPoolStats()
        {
            return _nodePool.GetStats();
        }

        /// <summary>
        /// 转换为数组（调试用，会产生 GC）
        /// </summary>
        public T[] ToArray()
        {
            if (_count == 0) return Array.Empty<T>();

            var array = new T[_count];
            var current = _head;
            int index = 0;

            while (current != null)
            {
                array[index++] = current.Value;
                current = current.Next;
            }

            return array;
        }

        /// <summary>
        /// 获取队列的字符串表示（调试用）
        /// </summary>
        public override string ToString()
        {
            if (_count == 0) return "PooledQueue<T> [空]";

            var sb = new StringBuilder();
            sb.Append($"PooledQueue<T> [Count={_count}]: ");

            var current = _head;
            int displayed = 0;
            const int maxDisplay = 10;

            while (current != null && displayed < maxDisplay)
            {
                sb.Append(current.Value);
                if (current.Next != null && displayed < maxDisplay - 1)
                {
                    sb.Append(" -> ");
                }
                current = current.Next;
                displayed++;
            }

            if (_count > maxDisplay)
            {
                sb.Append($" ... (还有 {_count - maxDisplay} 个)");
            }

            return sb.ToString();
        }

        #region IEnumerable<T> 实现（支持 foreach，会产生少量 GC）

        /// <summary>
        /// 获取枚举器（调试用，会产生迭代器 GC）
        /// - 生产环境避免使用 foreach，改用 while + TryDequeue
        /// </summary>
        public IEnumerator<T> GetEnumerator()
        {
            var current = _head;
            while (current != null)
            {
                yield return current.Value;
                current = current.Next;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        #region Unity 编辑器调试支持

#if UNITY_EDITOR
        /// <summary>
        /// 在 Inspector 中显示队列信息（仅编辑器）
        /// </summary>
        [ContextMenu("输出队列详细信息")]
        public void LogDebugInfo()
        {
            Debug.Log($"[PooledQueue] 队列状态:\n{ToString()}");
            Debug.Log($"[PooledQueue] {GetPoolStats()}");
        }
#endif

        #endregion
    }
}

