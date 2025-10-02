using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonHoverScaleGroup : MonoBehaviour
{
    [Header("Targets")] public List<Transform> targets = new List<Transform>();

    [Header("Hover Scale Settings")]
    [Min(0.01f)] public float animationDuration = 0.2f;
    [Min(0.01f)] public float hoverScaleMultiplier = 1.1f;
    public Ease ease = Ease.OutBack;
    public bool useUnscaledTime = true;

    private readonly Dictionary<Transform, Vector3> _originalScale = new Dictionary<Transform, Vector3>();
    private readonly Dictionary<Transform, Tween> _activeTweens = new Dictionary<Transform, Tween>();
    private readonly Dictionary<GameObject, List<EventTrigger.Entry>> _createdEntries = new Dictionary<GameObject, List<EventTrigger.Entry>>();

    private void Awake()
    {
        CacheOriginalScales();
    }

    private void OnEnable()
    {
        ResetAllScales();
        RegisterAllEventTriggers();
    }

    private void OnDisable()
    {
        KillAllTweens();
        ResetAllScales();
        UnregisterAllEventTriggers();
    }

    private void CacheOriginalScales()
    {
        _originalScale.Clear();
        foreach (var t in targets)
        {
            if (t == null) continue;
            if (!_originalScale.ContainsKey(t))
                _originalScale.Add(t, t.localScale);
        }
    }

    private void ResetAllScales()
    {
        foreach (var kv in _originalScale)
        {
            if (kv.Key != null)
                kv.Key.localScale = kv.Value;
        }
    }

    private void KillAllTweens()
    {
        foreach (var kv in _activeTweens)
        {
            var tween = kv.Value;
            if (tween != null && tween.IsActive()) tween.Kill();
        }
        _activeTweens.Clear();
    }

    private void RegisterAllEventTriggers()
    {
        foreach (var t in targets)
        {
            if (t == null) continue;
            RegisterEventTrigger(t.gameObject, t);
        }
    }

    private void UnregisterAllEventTriggers()
    {
        foreach (var pair in _createdEntries)
        {
            var go = pair.Key;
            if (go == null) continue;
            var trigger = go.GetComponent<EventTrigger>();
            if (trigger == null) continue;

            var entries = pair.Value;
            if (entries == null) continue;

            foreach (var entry in entries)
            {
                trigger.triggers.Remove(entry);
            }
        }
        _createdEntries.Clear();
    }

    private void RegisterEventTrigger(GameObject go, Transform target)
    {
        var trigger = go.GetComponent<EventTrigger>();
        if (trigger == null) trigger = go.AddComponent<EventTrigger>();

        if (!_createdEntries.ContainsKey(go))
            _createdEntries[go] = new List<EventTrigger.Entry>();

        // PointerEnter
        var enterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enterEntry.callback.AddListener(_ => OnPointerEnterTarget(target));
        trigger.triggers.Add(enterEntry);
        _createdEntries[go].Add(enterEntry);

        // PointerExit
        var exitEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exitEntry.callback.AddListener(_ => OnPointerExitTarget(target));
        trigger.triggers.Add(exitEntry);
        _createdEntries[go].Add(exitEntry);
    }

    private void OnPointerEnterTarget(Transform target)
    {
        if (target == null) return;
        if (!_originalScale.TryGetValue(target, out var baseScale))
        {
            baseScale = target.localScale;
            _originalScale[target] = baseScale;
        }
        ScaleTo(target, baseScale * hoverScaleMultiplier);
    }

    private void OnPointerExitTarget(Transform target)
    {
        if (target == null) return;
        if (!_originalScale.TryGetValue(target, out var baseScale)) baseScale = target.localScale;
        ScaleTo(target, baseScale);
    }

    private void ScaleTo(Transform target, Vector3 toScale)
    {
        if (target == null) return;

        if (_activeTweens.TryGetValue(target, out var tween) && tween != null && tween.IsActive())
        {
            tween.Kill();
        }

        var newTween = target
            .DOScale(toScale, animationDuration)
            .SetEase(ease)
            .SetUpdate(useUnscaledTime);

        _activeTweens[target] = newTween;
    }
}


