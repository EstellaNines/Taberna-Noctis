using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

// 使用DOTween将两个Image的RectTransform在指定时长内移动到目标位置
[ExecuteAlways]
public class GlassingClinking : MonoBehaviour
{
	// ImageA
	[SerializeField] private Image imageA;
	// ImageB
	[SerializeField] private Image imageB;
	// 目标位置A
	[SerializeField] private Vector2 targetAnchoredPosA;
	// 目标位置B
	[SerializeField] private Vector2 targetAnchoredPosB;
	// 时长
	[SerializeField] private float duration = 1f;
	// 缓动
	[SerializeField] private Ease ease = Ease.OutQuad;
	// 左右图片到达后的倾斜角度与时长
	[SerializeField] private float tiltAngleLeft = -10f;
	[SerializeField] private float tiltAngleRight = 10f;
	[SerializeField] private float tiltDuration = 0.2f;
	[SerializeField] private Ease tiltEase = Ease.OutQuad;
	// 倾斜完成时播放的音效
	[SerializeField] private AudioClip tiltSfx;
	[SerializeField] [Range(0f,1f)] private float tiltSfxVolume = 1f;
	// 可选：指定AudioSource；未指定则使用 PlayClipAtPoint
	[SerializeField] private AudioSource audioTarget;

	// 启用时自动播放（编辑与运行皆可）
	[SerializeField] private bool playOnEnable = true;

	// 起始位置缓存与当前序列
	private Vector2 initialA;
	private Vector2 initialB;
	private bool hasCached = false;
	private Sequence seq;

	// 对外方法：开始
	public void StartMove()
	{
		PlayInternal(!Application.isPlaying);
	}

	// 对外方法：重来
	public void RestartMove()
	{
		ResetToInitial();
		PlayInternal(!Application.isPlaying);
	}

	private void OnEnable()
	{
		CacheInitialIfNeeded();
		if (playOnEnable)
		{
			PlayInternal(!Application.isPlaying);
		}
	}

	private void OnDisable()
	{
		KillSeq();
		#if UNITY_EDITOR
		if (!Application.isPlaying) TryEditorPreviewStop();
		#endif
	}

	private void CacheInitialIfNeeded()
	{
		if (hasCached) return;
		if (imageA != null) initialA = imageA.rectTransform.anchoredPosition;
		if (imageB != null) initialB = imageB.rectTransform.anchoredPosition;
		hasCached = true;
	}

	private void ResetToInitial()
	{
		CacheInitialIfNeeded();
		if (imageA != null) imageA.rectTransform.anchoredPosition = initialA;
		if (imageB != null) imageB.rectTransform.anchoredPosition = initialB;
		// 重置旋转
		if (imageA != null)
		{
			var e = imageA.rectTransform.localEulerAngles;
			imageA.rectTransform.localEulerAngles = new Vector3(0f, 0f, 0f);
		}
		if (imageB != null)
		{
			var e = imageB.rectTransform.localEulerAngles;
			imageB.rectTransform.localEulerAngles = new Vector3(0f, 0f, 0f);
		}
		KillSeq();
	}

	private void KillSeq()
	{
		if (seq != null)
		{
			try { seq.Kill(false); } catch (Exception) { }
			seq = null;
		}
	}

	private void PlayInternal(bool preview)
	{
		KillSeq();
		CacheInitialIfNeeded();

		seq = DOTween.Sequence();
		if (imageA != null) seq.Join(imageA.rectTransform.DOAnchorPos(targetAnchoredPosA, duration).SetEase(ease));
		if (imageB != null) seq.Join(imageB.rectTransform.DOAnchorPos(targetAnchoredPosB, duration).SetEase(ease));

		// 到达后同时倾斜左右图片
		if (imageA != null)
		{
			seq.Append(imageA.rectTransform.DOLocalRotate(new Vector3(0f, 0f, tiltAngleLeft), tiltDuration).SetEase(tiltEase));
		}
		if (imageB != null)
		{
			// 与左图倾斜并行
			seq.Join(imageB.rectTransform.DOLocalRotate(new Vector3(0f, 0f, tiltAngleRight), tiltDuration).SetEase(tiltEase));
		}

		// 完成时播放音效并回调事件
		seq.OnComplete(() =>
		{
			PlayTiltSfx();
			try { onFinished?.Invoke(); } catch (Exception) { }
		});

		#if UNITY_EDITOR
		if (preview)
		{
			if (TryEditorPreviewPrepare(seq))
			{
				TryEditorPreviewStart();
				return;
			}
			// 回退路径：无 DOTweenEditor 时，使用手动更新驱动编辑器预览
			seq.SetUpdate(UpdateType.Manual, true);
			seq.Play();
			EnsureEditorManualLoop();
			return;
		}
		#endif
		seq.Play();
	}

#if UNITY_EDITOR
	// 通过反射调用 DOTweenEditorPreview，避免对 Editor 程序集的直接依赖
	private static bool TryEditorPreviewPrepare(Tween tween)
	{
		var t = Type.GetType("DG.DOTweenEditor.DOTweenEditorPreview, DOTweenEditor");
		if (t == null) t = Type.GetType("DG.DOTweenEditor.DOTweenEditorPreview");
		if (t == null) return false;
		var m = t.GetMethod("PrepareTweenForPreview", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static, null, new Type[] { typeof(Tween), typeof(bool), typeof(bool), typeof(bool) }, null);
		if (m == null) return false;
		m.Invoke(null, new object[] { tween, true, true, true });
		return true;
	}

