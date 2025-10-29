using UnityEngine;
using Sirenix.OdinInspector;
using TabernaNoctis.Cards;

/// <summary>
/// 基于配方SO与提交槽广播的合成控制器：
/// - 监听 CRAFTING_SLOT_CONTENT_UPDATED / CRAFTING_SLOT_CLEARED
/// - 当三个提交槽都填满时（可选自动）进行合成
/// - 合成结果通过 CraftingResultSlot 实例化显示
/// </summary>
public class RecipeCraftingController : MonoBehaviour
{
    [Header("数据源")]
    [LabelText("鸡尾酒配方数据库")]
    [SerializeField] private CocktailRecipeDatabase recipeDatabase;

    [Header("输出槽")]
    [LabelText("结果槽")]
    [SerializeField] private CraftingResultSlot resultSlot;

    [Header("行为")]
    [LabelText("三个槽填满时自动合成")]
	[SerializeField] private bool autoCraftOnReady = false;
	[LabelText("合成成功后清空三个槽")]
	[SerializeField] private bool clearSlotsOnSuccess = true;

	[Header("音效")]
	[LabelText("调酒音效(循环)")]
	[SerializeField] private string mixingSfxPath = GlobalAudio.MixingDrink;
	[LabelText("音量")]
	[Range(0f,1f)]
	[SerializeField] private float mixingSfxVolume = 1f;
	[LabelText("淡出时长(秒)")]
	[Range(0f,2f)]
	[SerializeField] private float mixingFadeOutSeconds = 0.25f;
	[LabelText("放入首个材料即开始播放")]
	[SerializeField] private bool autoStartMixingOnFirstMaterial = true;

    [Header("当前三槽（调试）")]
    [SerializeField, ReadOnly] private MaterialCardSO slotMat0;
    [SerializeField, ReadOnly] private MaterialCardSO slotMat1;
    [SerializeField, ReadOnly] private MaterialCardSO slotMat2;

    private void OnEnable()
    {
        MessageManager.Register<(int slotIndex, BaseCardSO card)>(MessageDefine.CRAFTING_SLOT_CONTENT_UPDATED, OnSlotUpdated);
        MessageManager.Register<int>(MessageDefine.CRAFTING_SLOT_CLEARED, OnSlotCleared);
    }

    private void OnDisable()
    {
        MessageManager.Remove<(int slotIndex, BaseCardSO card)>(MessageDefine.CRAFTING_SLOT_CONTENT_UPDATED, OnSlotUpdated);
        MessageManager.Remove<int>(MessageDefine.CRAFTING_SLOT_CLEARED, OnSlotCleared);
    }

    private void OnSlotUpdated((int slotIndex, BaseCardSO card) payload)
    {
        var idx = Mathf.Clamp(payload.slotIndex, 0, 2);
        int before = GetFilledCount();
        var material = payload.card as MaterialCardSO; // 只有材料才计入
        SetSlot(idx, material);
        Debug.Log($"[RecipeCraftingController] 槽{idx} -> {(material != null ? material.nameEN + "(ID:" + material.id + ")" : "<空>")}");

        int after = GetFilledCount();
        if (autoStartMixingOnFirstMaterial && before == 0 && after > 0)
        {
            StartMixingLoop();
        }
        if (after == 0)
        {
            StopMixingLoop();
        }
        if (autoCraftOnReady && AllFilled())
        {
            Craft();
        }
    }

    private void OnSlotCleared(int slotIndex)
    {
        var idx = Mathf.Clamp(slotIndex, 0, 2);
        SetSlot(idx, null);
        if (GetFilledCount() == 0)
        {
            StopMixingLoop();
        }
    }

    private void SetSlot(int idx, MaterialCardSO mat)
    {
        switch (idx)
        {
            case 0: slotMat0 = mat; break;
            case 1: slotMat1 = mat; break;
            case 2: slotMat2 = mat; break;
        }
    }

    private bool AllFilled()
    {
        return slotMat0 != null && slotMat1 != null && slotMat2 != null;
    }

    // 可绑定到UI按钮
    public void Craft()
    {
        if (recipeDatabase == null)
        {
            Debug.LogWarning("[RecipeCraftingController] 未指定配方数据库");
            return;
        }
        if (!AllFilled())
        {
            Debug.LogWarning("[RecipeCraftingController] 材料不足3个，无法合成");
            return;
        }

        var cocktail = recipeDatabase.ResolveCocktailByMaterials(slotMat0, slotMat1, slotMat2);
        if (cocktail == null)
        {
            Debug.LogWarning("[RecipeCraftingController] 未匹配到任何鸡尾酒，且未设置保底");
            return;
        }

        if (resultSlot != null)
        {
            resultSlot.SetHighlight(true);
            resultSlot.SetResult(cocktail);
        }
		Debug.Log($"[RecipeCraftingController] 合成结果: {cocktail.nameEN} (ID:{cocktail.id})");
		// 成功产出结果 → 停止调酒音效
		StopMixingLoop();

		// 广播：合成成功（仅鸡尾酒）
		MessageManager.Send<CocktailCardSO>(MessageDefine.CRAFTING_RESULT, cocktail);
		// 兼容旧事件名（若其它系统使用了字符串消息）
		MessageManager.Send<CocktailCardSO>("COCKTAIL_CRAFTED", cocktail);
		// 广播：合成成功（含配方材料明细）
		MessageManager.Send<(CocktailCardSO cocktail, MaterialCardSO a, MaterialCardSO b, MaterialCardSO c)>(
			"COCKTAIL_CRAFTED_DETAIL",
			(cocktail, slotMat0, slotMat1, slotMat2)
		);
		// 控制台一句话
		var tags = (cocktail.tags != null && cocktail.tags.Length > 0) ? string.Join("/", cocktail.tags) : "-";
		Debug.Log($"[Crafted] {cocktail.nameEN} | Category={cocktail.category} | Tags={tags} | Price=${cocktail.price} | Cost=${cocktail.cost} | Profit=${cocktail.profit}");

		// 合成成功后按需清空三个槽
		if (clearSlotsOnSuccess)
		{
			MessageManager.Send<int>(MessageDefine.CRAFTING_SLOT_CLEARED, 0);
			MessageManager.Send<int>(MessageDefine.CRAFTING_SLOT_CLEARED, 1);
			MessageManager.Send<int>(MessageDefine.CRAFTING_SLOT_CLEARED, 2);
			// 同步本地记录
			slotMat0 = null;
			slotMat1 = null;
			slotMat2 = null;
		}
    }

    private int mixingPlayId = -1;

    private int GetFilledCount()
    {
        int c = 0;
        if (slotMat0 != null) c++;
        if (slotMat1 != null) c++;
        if (slotMat2 != null) c++;
        return c;
    }

    private void StartMixingLoop()
    {
        if (!autoStartMixingOnFirstMaterial) return;
        if (mixingPlayId != -1) return;
        var audio = AudioManager.instance;
        if (audio == null) return;
        mixingPlayId = audio.PlayControllableSE(mixingSfxPath, mixingSfxVolume, loop: true);
    }

    private void StopMixingLoop()
    {
        if (mixingPlayId != -1)
        {
            var id = mixingPlayId;
            mixingPlayId = -1;
            AudioManager.instance?.FadeOutControllableSE(id, mixingFadeOutSeconds, stopAndCleanup: true);
        }
    }
}


