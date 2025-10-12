using System;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance; // 全局单例

    private AudioSource audioSource; // 用于BGM与默认SE
    private readonly Dictionary<string, AudioClip> dictAudio = new Dictionary<string, AudioClip>(StringComparer.OrdinalIgnoreCase);

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
        audioSource.Stop();
        audioSource.clip = clip;
        audioSource.volume = Mathf.Clamp01(volume);
        audioSource.loop = loop;
        audioSource.Play();
    }

    // 背景音乐停止
    public void StopBGM()
    {
        audioSource.Stop();
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
}
