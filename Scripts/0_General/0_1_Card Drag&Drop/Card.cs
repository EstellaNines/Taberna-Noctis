using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Card : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private Collider2D col;
    private Vector3 StartDragPosition;
    private RectTransform rectTransform;
    private Canvas rootCanvas;
    private CanvasGroup canvasGroup;
    private Vector2 uiStartAnchoredPosition;
    private bool uiDragActive;
    private bool uiDropHandled;
    [TextArea]
    [SerializeField] private string submitTestLog = "这是测试阶段的卡牌提交消息";

    private void Start()
    {
        col = GetComponent<Collider2D>();
        rectTransform = GetComponent<RectTransform>();
        rootCanvas = GetComponentInParent<Canvas>();
        canvasGroup = GetComponent<CanvasGroup>();
    }

    private void OnMouseDown()
    {
        StartDragPosition = transform.position;
        transform.position = GetMousePositionInWorldSpace();
    }

    private void OnMouseDrag()
    {
        transform.position = GetMousePositionInWorldSpace();
    }

    private void OnMouseUp()
    {
        col.enabled = false;
        Collider2D hitCollider = Physics2D.OverlapPoint(transform.position);
        col.enabled = true;
        if (hitCollider != null && hitCollider.TryGetComponent(out ICardDragArea cardDragArea))
        {
            cardDragArea.OnCardDropped(this);
        }
        else
        {
            transform.position = StartDragPosition;
        }
    }

    public Vector3 GetMousePositionInWorldSpace()
    {
        Vector3 P = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        P.z = 0f;
        return P;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (rectTransform == null || rootCanvas == null)
        {
            uiDragActive = false;
            return;
        }

        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        uiDragActive = true;
        uiDropHandled = false;
        uiStartAnchoredPosition = rectTransform.anchoredPosition;
        canvasGroup.blocksRaycasts = false;
        transform.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!uiDragActive)
        {
            return;
        }

        RectTransform parentRect = rectTransform.parent as RectTransform;
        if (parentRect == null)
        {
            return;
        }

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect,
            eventData.position,
            eventData.pressEventCamera,
            out localPoint
        );
        rectTransform.anchoredPosition = localPoint;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!uiDragActive)
        {
            return;
        }

        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = true;
        }

        if (!uiDropHandled)
        {
            rectTransform.anchoredPosition = uiStartAnchoredPosition;
        }

        uiDragActive = false;
        uiDropHandled = false;
    }

    public void ConfirmUIDropHandled()
    {
        uiDropHandled = true;
    }

    public string GetSubmitLogMessage()
    {
        return submitTestLog;
    }
}
