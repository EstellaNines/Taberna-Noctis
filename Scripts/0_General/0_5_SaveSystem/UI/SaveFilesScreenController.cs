using System.Collections.Generic;
using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

public class SaveFilesScreenController : MonoBehaviour
{
	[BoxGroup("槽位引用")]
	[LabelText("槽位1")] public SaveSlotUI slot1;
	[BoxGroup("槽位引用")]
	[LabelText("槽位2")] public SaveSlotUI slot2;
	[BoxGroup("槽位引用")]
	[LabelText("槽位3")] public SaveSlotUI slot3;

#if ODIN_INSPECTOR
	[LabelText("??")]
#endif
	public string sceneAfterLoad = "3_DayScreen";

	private void OnEnable()
	{
		RefreshSlots();
		Bind();
	}

	private void OnDisable()
	{
		Unbind();
	}

	private void Bind()
	{
		if (slot1 != null) { slot1.OnLoadClicked += OnLoad; slot1.OnDeleteClicked += OnDelete; }
		if (slot2 != null) { slot2.OnLoadClicked += OnLoad; slot2.OnDeleteClicked += OnDelete; }
		if (slot3 != null) { slot3.OnLoadClicked += OnLoad; slot3.OnDeleteClicked += OnDelete; }
	}

	private void Unbind()
	{
		if (slot1 != null) { slot1.OnLoadClicked -= OnLoad; slot1.OnDeleteClicked -= OnDelete; }
		if (slot2 != null) { slot2.OnLoadClicked -= OnLoad; slot2.OnDeleteClicked -= OnDelete; }
		if (slot3 != null) { slot3.OnLoadClicked -= OnLoad; slot3.OnDeleteClicked -= OnDelete; }
	}

	[Button("刷新槽位"), PropertySpace(8)]
	public void RefreshSlots()
	{
		if (SaveManager.Instance == null) new GameObject("SaveManager").AddComponent<SaveManager>();
		var infos = SaveManager.Instance.GetAllSaveSlots();
		Apply(slot1, infos, 1);
		Apply(slot2, infos, 2);
		Apply(slot3, infos, 3);
	}

	private static void Apply(SaveSlotUI ui, List<SaveSlotInfo> infos, int index)
	{
		if (ui == null) return;
		ui.slotIndex = index;
		var info = infos != null && infos.Count >= index ? infos[index - 1] : null;
		ui.Refresh(info);
	}

	private void OnLoad(int slotIndex)
	{
		SaveManager.Instance.LoadSaveSlot(slotIndex.ToString());
		if (!string.IsNullOrEmpty(sceneAfterLoad))
		{
			GlobalSceneManager.LoadWithLoadingScreen(sceneAfterLoad, UnityEngine.SceneManagement.LoadSceneMode.Single);
		}
	}

	private void OnDelete(int slotIndex)
	{
		SaveManager.Instance.DeleteSaveSlot(slotIndex.ToString());
		RefreshSlots();
	}
}


