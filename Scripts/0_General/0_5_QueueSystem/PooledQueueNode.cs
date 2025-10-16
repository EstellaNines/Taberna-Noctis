namespace TabernaNoctis.QueueSystem
{
    /// <summary>
    /// 链表节点类，用于 PooledQueue 的内部实现
    /// - sealed 避免虚方法调用开销
    /// - 节点本身从对象池复用，零 GC
    /// </summary>
    /// <typeparam name="T">队列元素类型</typeparam>
    public sealed class PooledQueueNode<T>
    {
        /// <summary>节点存储的值</summary>
        public T Value;
        
        /// <summary>下一个节点的引用</summary>
        public PooledQueueNode<T> Next;

        /// <summary>
        /// 重置节点状态，归还对象池前调用
        /// - 清空引用，避免内存泄漏
        /// - 对于值类型，Value 会被自动清零
        /// </summary>
        public void Reset()
        {
            Value = default;
            Next = null;
        }
    }
}

