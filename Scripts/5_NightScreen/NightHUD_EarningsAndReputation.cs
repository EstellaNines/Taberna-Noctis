using UnityEngine;
using TMPro;
using DG.Tweening;
using Sirenix.OdinInspector;

namespace TabernaNoctis.NightScreen
{
    /// <summary>
    /// 夜晚HUD - 显示：
    /// 1) 玩家当前拥有的金钱与评价（由外部系统刷新，可预留接口）
    /// 2) 本次鸡尾酒结算的收入与评价增量（绿色显示），自上而下淡入动画
    /// </summary>
    public class NightHUD_EarningsAndReputation : MonoBehaviour
    {
        [Title("当前数值显示")]
        [LabelText("当前金钱TMP")]
        [SerializeField] private TMP_Text currentMoneyText;      // "$1234"
        [LabelText("当前评价TMP")]
        [SerializeField] private TMP_Text currentReputationText; // "⭐ 456"

        [Title("本次结算显示（绿色）")]
        [LabelText("本次金钱增量TMP(绿色)")]
        [SerializeField] private TMP_Text gainMoneyText;         // "+72"（绿色）
        [LabelText("本次评价增量TMP(绿色)")]
        [SerializeField] private TMP_Text gainReputationText;    // "+5" （绿色）

        [Title("动画设置")]
        [LabelText("淡入时长(秒)")]
        [SerializeField] private float enterDuration = 0.3f;
        [LabelText("顺序间隔(秒)")]
        [SerializeField] private float betweenDelay = 0.05f;
        [LabelText("入场Y偏移(像素)")]
        [SerializeField] private float enterYOffset = 24f;
		[LabelText("停留时长(秒)")]
		[SerializeField] private float stayDuration = 1.0f;
		[LabelText("淡出时长(秒)")]
		[SerializeField] private float exitDuration = 0.2f;

		private CanvasGroup moneyCg;
		private CanvasGroup repCg;
		private RectTransform moneyRt;
		private RectTransform repRt;
		private Vector2 moneyBasePos;
		private Vector2 repBasePos;
        private int currentMoneyCache;
        private float currentReputationCache;

        private void Awake()
        {
            if (gainMoneyText != null)
            {
				moneyCg = gainMoneyText.GetComponent<CanvasGroup>();
				if (moneyCg == null) moneyCg = gainMoneyText.gameObject.AddComponent<CanvasGroup>();
				moneyRt = gainMoneyText.transform as RectTransform;
				if (moneyRt != null) moneyBasePos = moneyRt.anchoredPosition;
            }
            if (gainReputationText != null)
            {
				repCg = gainReputationText.GetComponent<CanvasGroup>();
				if (repCg == null) repCg = gainReputationText.gameObject.AddComponent<CanvasGroup>();
				repRt = gainReputationText.transform as RectTransform;
				if (repRt != null) repBasePos = repRt.anchoredPosition;
            }

			// 初始隐藏绿色增量
			HideGainsImmediate();
        }

        private void OnEnable()
        {
            MessageManager.Register<CustomerServiceManager.SettlementBroadcast>(MessageDefine.SERVICE_PAYMENT_COMPLETE, OnSettlementBroadcast);
            MessageManager.Register<string>(MessageDefine.SAVE_LOADED, OnSaveEvent);
            MessageManager.Register<string>(MessageDefine.SAVE_COMPLETED, OnSaveEvent);
            RefreshFromSaveSnapshot();
        }

        private void OnDisable()
        {
            MessageManager.Remove<CustomerServiceManager.SettlementBroadcast>(MessageDefine.SERVICE_PAYMENT_COMPLETE, OnSettlementBroadcast);
            MessageManager.Remove<string>(MessageDefine.SAVE_LOADED, OnSaveEvent);
            MessageManager.Remove<string>(MessageDefine.SAVE_COMPLETED, OnSaveEvent);
        }

        private void OnSettlementBroadcast(CustomerServiceManager.SettlementBroadcast data)
        {
            Debug.Log($"[NightHUD] 收到结算广播 → 收入:+{data.finalIncome} 评价:+{data.ratingDelta}");
			// 更新本次结算绿色文本（显示后自动淡出）
            if (gainMoneyText != null)
            {
                gainMoneyText.text = $"+{data.finalIncome}";
				PlayEnterAndAutoHide(moneyCg, moneyRt, moneyBasePos);
            }

            if (gainReputationText != null)
            {
                // 稍后一点出现
                DOVirtual.DelayedCall(betweenDelay, () =>
                {
					gainReputationText.text = $"+{data.ratingDelta}";
					PlayEnterAndAutoHide(repCg, repRt, repBasePos);
                });
            }

            // 刷新“当前拥有”的显示（以存档为基线，叠加本次增量）
            currentMoneyCache += data.finalIncome;
            currentReputationCache += data.ratingDelta;
            ApplyCurrentTexts();
        }

        // 对外接口：由外部系统在需要时调用，刷新当前值
        public void RefreshCurrentValues(int money, int reputation)
        {
            if (currentMoneyText != null) currentMoneyText.text = "$" + money.ToString();
            if (currentReputationText != null) currentReputationText.text = reputation.ToString();
        }

        private void OnSaveEvent(string slot)
        {
            RefreshFromSaveSnapshot();
        }

        private void RefreshFromSaveSnapshot()
        {
            if (SaveManager.Instance == null)
            {
                ApplyCurrentTexts();
                return;
            }
            try
            {
                var snap = SaveManager.Instance.GenerateSaveData();
                currentMoneyCache = snap != null ? Mathf.Max(0, snap.currentMoney) : 0;
                currentReputationCache = snap != null ? Mathf.Max(0f, snap.cumulativeScore) : 0f;
            }
            catch { }
            ApplyCurrentTexts();
        }

        private void ApplyCurrentTexts()
        {
            if (currentMoneyText != null) currentMoneyText.text = "$" + currentMoneyCache.ToString();
            if (currentReputationText != null) currentReputationText.text = Mathf.FloorToInt(currentReputationCache).ToString();
        }

        // 仅在结算完成（SERVICE_PAYMENT_COMPLETE）时显示增量；不在提交瞬间预览。

		private void HideGainsImmediate()
		{
			if (moneyCg != null) moneyCg.alpha = 0f;
			if (repCg != null) repCg.alpha = 0f;
		}

		private void PlayEnterAndAutoHide(CanvasGroup cg, RectTransform rt, Vector2 basePos)
		{
			if (cg == null) return;
            if (!cg.gameObject.activeSelf) cg.gameObject.SetActive(true);
			cg.DOKill();
			if (rt != null)
			{
				rt.DOKill();
				rt.anchoredPosition = basePos + new Vector2(0f, -enterYOffset);
			}
			cg.alpha = 0f;
			var seq = DOTween.Sequence();
			seq.Append(cg.DOFade(1f, enterDuration));
			if (rt != null) seq.Join(rt.DOAnchorPosY(basePos.y, enterDuration).SetEase(Ease.OutCubic));
			seq.AppendInterval(stayDuration);
			seq.Append(cg.DOFade(0f, exitDuration));
		}
    }
}


