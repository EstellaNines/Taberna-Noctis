using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using DG.Tweening;

// 简易自动保存指示器：监听 SAVE_REQUESTED / SAVE_COMPLETED
public class AutoSaveIndicator : MonoBehaviour
{
    [Header("Blink Icon (optional)")]
    public Graphic blinkGraphic;           // 任意 Graphic（Image/TMP 等）
    public float blinkDuration = 0.4f;     // 闪烁半周期时长
    public float blinkMinAlpha = 0.2f;     // 最低透明度

    [Header("Spinner (optional)")]
    public RectTransform spinner;          // 旋转图标
    public CanvasGroup spinnerGroup;       // 用于淡入淡出（可选）
    public float rotateSpeed = 360f;       // 旋转速度（度/秒）
    public float fadeDuration = 0.2f;      // 淡入淡出时长

    [Header("Common")]
    public float minShowSeconds = 0.5f;    // 最小显示时长

    private UnityAction<string> _onRequested;
    private UnityAction<string> _onCompleted;
    private float _shownTime;
    private bool _pendingHide;
    private bool _active;

    private Tween _blinkTween;
    private Tween _rotateTween;
    private Tween _fadeTween;

    void Awake()
    {
        _onRequested = OnSaveRequested;
        _onCompleted = OnSaveCompleted;
        MessageManager.Register(MessageDefine.SAVE_REQUESTED, _onRequested);
        MessageManager.Register(MessageDefine.SAVE_COMPLETED, _onCompleted);
        HideAll(immediate: true);
    }

    void OnDestroy()
    {
        MessageManager.Remove(MessageDefine.SAVE_REQUESTED, _onRequested);
        MessageManager.Remove(MessageDefine.SAVE_COMPLETED, _onCompleted);
        KillTweens();
    }

    void Update()
    {
        if (_active)
        {
            _shownTime += Time.deltaTime;
            if (_pendingHide && _shownTime >= minShowSeconds)
            {
                HideAll(immediate: false);
                _pendingHide = false;
            }
        }
    }

    private void OnSaveRequested(string slotId)
    {
        ShowAll();
    }

    private void OnSaveCompleted(string slotId)
    {
        if (_shownTime >= minShowSeconds) HideAll(immediate: false);
        else _pendingHide = true;
    }

    private void ShowAll()
    {
        _active = true;
        _shownTime = 0f;
        _pendingHide = false;
        // 启动闪烁
        if (blinkGraphic != null)
        {
            var c = blinkGraphic.color;
            blinkGraphic.color = new Color(c.r, c.g, c.b, 1f);
            blinkGraphic.enabled = true;
            if (_blinkTween != null && _blinkTween.IsActive()) _blinkTween.Kill();
            _blinkTween = blinkGraphic
                .DOFade(blinkMinAlpha, blinkDuration)
                .SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(true);
        }
        // 启动旋转
        if (spinner != null)
        {
            if (spinnerGroup != null)
            {
                if (_fadeTween != null && _fadeTween.IsActive()) _fadeTween.Kill();
                spinnerGroup.gameObject.SetActive(true);
                spinnerGroup.alpha = 0f;
                _fadeTween = spinnerGroup.DOFade(1f, fadeDuration).SetUpdate(true);
            }
            else
            {
                // 无 CanvasGroup 时直接显隐对象
                spinner.gameObject.SetActive(true);
            }
            if (_rotateTween != null && _rotateTween.IsActive()) _rotateTween.Kill();
            spinner.localRotation = Quaternion.identity;
            // 以角速度旋转
            _rotateTween = spinner
                .DORotate(new Vector3(0f, 0f, -360f), rotateSpeed, RotateMode.FastBeyond360)
                .SetSpeedBased(true)
                .SetEase(Ease.Linear)
                .SetLoops(-1)
                .SetUpdate(true);
        }
    }

    private void HideAll(bool immediate)
    {
        _active = false;
        // 停止闪烁
        if (_blinkTween != null && _blinkTween.IsActive()) _blinkTween.Kill();
        if (blinkGraphic != null)
        {
            if (immediate) blinkGraphic.enabled = false;
            else
            {
                // 直接关闭即可，避免额外补间
                blinkGraphic.enabled = false;
            }
        }
        // 停止旋转
        if (_rotateTween != null && _rotateTween.IsActive()) _rotateTween.Kill();
        if (spinner != null) spinner.localRotation = Quaternion.identity;
        if (spinnerGroup != null)
        {
            if (_fadeTween != null && _fadeTween.IsActive()) _fadeTween.Kill();
            if (immediate)
            {
                spinnerGroup.alpha = 0f;
                spinnerGroup.gameObject.SetActive(false);
            }
            else
            {
                _fadeTween = spinnerGroup.DOFade(0f, fadeDuration).SetUpdate(true).OnComplete(() =>
                {
                    spinnerGroup.gameObject.SetActive(false);
                });
            }
        }
        else if (spinner != null)
        {
            // 无 CanvasGroup 时直接隐藏对象
            spinner.gameObject.SetActive(false);
        }
    }

    private void KillTweens()
    {
        if (_blinkTween != null && _blinkTween.IsActive()) _blinkTween.Kill();
        if (_rotateTween != null && _rotateTween.IsActive()) _rotateTween.Kill();
        if (_fadeTween != null && _fadeTween.IsActive()) _fadeTween.Kill();
        _blinkTween = null;
        _rotateTween = null;
        _fadeTween = null;
    }
}


