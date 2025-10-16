using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sirenix.OdinInspector;

/// <summary>
/// 顾客队列UI显示组件（可选）
/// - 显示当前队列中的顾客信息
/// - 实时更新队列状态
/// - 提供简单的队列可视化
/// </summary>
public class CustomerQueueUI : MonoBehaviour
{
    [Title("UI组件")]
    [LabelText("队列信息文本")][SerializeField] private TextMeshProUGUI queueInfoText;
    [LabelText("顾客列表父物体")][SerializeField] private Transform customerListParent;
    [LabelText("顾客项预制体")][SerializeField] private GameObject customerItemPrefab;
    
    [Title("显示设置")]
    [LabelText("最大显示数量")][SerializeField] private int maxDisplayCount = 10;
    [LabelText("自动刷新间隔")][SerializeField] private float refreshInterval = 1f;

    [Title("运行时状态")]
    [ShowInInspector][ReadOnly] private int currentQueueCount = 0;
    [ShowInInspector][ReadOnly] private int availablePoolCount = 0;
    [ShowInInspector][ReadOnly] private int cooldownPoolCount = 0;

    private List<GameObject> customerItems = new List<GameObject>();
    private float refreshTimer = 0f;

    private void Start()
    {
        // 订阅顾客相关消息
        MessageManager.Register<NpcCharacterData>(MessageDefine.CUSTOMER_SPAWNED, OnCustomerSpawned);
        MessageManager.Register<NpcCharacterData>(MessageDefine.CUSTOMER_DEQUEUED, OnCustomerDequeued);
        MessageManager.Register<NpcCharacterData>(MessageDefine.CUSTOMER_VISITED, OnCustomerVisited);
        
        RefreshDisplay();
    }

    private void OnDestroy()
    {
        // 取消订阅
        MessageManager.Remove<NpcCharacterData>(MessageDefine.CUSTOMER_SPAWNED, OnCustomerSpawned);
        MessageManager.Remove<NpcCharacterData>(MessageDefine.CUSTOMER_DEQUEUED, OnCustomerDequeued);
        MessageManager.Remove<NpcCharacterData>(MessageDefine.CUSTOMER_VISITED, OnCustomerVisited);
    }

    private void Update()
    {
        refreshTimer += Time.deltaTime;
        if (refreshTimer >= refreshInterval)
        {
            refreshTimer = 0f;
            RefreshDisplay();
        }
    }

    #region 消息处理

    private void OnCustomerSpawned(NpcCharacterData npc)
    {
        RefreshDisplay();
    }

    private void OnCustomerDequeued(NpcCharacterData npc)
    {
        RefreshDisplay();
    }

    private void OnCustomerVisited(NpcCharacterData npc)
    {
        RefreshDisplay();
    }

    #endregion

    #region UI更新

    private void RefreshDisplay()
    {
        if (CustomerSpawnManager.Instance == null)
        {
            UpdateQueueInfo("顾客系统未初始化", 0, 0, 0);
            return;
        }

        // 获取当前状态
        var stats = CustomerSpawnManager.Instance.GetStats();
        currentQueueCount = stats.queueCount;
        availablePoolCount = stats.availableCount;
        cooldownPoolCount = stats.cooldownCount;
        
        // 更新信息文本
        UpdateQueueInfo("队列状态", currentQueueCount, availablePoolCount, cooldownPoolCount);
        
        // 更新顾客列表（如果有预制体和父物体）
        if (customerItemPrefab != null && customerListParent != null)
        {
            UpdateCustomerList();
        }
    }

    private void UpdateQueueInfo(string status, int queueCount, int availableCount, int cooldownCount)
    {
        if (queueInfoText == null) return;

        string info = $"{status}\n";
        info += $"队列: {queueCount}/18\n";
        info += $"可用: {availableCount}\n";
        info += $"冷却: {cooldownCount}";

        queueInfoText.text = info;
    }

    private void UpdateCustomerList()
    {
        // 清理现有项目
        foreach (var item in customerItems)
        {
            if (item != null)
            {
                Destroy(item);
            }
        }
        customerItems.Clear();

        // 获取队列中的顾客
        var queuedCustomers = CustomerSpawnManager.Instance.GetQueuedCustomers();
        int displayCount = Mathf.Min(queuedCustomers.Count, maxDisplayCount);
        
        for (int i = 0; i < displayCount; i++)
        {
            var customer = queuedCustomers[i];
            var item = Instantiate(customerItemPrefab, customerListParent);
            var text = item.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                text.text = $"{i + 1}. {customer.displayName}\n{customer.identityId}-{customer.state}";
            }
            customerItems.Add(item);
        }
        
        // 如果队列中还有更多顾客，显示省略号
        if (queuedCustomers.Count > maxDisplayCount)
        {
            var item = Instantiate(customerItemPrefab, customerListParent);
            var text = item.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                text.text = $"... 还有 {queuedCustomers.Count - maxDisplayCount} 位";
            }
            customerItems.Add(item);
        }
    }

    #endregion

    #region 调试

    [Title("调试操作")]
    [Button("手动刷新显示")]
    private void DebugRefreshDisplay()
    {
        if (Application.isPlaying)
        {
            RefreshDisplay();
        }
    }

    #endregion
}