	private static void TryEditorPreviewStart()
	{
		var t = Type.GetType("DG.DOTweenEditor.DOTweenEditorPreview, DOTweenEditor") ?? Type.GetType("DG.DOTweenEditor.DOTweenEditorPreview");
		var m = t != null ? t.GetMethod("Start", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static, null, Type.EmptyTypes, null) : null;
		if (m != null) m.Invoke(null, null);
	}

	private static void TryEditorPreviewStop()
	{
		var t = Type.GetType("DG.DOTweenEditor.DOTweenEditorPreview, DOTweenEditor") ?? Type.GetType("DG.DOTweenEditor.DOTweenEditorPreview");
		var m = t != null ? t.GetMethod("Stop", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static, null, Type.EmptyTypes, null) : null;
		if (m != null) m.Invoke(null, null);
	}

	// 手动更新循环（编辑模式下的回退预览）
	private static bool s_EditorLoopActive;
	private static double s_LastEditorTime;
	private static void EnsureEditorManualLoop()
	{
		if (s_EditorLoopActive) return;
		s_EditorLoopActive = true;
		s_LastEditorTime = EditorApplication.timeSinceStartup;
		EditorApplication.update += EditorManualUpdate;
	}

	private static void EditorManualUpdate()
	{
		double now = EditorApplication.timeSinceStartup;
		float dt = (float)(now - s_LastEditorTime);
		s_LastEditorTime = now;
		DOTween.ManualUpdate(dt, dt);
		// 刷新视图以便看到数值变化
		EditorApplication.QueuePlayerLoopUpdate();
		SceneView.RepaintAll();
		if (!DOTween.IsTweening(null))
		{
			EditorApplication.update -= EditorManualUpdate;
			s_EditorLoopActive = false;
		}
	}

	// 编辑器下播放音效（预览）
	private void TryEditorPreviewAudio(AudioClip clip, float volume)
	{
		if (clip == null) return;
		var t = Type.GetType("UnityEditor.AudioUtil, UnityEditor") ?? Type.GetType("UnityEditor.AudioUtil");
		if (t == null) return;
		var setVol = t.GetMethod("SetPreviewVolume", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
		if (setVol != null)
		{
			try { setVol.Invoke(null, new object[] { volume }); } catch (Exception) { }
		}
		var play = t.GetMethod("PlayPreviewClip", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic, null, new Type[] { typeof(AudioClip), typeof(int), typeof(bool) }, null);
		if (play != null)
		{
			try { play.Invoke(null, new object[] { clip, 0, false }); } catch (Exception) { }
		}
	}
#endif

	// 运行或编辑模式：播放倾斜完成音效
	private void PlayTiltSfx()
	{
		if (tiltSfx == null)
		{
			return;
		}
		if (Application.isPlaying)
		{
			if (audioTarget != null)
			{
				audioTarget.PlayOneShot(tiltSfx, tiltSfxVolume);
			}
			else
			{
				var pos = Camera.main != null ? Camera.main.transform.position : Vector3.zero;
				AudioSource.PlayClipAtPoint(tiltSfx, pos, tiltSfxVolume);
			}
		}
		#if UNITY_EDITOR
		else
		{
			TryEditorPreviewAudio(tiltSfx, tiltSfxVolume);
		}
		#endif
	}

	// 动效完成事件，供外部衔接流程
	public event Action onFinished;
}


