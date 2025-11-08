using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using System.Collections;

namespace TabernaNoctis.UI
{
    /// <summary>
    /// Logo 视频播放器
    /// 在游戏启动时播放 Logo 视频，播放完成后自动跳转到下一个场景
    /// </summary>
    public class LogoVideoPlayer : MonoBehaviour
    {
        [Header("视频设置")]
        [Tooltip("视频片段资源")]
        [SerializeField] private VideoClip videoClip;
        
        [Tooltip("视频播放完成后要加载的场景名称")]
        [SerializeField] private string nextSceneName = "0_StartScreen";
        
        [Tooltip("是否允许跳过视频（按任意键）")]
        [SerializeField] private bool allowSkip = true;
        
        [Tooltip("最小播放时间（秒），防止误触跳过")]
        [SerializeField] private float minPlayTime = 0.5f;

        [Header("淡入淡出设置")]
        [Tooltip("视频开始时的淡入时间")]
        [SerializeField] private float fadeInDuration = 0.5f;
        
        [Tooltip("视频结束时的淡出时间")]
        [SerializeField] private float fadeOutDuration = 0.5f;
        
        [Tooltip("淡入淡出使用的 CanvasGroup")]
        [SerializeField] private CanvasGroup fadeCanvasGroup;

        private VideoPlayer videoPlayer;
        private bool isVideoComplete = false;
        private bool isTransitioning = false;
        private float playStartTime;

        private void Awake()
        {
            // 获取或添加 VideoPlayer 组件
            videoPlayer = GetComponent<VideoPlayer>();
            if (videoPlayer == null)
            {
                videoPlayer = gameObject.AddComponent<VideoPlayer>();
            }

            SetupVideoPlayer();
        }

        private void Start()
        {
            playStartTime = Time.time;
            StartCoroutine(PlayVideoSequence());
        }

        private void Update()
        {
            // 检测跳过输入
            if (allowSkip && !isTransitioning && !isVideoComplete)
            {
                if (Time.time - playStartTime > minPlayTime && IsSkipInput())
                {
                    Debug.Log("[LogoVideoPlayer] 用户跳过视频");
                    SkipVideo();
                }
            }
        }

        /// <summary>
        /// 设置 VideoPlayer 组件
        /// </summary>
        private void SetupVideoPlayer()
        {
            if (videoClip != null)
            {
                videoPlayer.clip = videoClip;
            }
            else
            {
                // 尝试从 Resources 文件夹加载
                videoPlayer.source = VideoSource.Url;
                videoPlayer.url = System.IO.Path.Combine(Application.streamingAssetsPath, "Video/签名.avi");
            }

            videoPlayer.renderMode = VideoRenderMode.CameraNearPlane;
            videoPlayer.playOnAwake = false;
            videoPlayer.isLooping = false;
            videoPlayer.skipOnDrop = true;
            
            // 音频设置
            videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;
            
            // 注册事件
            videoPlayer.loopPointReached += OnVideoComplete;
            videoPlayer.errorReceived += OnVideoError;
        }

        /// <summary>
        /// 播放视频序列（包含淡入淡出）
        /// </summary>
        private IEnumerator PlayVideoSequence()
        {
            Debug.Log("[LogoVideoPlayer] 开始播放 Logo 视频");

            // 淡入
            if (fadeCanvasGroup != null && fadeInDuration > 0)
            {
                yield return StartCoroutine(FadeIn());
            }

            // 准备并播放视频
            videoPlayer.Prepare();
            
            // 等待视频准备完成
            while (!videoPlayer.isPrepared)
            {
                yield return null;
            }

            videoPlayer.Play();

            // 等待视频播放完成或被跳过
            while (!isVideoComplete && !isTransitioning)
            {
                yield return null;
            }

            // 淡出并切换场景
            if (!isTransitioning)
            {
                yield return StartCoroutine(TransitionToNextScene());
            }
        }

        /// <summary>
        /// 淡入效果
        /// </summary>
        private IEnumerator FadeIn()
        {
            float elapsed = 0f;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                fadeCanvasGroup.alpha = Mathf.Clamp01(elapsed / fadeInDuration);
                yield return null;
            }
            fadeCanvasGroup.alpha = 1f;
        }

        /// <summary>
        /// 淡出效果
        /// </summary>
        private IEnumerator FadeOut()
        {
            if (fadeCanvasGroup == null) yield break;
            
            float elapsed = 0f;
            float startAlpha = fadeCanvasGroup.alpha;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / fadeOutDuration);
                yield return null;
            }
            fadeCanvasGroup.alpha = 0f;
        }

        /// <summary>
        /// 视频播放完成回调
        /// </summary>
        private void OnVideoComplete(VideoPlayer vp)
        {
            Debug.Log("[LogoVideoPlayer] 视频播放完成");
            isVideoComplete = true;
        }

        /// <summary>
        /// 视频错误回调
        /// </summary>
        private void OnVideoError(VideoPlayer vp, string message)
        {
            Debug.LogError($"[LogoVideoPlayer] 视频播放错误: {message}");
            // 如果视频播放失败，直接跳转到下一个场景
            StartCoroutine(TransitionToNextScene());
        }

        /// <summary>
        /// 检测跳过输入
        /// </summary>
        private bool IsSkipInput()
        {
            return Input.anyKeyDown || 
                   Input.GetMouseButtonDown(0) || 
                   Input.GetMouseButtonDown(1) ||
                   Input.touchCount > 0;
        }

        /// <summary>
        /// 跳过视频
        /// </summary>
        private void SkipVideo()
        {
            if (isTransitioning) return;
            
            videoPlayer.Stop();
            StartCoroutine(TransitionToNextScene());
        }

        /// <summary>
        /// 过渡到下一个场景
        /// </summary>
        private IEnumerator TransitionToNextScene()
        {
            if (isTransitioning) yield break;
            
            isTransitioning = true;

            // 淡出
            if (fadeCanvasGroup != null && fadeOutDuration > 0)
            {
                yield return StartCoroutine(FadeOut());
            }

            Debug.Log($"[LogoVideoPlayer] 切换到场景: {nextSceneName}");
            
            // 加载下一个场景
            SceneManager.LoadScene(nextSceneName);
        }

        private void OnDestroy()
        {
            // 清理事件订阅
            if (videoPlayer != null)
            {
                videoPlayer.loopPointReached -= OnVideoComplete;
                videoPlayer.errorReceived -= OnVideoError;
            }
        }
    }
}

