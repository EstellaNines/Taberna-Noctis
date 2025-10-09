using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class ExitApplication : MonoBehaviour
{
	[SerializeField]
	private bool logOnExit = true;

	// 供 Button.onClick 直接绑定
	public void OnExitButtonClicked()
	{
		if (logOnExit)
		{
			Debug.Log("[ExitApplication] Quit requested by Exit button.");
		}
		Quit();
	}

	public static void Quit()
	{
#if UNITY_EDITOR
		EditorApplication.isPlaying = false; // 在编辑器运行状态下，停止播放
#else
		Application.Quit(); // 正式编译后退出程序
#endif
	}
}


