using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TabernaNoctis.Cards;
using DG.Tweening;
using TMPro;

namespace TabernaNoctis.CardSystem
{
	/// <summary>
	/// 材料卡队列管理器 - 加载并按序号排序材料卡，自动派发
	/// </summary>
	public class MaterialCardQueueManager : MonoBehaviour
	{
	[Header("派发器引用")]
	[SerializeField]
	[Tooltip("卡牌队列派发器")]
	private CardQueueDispenser dispenser;

	[Header("材料卡列表")]
	[SerializeField]
	[Tooltip("材料卡列表（拖入所有MaterialCardSO，或点击下方按钮自动加载）")]
	private List<MaterialCardSO> materialCards = new List<MaterialCardSO>();

	[Header("淡入效果")]
	[SerializeField]
	[Tooltip("派发前先淡入此Image")]
	private Image fadeInImage;

	[SerializeField]
	[Tooltip("Image淡入时长（秒）")]
	private float fadeInDuration = 1f;

	[SerializeField]
	[Tooltip("Image目标Alpha")]
	private float fadeTargetAlpha = 1f;

	[Header("派发设置")]
	[SerializeField]
	[Tooltip("场景加载时自动开始（先淡入Image，再派发）")]
	private bool autoDispenseOnLoad = true;

	[SerializeField]
	[Tooltip("开始延迟（秒），等待场景完全加载")]
	private float startDelay = 0.5f;

	[SerializeField]
	[Tooltip("起始ID（含）——例如16表示从ID=16开始")]
	private int startIdInclusive = 16;

	[SerializeField]
	[Tooltip("是否按从大到小倒序派发（true=倒序，从起始ID向下）")]
	private bool dispenseDescending = true;

		private void Start()
		{
			if (autoDispenseOnLoad)
			{
				Invoke(nameof(StartFadeInAndDispense), startDelay);
			}
		}

		/// <summary>
		/// 先淡入Image/组（父+子），完成后再派发卡牌
		/// </summary>
		private void StartFadeInAndDispense()
		{
			if (fadeInImage != null)
			{
				var root = fadeInImage.transform;
				if (root != null)
				{
					root.gameObject.SetActive(true);

					// 优先整体CanvasGroup淡入
					var canvasGroup = root.GetComponent<CanvasGroup>();
					if (canvasGroup != null)
					{
						canvasGroup.alpha = 0f;
						canvasGroup.DOFade(fadeTargetAlpha, fadeInDuration)
							.SetEase(Ease.InOutQuad)
							.OnComplete(() =>
							{
								Debug.Log("[MaterialCardQueueManager] 组淡入完成（CanvasGroup），开始派发卡牌");
								LoadAndDispenseMaterialCards();
							});

						Debug.Log("[MaterialCardQueueManager] 开始Image组淡入（CanvasGroup）");
					}
					else
					{
						// 无CanvasGroup时，遍历本体与子节点所有 Graphic 与 TMP_Text 逐一淡入
						var graphics = root.GetComponentsInChildren<Graphic>(true);
						var tmps = root.GetComponentsInChildren<TMP_Text>(true);

						if ((graphics == null || graphics.Length == 0) && (tmps == null || tmps.Length == 0))
						{
							// 无可淡入目标，直接派发
							LoadAndDispenseMaterialCards();
							return;
						}

						// 统一重置初始透明度为0
						for (int i = 0; i < graphics.Length; i++)
						{
							var c = graphics[i].color; c.a = 0f; graphics[i].color = c;
						}
						for (int i = 0; i < tmps.Length; i++)
						{
							var c = tmps[i].color; c.a = 0f; tmps[i].color = c;
						}

						// 构建并行序列
						Sequence seq = DOTween.Sequence().SetEase(Ease.InOutQuad);
						for (int i = 0; i < graphics.Length; i++)
						{
							seq.Join(graphics[i].DOFade(fadeTargetAlpha, fadeInDuration));
						}
						for (int i = 0; i < tmps.Length; i++)
						{
							seq.Join(tmps[i].DOFade(fadeTargetAlpha, fadeInDuration));
						}

						seq.OnComplete(() =>
						{
							Debug.Log("[MaterialCardQueueManager] 组淡入完成，开始派发卡牌");
							LoadAndDispenseMaterialCards();
						});

						seq.Play();
						Debug.Log("[MaterialCardQueueManager] 开始Image组淡入（逐组件）");
					}
					return;
				}
			}

			// 未设置根Image，直接派发
			LoadAndDispenseMaterialCards();
		}

	/// <summary>
	/// 加载所有材料卡并按序号排序后派发
	/// </summary>
	public void LoadAndDispenseMaterialCards()
	{
		if (dispenser == null)
		{
			Debug.LogError("[MaterialCardQueueManager] 未设置卡牌派发器！");
			return;
		}

		if (materialCards == null || materialCards.Count == 0)
		{
			Debug.LogWarning("[MaterialCardQueueManager] 材料卡列表为空，请拖入卡牌或点击'自动加载材料卡'按钮");
			return;
		}

		// 过滤与排序：从指定起始ID开始，倒序（如需）
		List<MaterialCardSO> filtered = materialCards
			.Where(card => card != null && card.id <= startIdInclusive)
			.ToList();

		if (dispenseDescending)
		{
			filtered = filtered.OrderByDescending(c => c.id).ToList();
		}
		else
		{
			filtered = filtered.OrderBy(c => c.id).ToList();
		}

		if (filtered.Count == 0)
		{
			Debug.LogWarning($"[MaterialCardQueueManager] 未找到 id<= {startIdInclusive} 的材料卡");
			return;
		}

		Debug.Log($"[MaterialCardQueueManager] 将派发 {filtered.Count} 张材料卡：起始ID={startIdInclusive}，顺序={(dispenseDescending ? "倒序" : "正序")}。");

		// 添加到派发队列
		dispenser.EnqueueCards(filtered.Cast<BaseCardSO>().ToList());

		// 开始派发
		dispenser.StartDispensing();
	}

		/// <summary>
		/// 手动派发下一张卡（出队操作）
		/// </summary>
		public void DispenseNextCard()
		{
			if (dispenser == null)
			{
				Debug.LogError("[MaterialCardQueueManager] 未设置卡牌派发器！");
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
	[ContextMenu("测试：加载并派发材料卡")]
	private void TestLoadAndDispense()
	{
		LoadAndDispenseMaterialCards();
	}

	[ContextMenu("自动加载所有材料卡（编辑器）")]
	private void AutoLoadAllMaterialCards()
	{
		materialCards.Clear();
		
		string[] guids = UnityEditor.AssetDatabase.FindAssets("t:MaterialCardSO");
		foreach (string guid in guids)
		{
			string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
			MaterialCardSO material = UnityEditor.AssetDatabase.LoadAssetAtPath<MaterialCardSO>(path);
			if (material != null)
			{
				materialCards.Add(material);
			}
		}

		// 按id排序
		materialCards = materialCards.OrderBy(card => card.id).ToList();

		Debug.Log($"[MaterialCardQueueManager] 自动加载了 {materialCards.Count} 张材料卡");
		UnityEditor.EditorUtility.SetDirty(this);
	}
#endif
	}
}

