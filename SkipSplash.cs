#if !UNITY_EDITOR
using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;

/// <summary>
/// 激进的启动画面跳过器 - 专为Unity个人版设计
/// 虽然无法完全移除Unity Logo，但可以让它显示时间最短
/// </summary>
public class SkipSplash
{
    private static bool _skipAttempted = false;
    
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
    private static void BeforeSplashScreen()
    {
        // 立即尝试跳过
        AggressiveSkip();
        
        // 多重备用机制确保跳过
#if UNITY_WEBGL
        SetupWebGLSkip();
#else
        SetupStandaloneSkip();
#endif
    }
    
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AfterSceneLoad()
    {
        // 场景加载后再次尝试跳过
        AggressiveSkip();
    }
    
    private static void AggressiveSkip()
    {
        if (_skipAttempted) return;
        _skipAttempted = true;
        
        try
        {
            // 多次尝试不同的跳过方法
            SplashScreen.Stop(SplashScreen.StopBehavior.StopImmediate);
            
            // 备用方法：使用协程持续尝试
            var skipper = new GameObject("AggressiveSplashSkipper");
            skipper.AddComponent<PersistentSkipper>();
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Initial splash skip failed: {e.Message}");
        }
    }

#if UNITY_WEBGL
    private static void SetupWebGLSkip()
    {
        // WebGL特定的跳过机制
        Application.focusChanged += (hasFocus) => {
            if (hasFocus) AggressiveSkip();
        };
        
        // 备用：鼠标/键盘交互触发
        Application.wantsToQuit += () => {
            AggressiveSkip();
            return true;
        };
    }
#else
    private static void SetupStandaloneSkip()
    {
        // 为桌面平台创建持续的跳过器
        var persistentGO = new GameObject("PersistentSplashSkipper");
        Object.DontDestroyOnLoad(persistentGO);
        persistentGO.AddComponent<PersistentSkipper>();
    }
#endif

    private class PersistentSkipper : MonoBehaviour
    {
        private int _attempts = 0;
        private const int MAX_ATTEMPTS = 300; // 5秒 * 60fps
        
        void Start()
        {
            StartCoroutine(ContinuousSkip());
        }
        
        private IEnumerator ContinuousSkip()
        {
            while (_attempts < MAX_ATTEMPTS && !SplashScreen.isFinished)
            {
                _attempts++;
                
                try
                {
                    // 每帧都尝试跳过
                    SplashScreen.Stop(SplashScreen.StopBehavior.StopImmediate);
                    
                    // 尝试所有可能的跳过方法
                    if (_attempts % 10 == 0)
                    {
                        // 每10帧尝试一次更激进的方法
                        SplashScreen.Stop(SplashScreen.StopBehavior.FadeOut);
                    }
                }
                catch (System.Exception) 
                { 
                    // 忽略异常，继续尝试
                }
                
                yield return null; // 等待下一帧
            }
            
            // 清理
            yield return new WaitForSeconds(0.1f);
            if (gameObject != null)
                Destroy(gameObject);
        }
        
        void Update()
        {
            // 在Update中也尝试跳过（双重保险）
            if (!SplashScreen.isFinished && _attempts < MAX_ATTEMPTS)
            {
                try
                {
                    SplashScreen.Stop(SplashScreen.StopBehavior.StopImmediate);
                }
                catch (System.Exception) { }
            }
        }
        
        // 响应用户输入立即跳过
        void OnGUI()
        {
            if (Event.current != null && (Event.current.type == EventType.KeyDown || 
                Event.current.type == EventType.MouseDown))
            {
                try
                {
                    SplashScreen.Stop(SplashScreen.StopBehavior.StopImmediate);
                }
                catch (System.Exception) { }
            }
        }
    }
}
#endif
