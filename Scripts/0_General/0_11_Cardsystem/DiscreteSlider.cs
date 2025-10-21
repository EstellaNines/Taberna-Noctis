using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

namespace TN.UI
{
    /// <summary>
    /// 离散滑动条组件 - 滑动点固定在整数位置上
    /// </summary>
    public class DiscreteSlider : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        #region 序列化字段

        [Header("滑动条组件")]
        [SerializeField]
        [Tooltip("滑动条背景Image（RectTransform）")]
        private RectTransform sliderBackground;

        [SerializeField]
        [Tooltip("滑动点Image（RectTransform）")]
        private RectTransform sliderHandle;

        [Header("数值范围")]
        [SerializeField]
        [Tooltip("最小值")]
        private int minValue = -5;

        [SerializeField]
        [Tooltip("最大值")]
        private int maxValue = 5;

        [SerializeField]
        [Tooltip("当前值")]
        private int currentValue = 0;

        [Header("视觉设置")]
        [SerializeField]
        [Tooltip("是否显示刻度标记")]
        private bool showTicks = true;

        [SerializeField]
        [Tooltip("刻度线颜色")]
        private Color tickColor = new Color(1f, 1f, 1f, 0.3f);

        [SerializeField]
        [Tooltip("刻度线宽度")]
        private float tickWidth = 2f;

        [SerializeField]
        [Tooltip("刻度线高度")]
        private float tickHeight = 10f;

        [Header("交互设置")]
        [SerializeField]
        [Tooltip("是否可以拖拽")]
        private bool isDraggable = true;

        [SerializeField]
        [Tooltip("是否可以点击跳转")]
        private bool isClickable = true;

        [SerializeField]
        [Tooltip("是否启用吸附动画")]
        private bool useSnapAnimation = true;

        [SerializeField]
        [Tooltip("吸附动画速度")]
        private float snapSpeed = 15f;

        [Header("事件")]
        [Tooltip("值改变时触发")]
        public UnityEngine.Events.UnityEvent<int> onValueChanged = new UnityEngine.Events.UnityEvent<int>();

        #endregion

        #region 私有字段

        private bool isDragging = false;
        private Canvas parentCanvas;
        private RectTransform rectTransform;
        
        // 刻度线对象
        private GameObject tickContainer;

        #endregion

        #region Unity生命周期

        private void Awake()
        {
            rectTransform = transform as RectTransform;
            parentCanvas = GetComponentInParent<Canvas>();
            
            ValidateComponents();
            CreateTickMarks();
        }

        private void Start()
        {
            // 初始化位置
            SetValue(currentValue, false);
        }

