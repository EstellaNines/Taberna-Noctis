using System;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance; // 全局单例

    private AudioSource audioSource; // 用于默认SE
    private AudioSource bgmSource;   // 专用BGM源（避免淡入淡出影响SE）
    private readonly Dictionary<string, AudioClip> dictAudio = new Dictionary<string, AudioClip>(StringComparer.OrdinalIgnoreCase);
    
    // 可控音效管理
    private readonly Dictionary<int, AudioSource> controllableSources = new Dictionary<int, AudioSource>();
    private int nextPlayId = 1;

    [SerializeField] private string resourcesAudioFolder = "Audio"; // Resources 下的音频根目录
    [SerializeField] private bool dontDestroyOnLoad = true;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        if (dontDestroyOnLoad) DontDestroyOnLoad(gameObject);

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        // 创建专用BGM源
        bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.playOnAwake = false;
        bgmSource.loop = true;

        PreloadAllResourcesAudio();
    }

    // 预加载 Resources/Audio 下所有音频，建立名称与路径两种索引
    private void PreloadAllResourcesAudio()
    {
        if (string.IsNullOrEmpty(resourcesAudioFolder)) return;
        var clips = Resources.LoadAll<AudioClip>(resourcesAudioFolder);
        foreach (var clip in clips)
        {
            if (clip == null) continue;
            dictAudio[clip.name] = clip; // 通过名称索引
            var keyWithFolder = resourcesAudioFolder + "/" + clip.name; // 通过路径索引
            dictAudio[keyWithFolder] = clip;
        }
    }

    // 显式按 Resources 路径加载（若未预载或未命中）
    public AudioClip LoadAudio(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;
        if (dictAudio.TryGetValue(path, out var cached)) return cached;
        var clip = Resources.Load<AudioClip>(path);
        if (clip != null)
        {
            dictAudio[path] = clip;
            if (!dictAudio.ContainsKey(clip.name)) dictAudio[clip.name] = clip;
        }
        return clip;
    }

    // 获取音频：支持传入名称或 "Audio/名称"
    private AudioClip GetAudio(string pathOrName)
    {
        if (string.IsNullOrEmpty(pathOrName)) return null;
        if (dictAudio.TryGetValue(pathOrName, out var hit)) return hit;
        var withFolder = resourcesAudioFolder + "/" + pathOrName;
        if (dictAudio.TryGetValue(withFolder, out hit)) return hit;
        return LoadAudio(pathOrName) ?? LoadAudio(withFolder);
    }

    // 背景音乐播放
    public void PlayBGM(string nameOrPath, float volume = 1f, bool loop = true)
    {
        var clip = GetAudio(nameOrPath);
        if (clip == null)
        {
            Debug.LogWarning($"[AudioManager] BGM 未找到：{nameOrPath}");
            return;
        }
        bgmSource.Stop();
        bgmSource.clip = clip;
        bgmSource.volume = Mathf.Clamp01(volume);
        bgmSource.loop = loop;
        bgmSource.Play();
    }

    // 背景音乐停止
    public void StopBGM()
    {
        bgmSource.Stop();
    }

    // ============ BGM 淡入淡出 ============
    public void FadeInBGM(string nameOrPath, float targetVolume, float seconds, bool loop = true)
    {
        var clip = GetAudio(nameOrPath);
        if (clip == null)
        {
            Debug.LogWarning($"[AudioManager] BGM 未找到：{nameOrPath}");
            return;
        }
        StopAllCoroutines(); // 取消可能的上一次淡入淡出
        bgmSource.clip = clip;
        bgmSource.loop = loop;
        bgmSource.volume = 0f;
        bgmSource.Play();
        StartCoroutine(FadeAudioSource(bgmSource, 0f, Mathf.Clamp01(targetVolume), Mathf.Max(0.01f, seconds)));
    }

    public void FadeOutBGM(float seconds, bool stopAtEnd = true)
    {
        StopAllCoroutines();
        StartCoroutine(FadeOutBgmRoutine(Mathf.Max(0.01f, seconds), stopAtEnd));
    }

    private System.Collections.IEnumerator FadeOutBgmRoutine(float seconds, bool stopAtEnd)
    {
        yield return FadeAudioSource(bgmSource, bgmSource.volume, 0f, seconds);
        if (stopAtEnd) bgmSource.Stop();
    }

    private System.Collections.IEnumerator FadeAudioSource(AudioSource src, float from, float to, float seconds)
    {
        float t = 0f;
        src.volume = from;
        while (t < seconds)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / seconds);
            src.volume = Mathf.Lerp(from, to, k);
            yield return null;
        }
        src.volume = to;
    }

    // 音效播放（默认源）
    public void PlaySE(string pathOrName, float volume = 1f)
    {
        var clip = GetAudio(pathOrName);
        if (clip == null)
        {
            Debug.LogWarning($"[AudioManager] SE 未找到：{pathOrName}");
            return;
        }
        audioSource.PlayOneShot(clip, Mathf.Clamp01(volume));
    }

    // 音效播放（指定源，如3D音源）
    public void PlaySE(AudioSource target, string pathOrName, float volume = 1f)
    {
        if (target == null)
        {
            PlaySE(pathOrName, volume);
            return;
        }
        var clip = GetAudio(pathOrName);
        if (clip == null)
        {
            Debug.LogWarning($"[AudioManager] SE 未找到：{pathOrName}");
            return;
        }
        target.PlayOneShot(clip, Mathf.Clamp01(volume));
    }

    // 直接用 AudioClip 播放一次（便于旧逻辑迁移）
    public void PlaySE(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;
        audioSource.PlayOneShot(clip, Mathf.Clamp01(volume));
        if (!dictAudio.ContainsKey(clip.name)) dictAudio[clip.name] = clip;
    }

    #region 可控音效播放（支持暂停/恢复/停止）

    /// <summary>
    /// 播放可控音效（支持循环、暂停、恢复、停止）
    /// </summary>
    /// <param name="pathOrName">音频路径或名称</param>
    /// <param name="volume">音量（0-1）</param>
    /// <param name="loop">是否循环播放</param>
    /// <returns>音效播放ID，用于后续控制（暂停/恢复/停止），如果失败返回-1</returns>
    public int PlayControllableSE(string pathOrName, float volume = 1f, bool loop = true)
    {
        var clip = GetAudio(pathOrName);
        if (clip == null)
        {
            Debug.LogWarning($"[AudioManager] 可控音效未找到：{pathOrName}");
            return -1;
        }

        // 创建新的AudioSource
        var audioSourceGo = new GameObject($"ControllableSE_{nextPlayId}_{clip.name}");
        audioSourceGo.transform.SetParent(transform);
        var source = audioSourceGo.AddComponent<AudioSource>();
        
        source.clip = clip;
        source.volume = Mathf.Clamp01(volume);
        source.loop = loop;
        source.playOnAwake = false;
        source.Play();

        int playId = nextPlayId++;
        controllableSources[playId] = source;

        Debug.Log($"[AudioManager] 开始播放可控音效 ID:{playId}, {pathOrName}, 循环:{loop}");

        // 如果不循环，播放完成后自动清理
        if (!loop)
        {
            StartCoroutine(AutoCleanupAfterPlay(playId, clip.length));
        }

        return playId;
    }

    /// <summary>
    /// 暂停可控音效
    /// </summary>
    public void PauseControllableSE(int playId)
    {
        if (controllableSources.TryGetValue(playId, out var source) && source != null)
        {
            source.Pause();
            Debug.Log($"[AudioManager] 暂停可控音效 ID:{playId}");
        }
    }

    /// <summary>
    /// 恢复可控音效
    /// </summary>
    public void ResumeControllableSE(int playId)
    {
        if (controllableSources.TryGetValue(playId, out var source) && source != null)
        {
            source.UnPause();
            Debug.Log($"[AudioManager] 恢复可控音效 ID:{playId}");
        }
    }

    /// <summary>
    /// 停止可控音效并释放资源
    /// </summary>
    public void StopControllableSE(int playId)
    {
        if (controllableSources.TryGetValue(playId, out var source))
        {
            if (source != null)
            {
                source.Stop();
                Destroy(source.gameObject);
            }
            controllableSources.Remove(playId);
            Debug.Log($"[AudioManager] 停止可控音效 ID:{playId}");
        }
    }

    /// <summary>
    /// 检查可控音效是否正在播放
    /// </summary>
    public bool IsControllableSEPlaying(int playId)
    {
        if (controllableSources.TryGetValue(playId, out var source) && source != null)
        {
            return source.isPlaying;
        }
        return false;
    }

    /// <summary>
    /// 停止所有可控音效
    /// </summary>
    public void StopAllControllableSE()
    {
        foreach (var kvp in controllableSources)
        {
            if (kvp.Value != null)
            {
                kvp.Value.Stop();
                Destroy(kvp.Value.gameObject);
            }
        }
        controllableSources.Clear();
        Debug.Log("[AudioManager] 停止所有可控音效");
    }

    /// <summary>
    /// 设置可控音效的播放音高（会影响播放速度）
    /// </summary>
    public void SetControllableSEPitch(int playId, float pitch)
    {
        if (controllableSources.TryGetValue(playId, out var source) && source != null)
        {
            source.pitch = Mathf.Clamp(pitch, 0.1f, 3f);
            Debug.Log($"[AudioManager] 设置可控音效音高 ID:{playId}, pitch:{source.pitch:F2}");
        }
    }

    /// <summary>
    /// 将可控音效在指定时长内淡出（线性降音量），结束后可选停止并释放
    /// </summary>
    public void FadeOutControllableSE(int playId, float duration, bool stopAndCleanup = true)
    {
        if (duration <= 0f)
        {
            if (stopAndCleanup) StopControllableSE(playId);
            return;
        }
        if (controllableSources.TryGetValue(playId, out var source) && source != null)
        {
            StartCoroutine(FadeOutRoutine(playId, source, duration, stopAndCleanup));
        }
    }

    private System.Collections.IEnumerator FadeOutRoutine(int playId, AudioSource source, float duration, bool stopAndCleanup)
    {
        float startVol = source.volume;
        float t = 0f;
        while (t < duration && source != null)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            source.volume = Mathf.Lerp(startVol, 0f, k);
            yield return null;
        }
        if (source != null)
        {
            source.volume = 0f;
            if (stopAndCleanup)
            {
                StopControllableSE(playId);
            }
        }
    }

    /// <summary>
    /// 在指定秒数后停止可控音效
    /// </summary>
    public void StopControllableSEAfter(int playId, float seconds)
    {
        StartCoroutine(StopAfterDelay(playId, Mathf.Max(0f, seconds)));
    }

    private System.Collections.IEnumerator StopAfterDelay(int playId, float seconds)
    {
        yield return new WaitForSeconds(seconds);
        StopControllableSE(playId);
    }

    private System.Collections.IEnumerator AutoCleanupAfterPlay(int playId, float duration)
    {
        yield return new WaitForSeconds(duration + 0.1f);
        if (controllableSources.ContainsKey(playId))
        {
            StopControllableSE(playId);
        }
    }

    #endregion
}
