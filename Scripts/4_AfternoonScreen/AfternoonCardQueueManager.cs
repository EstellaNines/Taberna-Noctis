using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using TabernaNoctis.Cards;
using TabernaNoctis.CardSystem;
using Sirenix.OdinInspector;

namespace TabernaNoctis.AfternoonSystem
{
	/// <summary>
	/// 下午场景卡牌队列管理器 - 读取早上购买的材料卡牌并派发
	/// </summary>
	public class AfternoonCardQueueManager : MonoBehaviour
	{
		[Header("派发器引用")]
		[LabelText("卡牌队列派发器")]
		[SerializeField]
		[Tooltip("卡牌队列派发器")]
		private CardQueueDispenser dispenser;

		[Header("材料卡数据")]
		[LabelText("全部材料卡列表")]
		[SerializeField]
		[Tooltip("所有可用的材料卡列表（用于根据ID查找）")]
		private List<MaterialCardSO> allMaterialCards = new List<MaterialCardSO>();

		[Header("派发设置")]
		[LabelText("自动开始派发")]
		[SerializeField]
		[Tooltip("场景加载时自动开始派发")]
		private bool autoDispenseOnStart = true;

		[LabelText("开始延迟(秒)")]
		[SerializeField]
		[Tooltip("开始延迟（秒），等待场景完全加载")]
		private float startDelay = 0.5f;

		[LabelText("按购买顺序派发")]
		[SerializeField]
		[Tooltip("是否按购买顺序派发（true=按购买顺序，false=按ID排序）")]
		private bool dispenseByPurchaseOrder = true;

		private void Start()
		{
			if (autoDispenseOnStart)
			{
				Invoke(nameof(LoadAndDispensePurchasedMaterials), startDelay);
			}
		}

		/// <summary>
		/// 加载早上购买的材料卡并派发
		/// </summary>
		public void LoadAndDispensePurchasedMaterials()
		{
			if (dispenser == null)
			{
				Debug.LogError("[AfternoonCardQueueManager] 未设置卡牌派发器！");
				return;
			}

			// 从存档获取今日购买的材料
			var purchasedItems = GetTodayPurchasedItems();
			if (purchasedItems == null || purchasedItems.Count == 0)
			{
				Debug.LogWarning("[AfternoonCardQueueManager] 今日未购买任何材料");
				return;
			}

		// 根据购买的物品ID查找对应的材料卡（去重处理）
		var purchasedCards = new List<MaterialCardSO>();
		var processedIds = new HashSet<string>();
		
		foreach (var itemKey in purchasedItems)
		{
			// 避免重复添加相同ID的卡牌
			if (processedIds.Contains(itemKey))
			{
				Debug.Log($"[AfternoonCardQueueManager] 跳过重复材料: {itemKey}");
				continue;
			}
			
			var materialCard = FindMaterialCardById(itemKey);
			if (materialCard != null)
			{
				purchasedCards.Add(materialCard);
				processedIds.Add(itemKey);
			}
			else
			{
				Debug.LogWarning($"[AfternoonCardQueueManager] 未找到ID为 {itemKey} 的材料卡");
			}
		}

			if (purchasedCards.Count == 0)
			{
				Debug.LogWarning("[AfternoonCardQueueManager] 没有找到有效的购买材料卡");
				return;
			}

			// 排序处理
			if (!dispenseByPurchaseOrder)
			{
				purchasedCards = purchasedCards.OrderBy(card => card.id).ToList();
			}

			Debug.Log($"[AfternoonCardQueueManager] 将派发 {purchasedCards.Count} 张购买的材料卡（去重后）");

			// 添加到派发队列
			dispenser.EnqueueCards(purchasedCards.Cast<BaseCardSO>().ToList());

			// 开始派发
			dispenser.StartDispensing();
		}

		/// <summary>
		/// 从存档获取今日购买的材料列表
		/// </summary>
		private List<string> GetTodayPurchasedItems()
		{
			if (SaveManager.Instance == null)
			{
				Debug.LogWarning("[AfternoonCardQueueManager] SaveManager 未找到");
				return new List<string>();
			}

			try
			{
				var saveData = SaveManager.Instance.GenerateSaveData();
				return saveData?.todayPurchasedItems ?? new List<string>();
			}
			catch (System.Exception e)
			{
				Debug.LogError($"[AfternoonCardQueueManager] 读取存档失败: {e.Message}");
				return new List<string>();
			}
		}

		/// <summary>
		/// 根据ID查找材料卡
		/// </summary>
		private MaterialCardSO FindMaterialCardById(string itemKey)
		{
			// 尝试将itemKey转换为int ID
			if (int.TryParse(itemKey, out int id))
			{
				return allMaterialCards.FirstOrDefault(card => card != null && card.id == id);
			}

			// 如果不是数字ID，尝试按名称查找
			return allMaterialCards.FirstOrDefault(card => 
				card != null && 
				(string.Equals(card.nameEN, itemKey, System.StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(card.nameCN, itemKey, System.StringComparison.OrdinalIgnoreCase)));
		}

		/// <summary>
		/// 手动派发下一张卡（出队操作）
		/// </summary>
		public void DispenseNextCard()
		{
			if (dispenser == null)
			{
				Debug.LogError("[AfternoonCardQueueManager] 未设置卡牌派发器！");
				return;
			}

			dispenser.DispenseNextCard();
		}

		/// <summary>
		/// 清空队列
		/// </summary>
		public void ClearQueue()
		{
			if (dispenser != null)
			{
				dispenser.ClearQueue();
				dispenser.ClearAllSlots();
			}
		}

#if UNITY_EDITOR
		[ContextMenu("测试：加载并派发购买材料")]
		private void TestLoadAndDispense()
		{
			LoadAndDispensePurchasedMaterials();
		}

		[ContextMenu("自动加载所有材料卡（编辑器）")]
		private void AutoLoadAllMaterialCards()
		{
			allMaterialCards.Clear();
			
			string[] guids = UnityEditor.AssetDatabase.FindAssets("t:MaterialCardSO");
			foreach (string guid in guids)
			{
				string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
				MaterialCardSO material = UnityEditor.AssetDatabase.LoadAssetAtPath<MaterialCardSO>(path);
				if (material != null)
				{
					allMaterialCards.Add(material);
				}
			}

			// 按id排序
			allMaterialCards = allMaterialCards.OrderBy(card => card.id).ToList();

			Debug.Log($"[AfternoonCardQueueManager] 自动加载了 {allMaterialCards.Count} 张材料卡");
			UnityEditor.EditorUtility.SetDirty(this);
		}
#endif
	}
}
