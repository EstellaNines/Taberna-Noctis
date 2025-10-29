using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using TabernaNoctis.Utility;

namespace TabernaNoctis.NightScreen
{
    /// <summary>
    /// 英文台词解析器：从 Resources/Character/Dialogues/NPCDialogues_en.json 读取，并提供 Dialogue:[1..3] 映射。
    /// </summary>
    public class EnglishJsonDialogueResolver : MonoBehaviour, IDialogueResolver
    {
        [Tooltip("Resources 内的英文台词 JSON 路径（不含扩展名）")]
        [SerializeField] private string resourcePath = "Character/Dialogues/NPCDialogues_en";

        // 结构：root[identityId][state][gender] => Dictionary("Dialogue:[1]"..)
        private Dictionary<string, object> root;

        private void Awake()
        {
            TryLoad();
        }

        public bool TryGetDialogueMap(string identityId, string state, string gender, out Dictionary<string, string> dialogueMap)
        {
            dialogueMap = null;
            if (root == null && !TryLoad())
            {
                Debug.LogWarning("[EnglishJsonDialogueResolver] 未能加载英文台词数据");
                return false;
            }

            try
            {
                // identity
                if (!TryGetDict(root, identityId, out var identityObj)) return false;
                var identityDict = (Dictionary<string, object>)identityObj;

                // state
                if (!TryGetDict(identityDict, state, out var stateObj)) return false;
                var stateDict = (Dictionary<string, object>)stateObj;

                // gender: 统一性别键首字母大写（数据为 Male/Female）
                string normalizedGender = NormalizeGender(gender);
                if (!TryGetDict(stateDict, normalizedGender, out var genderObj)) return false;
                var genderDict = (Dictionary<string, object>)genderObj;

                var result = new Dictionary<string, string>(3, StringComparer.OrdinalIgnoreCase);
                for (int i = 1; i <= 3; i++)
                {
                    string key = $"Dialogue:[{i}]";
                    if (genderDict.TryGetValue(key, out var valObj) && valObj is string s && !string.IsNullOrEmpty(s))
                    {
                        result[key] = CleanString(s);
                    }
                }

                if (result.Count == 0) return false;
                dialogueMap = result;
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[EnglishJsonDialogueResolver] 解析失败: {e}");
                return false;
            }
        }

        private bool TryLoad()
        {
            try
            {
                var textAsset = Resources.Load<TextAsset>(resourcePath);
                if (textAsset == null)
                {
                    Debug.LogError($"[EnglishJsonDialogueResolver] Resources.Load 失败: {resourcePath}");
                    return false;
                }
                var parsed = MiniJSON.Deserialize(textAsset.text) as Dictionary<string, object>;
                if (parsed == null)
                {
                    Debug.LogError("[EnglishJsonDialogueResolver] JSON 反序列化失败");
                    return false;
                }
                root = parsed;
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[EnglishJsonDialogueResolver] 加载异常: {e}");
                return false;
            }
        }

        private static bool TryGetDict(Dictionary<string, object> dict, string key, out object value)
        {
            value = null;
            if (dict == null || string.IsNullOrEmpty(key)) return false;
            return dict.TryGetValue(key, out value) && value is Dictionary<string, object>;
        }

        private static string NormalizeGender(string gender)
        {
            if (string.IsNullOrEmpty(gender)) return "Male"; // 默认 Male
            var lower = gender.ToLowerInvariant();
            if (lower == "female") return "Female";
            return "Male";
        }

        // 运行时清理英文文本中的乱码/错误字符映射
        private static string CleanString(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            // 常见替换：
            // A) Mojibake em-dash
            s = s.Replace("ĄŞ", "—");
            // B) Mojibake apostrophe/contractions (weĄŻre -> we're)
            s = s.Replace("ĄŻ", "'");
            // C) Mojibake quotes (ĄŽlike thisĄŻ -> "like this")
            s = s.Replace("ĄŽ", "\"");
            // D) Broken hyphen/connector (cross6Ľ2timezone -> cross-timezone)
            s = s.Replace("6Ľ2", "-");
            // E) Any leftover non-printables – best-effort trim
            return s;
        }
    }
}