        private void Update()
        {
            if (useSnapAnimation && !isDragging)
            {
                // 平滑移动到目标位置
                Vector2 targetPosition = GetPositionForValue(currentValue);
                if (sliderHandle != null)
                {
                    sliderHandle.anchoredPosition = Vector2.Lerp(
                        sliderHandle.anchoredPosition,
                        targetPosition,
                        Time.deltaTime * snapSpeed
                    );
                }
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // 确保值在范围内
            currentValue = Mathf.Clamp(currentValue, minValue, maxValue);
            
            if (!Application.isPlaying)
            {
                UnityEditor.EditorApplication.delayCall += () =>
                {
                    if (this != null && sliderHandle != null)
                    {
                        Vector2 targetPos = GetPositionForValue(currentValue);
                        sliderHandle.anchoredPosition = targetPos;
                    }
                };
            }
        }
#endif

        #endregion

        #region 公共方法

        /// <summary>
        /// 设置滑动条的值
        /// </summary>
        /// <param name="value">目标值（会自动限制在范围内）</param>
        /// <param name="triggerEvent">是否触发事件</param>
        public void SetValue(int value, bool triggerEvent = true)
        {
            int newValue = Mathf.Clamp(value, minValue, maxValue);
            
            if (newValue != currentValue)
            {
                currentValue = newValue;
                
                if (triggerEvent)
                {
                    onValueChanged?.Invoke(currentValue);
                }
            }

            if (!useSnapAnimation || !Application.isPlaying)
            {
                // 立即更新位置
                UpdateHandlePosition();
            }
        }

        /// <summary>
        /// 获取当前值
        /// </summary>
        public int GetValue()
        {
            return currentValue;
        }

        /// <summary>
        /// 获取值的范围
        /// </summary>
        public (int min, int max) GetRange()
        {
            return (minValue, maxValue);
        }

        /// <summary>
        /// 增加值
        /// </summary>
        public void Increment()
        {
            SetValue(currentValue + 1);
        }

        /// <summary>
        /// 减少值
        /// </summary>
        public void Decrement()
        {
            SetValue(currentValue - 1);
        }

        /// <summary>
        /// 重置到0
        /// </summary>
        public void ResetToZero()
        {
            SetValue(0);
        }

        /// <summary>
        /// 设置范围（运行时）
        /// </summary>
        public void SetRange(int min, int max)
        {
            minValue = min;
            maxValue = max;
            currentValue = Mathf.Clamp(currentValue, minValue, maxValue);
            
            RecreateTickMarks();
            UpdateHandlePosition();
        }

        #endregion

        #region 拖拽事件处理

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!isDraggable) return;
            isDragging = true;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!isDraggable || sliderBackground == null || sliderHandle == null)
                return;

            Vector2 localPoint;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                sliderBackground, eventData.position, eventData.pressEventCamera, out localPoint))
            {
                // 计算相对位置（0-1）
                float normalizedPosition = Mathf.InverseLerp(
                    -sliderBackground.rect.width / 2f,
                    sliderBackground.rect.width / 2f,
                    localPoint.x
                );

                // 转换为离散值
                int targetValue = GetValueFromNormalizedPosition(normalizedPosition);
                
                if (useSnapAnimation)
                {
                    // 使用吸附动画
                    if (targetValue != currentValue)
                    {
                        SetValue(targetValue);
                    }
                }
                else
                {
                    // 立即跳转
                    SetValue(targetValue);
                }
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!isDraggable) return;
            
            isDragging = false;
            
            // 确保最终吸附到正确位置
            UpdateHandlePosition();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!isClickable || sliderBackground == null)
                return;

            Vector2 localPoint;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                sliderBackground, eventData.position, eventData.pressEventCamera, out localPoint))
            {
                float normalizedPosition = Mathf.InverseLerp(
                    -sliderBackground.rect.width / 2f,
                    sliderBackground.rect.width / 2f,
                    localPoint.x
                );

                int targetValue = GetValueFromNormalizedPosition(normalizedPosition);
                SetValue(targetValue);
            }
        }

        #endregion

        #region 私有方法 - 位置计算

        private void ValidateComponents()
        {
            if (sliderBackground == null)
            {
                Debug.LogError("[DiscreteSlider] 未设置滑动条背景！", this);
            }

            if (sliderHandle == null)
            {
                Debug.LogError("[DiscreteSlider] 未设置滑动点！", this);
            }
        }

        private Vector2 GetPositionForValue(int value)
        {
            if (sliderBackground == null)
                return Vector2.zero;

            // 计算归一化位置 [0, 1]
            float normalizedPosition = Mathf.InverseLerp(minValue, maxValue, value);
            
            // 转换为滑动条上的实际位置
            float width = sliderBackground.rect.width;
            float xPosition = Mathf.Lerp(-width / 2f, width / 2f, normalizedPosition);
            
            return new Vector2(xPosition, 0);
        }

        private int GetValueFromNormalizedPosition(float normalizedPosition)
        {
            // 将归一化位置转换为实际值
            float rawValue = Mathf.Lerp(minValue, maxValue, normalizedPosition);
            
            // 四舍五入到最近的整数
            int discreteValue = Mathf.RoundToInt(rawValue);
            
            return Mathf.Clamp(discreteValue, minValue, maxValue);
        }

        private void UpdateHandlePosition()
        {
            if (sliderHandle == null) return;
            
            Vector2 targetPosition = GetPositionForValue(currentValue);
            sliderHandle.anchoredPosition = targetPosition;
        }

        #endregion

        #region 刻度线绘制

        private void CreateTickMarks()
        {
            if (!showTicks || sliderBackground == null)
                return;

            // 清理旧的刻度线
            if (tickContainer != null)
            {
                if (Application.isPlaying)
                    Destroy(tickContainer);
                else
                    DestroyImmediate(tickContainer);
            }

            // 创建刻度线容器
            tickContainer = new GameObject("TickMarks");
            tickContainer.transform.SetParent(sliderBackground, false);
            
            RectTransform containerRect = tickContainer.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0, 0.5f);
            containerRect.anchorMax = new Vector2(1, 0.5f);
            containerRect.sizeDelta = new Vector2(0, tickHeight);
            containerRect.anchoredPosition = Vector2.zero;

            // 创建每个刻度线
            int tickCount = maxValue - minValue + 1;
            
            for (int i = 0; i < tickCount; i++)
            {
                int value = minValue + i;
                CreateSingleTick(value, containerRect);
            }
        }

        private void CreateSingleTick(int value, RectTransform parent)
        {
            GameObject tick = new GameObject($"Tick_{value}");
            tick.transform.SetParent(parent, false);
            
            Image tickImage = tick.AddComponent<Image>();
            tickImage.color = tickColor;
            
            RectTransform tickRect = tick.GetComponent<RectTransform>();
            
            // 计算刻度线位置
            float normalizedPosition = Mathf.InverseLerp(minValue, maxValue, value);
            
            tickRect.anchorMin = new Vector2(normalizedPosition, 0.5f);
            tickRect.anchorMax = new Vector2(normalizedPosition, 0.5f);
            tickRect.sizeDelta = new Vector2(tickWidth, tickHeight);
            tickRect.anchoredPosition = Vector2.zero;
            
            // 0位置的刻度线加粗
            if (value == 0)
            {
                tickRect.sizeDelta = new Vector2(tickWidth * 2f, tickHeight * 1.5f);
                tickImage.color = new Color(tickColor.r, tickColor.g, tickColor.b, tickColor.a * 2f);
            }
        }

        private void RecreateTickMarks()
        {
            CreateTickMarks();
        }

        #endregion

        #region 编辑器辅助

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (sliderBackground == null) return;

            // 在Scene视图中绘制刻度位置
            Gizmos.color = Color.yellow;
            
            int tickCount = maxValue - minValue + 1;
            for (int i = 0; i < tickCount; i++)
            {
                int value = minValue + i;
                Vector3 worldPos = sliderBackground.TransformPoint(GetPositionForValue(value));
                Gizmos.DrawWireSphere(worldPos, 5f);
            }
        }

        [ContextMenu("自动查找组件")]
        private void AutoFindComponents()
        {
            // 自动查找子对象
            if (sliderBackground == null)
            {
                Transform bg = transform.Find("Background");
                if (bg != null)
                    sliderBackground = bg.GetComponent<RectTransform>();
            }

            if (sliderHandle == null)
            {
                Transform handle = transform.Find("Handle");
                if (handle != null)
                    sliderHandle = handle.GetComponent<RectTransform>();
            }

            Debug.Log("[DiscreteSlider] 自动查找组件完成");
        }

        [ContextMenu("重置到0")]
        private void ResetToZeroEditor()
        {
            currentValue = 0;
            if (sliderHandle != null)
            {
                sliderHandle.anchoredPosition = GetPositionForValue(0);
            }
        }

        [ContextMenu("重新创建刻度线")]
        private void RecreateTickMarksEditor()
        {
            CreateTickMarks();
        }
#endif

        #endregion
    }
}

